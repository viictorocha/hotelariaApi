using System.Text;
using System.Security.Claims;
using System.IdentityModel.Tokens.Jwt;
using Microsoft.IdentityModel.Tokens;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.OpenApi.Models;
using HotelariaApi.Data;
using HotelariaApi.Domain;

var builder = WebApplication.CreateBuilder(args);

// --- 1. CONFIGURAÇÃO DE SEGURANÇA (JWT) ---
var jwtKey = builder.Configuration["JWT_SECRET_KEY"];

if (string.IsNullOrEmpty(jwtKey))
{
    jwtKey = "chave_temporaria_desenvolvimento_123456789012"; 
    if (builder.Environment.IsProduction()) {
        throw new Exception("ERRO CRÍTICO: Variável de ambiente JWT_SECRET_KEY não definida!");
    }
}
var keyBytes = Encoding.ASCII.GetBytes(jwtKey);

builder.Services.AddAuthentication(x => {
    x.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
    x.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
})
.AddJwtBearer(x => {
    x.RequireHttpsMetadata = false;
    x.SaveToken = true;
    x.TokenValidationParameters = new TokenValidationParameters {
        ValidateIssuerSigningKey = true,
        IssuerSigningKey = new SymmetricSecurityKey(keyBytes),
        ValidateIssuer = false,
        ValidateAudience = false
    };
});

builder.Services.AddAuthorization();

// --- 2. CONFIGURAÇÃO DO BANCO DE DADOS (RENDER COMPATIBLE) ---
var connectionString = builder.Configuration.GetConnectionString("DefaultConnection");
if (connectionString != null && connectionString.Contains("://")) {
    var databaseUri = new Uri(connectionString);
    var userInfo = databaseUri.UserInfo.Split(':');
    int port = databaseUri.Port == -1 ? 5432 : databaseUri.Port;
    connectionString = $"Host={databaseUri.Host};Port={port};Database={databaseUri.AbsolutePath.TrimStart('/')};Username={userInfo[0]};Password={userInfo[1]};SSL Mode=Require;Trust Server Certificate=true;";
}

builder.Services.AddDbContext<HotelDbContext>(options => options.UseNpgsql(connectionString));

// --- 3. SWAGGER COM SUPORTE A JWT ---
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(c => {
    c.SwaggerDoc("v1", new OpenApiInfo { Title = "HotelariaPro API", Version = "v1" });
    
    c.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme {
        In = ParameterLocation.Header,
        Description = "Insira o token JWT desta forma: Bearer {seu_token}",
        Name = "Authorization",
        Type = SecuritySchemeType.ApiKey,
        BearerFormat = "JWT",
        Scheme = "Bearer"
    });

    c.AddSecurityRequirement(new OpenApiSecurityRequirement {
        {
            new OpenApiSecurityScheme {
                Reference = new OpenApiReference {
                    Type = ReferenceType.SecurityScheme,
                    Id = "Bearer"
                }
            },
            Array.Empty<string>()
        }
    });
});

builder.Services.AddCors(opt => opt.AddDefaultPolicy(p => p.AllowAnyOrigin().AllowAnyHeader().AllowAnyMethod()));

var app = builder.Build();

// Bloco para criar o usuário inicial
using (var scope = app.Services.CreateScope())
{
    var services = scope.ServiceProvider;
    var context = services.GetRequiredService<HotelDbContext>();
    
    // 1. Garante que o banco existe e as tabelas estão lá
    context.Database.EnsureCreated();

    // 2. Só cria se não houver nenhum usuário cadastrado
    if (!context.Usuarios.Any())
    {
        // Criar uma funcionalidade mestre
        var funcMaster = new Funcionalidade { Nome = "FULL_ACCESS", Descricao = "Acesso total ao sistema" };
        context.Funcionalidades.Add(funcMaster);

        // Criar o perfil Admin e vincular a funcionalidade
        var perfilAdmin = new Perfil { 
            Nome = "Admin", 
            Funcionalidades = new List<Funcionalidade> { funcMaster } 
        };
        context.Perfis.Add(perfilAdmin);

        // Criar o usuário mestre
        context.Usuarios.Add(new Usuario { 
            Email = "admin@hotel.com", 
            SenhaHash = "123456", // Em produção, use BCrypt para hash!
            Perfil = perfilAdmin 
        });

        context.SaveChanges();
        Console.WriteLine("--> Usuário admin@hotel.com criado com sucesso!");
    }
}

// --- 4. MIDDLEWARES ---
app.UseCors();
app.UseSwagger();
app.UseSwaggerUI(c =>
{
    c.SwaggerEndpoint("/swagger/v1/swagger.json", "Hotelaria API v1");
    c.RoutePrefix = "swagger";
});

app.UseAuthentication();
app.UseAuthorization();

// --- 5. ENDPOINTS ---
app.MapGet("/", () => "HotelariaPro API v1 - Online");
// AUTH - Login

app.MapPost("/auth/login", async (LoginRequest login, HotelDbContext db) => {
    // 1. Procuramos o usuário pelo email
    var user = await db.Usuarios
        .Include(u => u.Perfil)
        .ThenInclude(p => p.Funcionalidades)
        .FirstOrDefaultAsync(u => u.Email == login.Email);

    // 2. Verificação de Segurança com BCrypt
    // O método Verify compara o texto puro (login.Senha) com o Hash (user.SenhaHash)
    if (user == null || !BCrypt.Net.BCrypt.Verify(login.Senha, user.SenhaHash)) 
    {
        return Results.Unauthorized();
    }

    // 3. Se chegou aqui, a senha está correta
    var token = GenerateJwtToken(user, jwtKey); 
    
    // Retornamos o token e os dados do usuário (que o Flutter já espera)
    return Results.Ok(new { token, user });
});

// QUARTOS
app.MapGet("/quartos", async (HotelDbContext db) => 
    await db.Quartos.ToListAsync())
    .RequireAuthorization(); // <--- Fora do parênteses do MapGet

app.MapPost("/quartos", async (Quarto quarto, HotelDbContext db) => {
    db.Quartos.Add(quarto);
    await db.SaveChangesAsync();
    return Results.Created($"/quartos/{quarto.Id}", quarto);
}).RequireAuthorization();

// USUARIOS
app.MapGet("/usuarios", async (HotelDbContext db) => 
    await db.Usuarios.Include(u => u.Perfil).ThenInclude(p => p.Funcionalidades).ToListAsync());

app.MapPost("/usuarios", async (Usuario user, HotelDbContext db) => {
    // 1. O Flutter envia a senha em texto puro no campo 'SenhaHash' (ou 'Senha')
    // Precisamos hashear antes de salvar no banco
    string senhaPura = user.SenhaHash; 
    
    // 2. Substitui o texto puro pelo Hash
    user.SenhaHash = BCrypt.Net.BCrypt.HashPassword(senhaPura);

    db.Usuarios.Add(user);
    await db.SaveChangesAsync();

    // 3. Retorna um objeto anônimo para não devolver o Hash da senha no JSON
    return Results.Created($"/usuarios/{user.Id}", new { 
        id = user.Id, 
        nome = user.Nome, 
        email = user.Email 
    });
}).RequireAuthorization();

// PERFIS
app.MapGet("/perfis", async (HotelDbContext db) => 
    await db.Perfis.Include(p => p.Funcionalidades).ToListAsync())
    .RequireAuthorization();

app.MapPost("/perfis", async (PerfilCreateDTO dto, HotelDbContext db) => {
    
    var novoPerfil = new Perfil { 
        Nome = dto.Nome 
    };

    if (dto.FuncionalidadesIds != null && dto.FuncionalidadesIds.Any()) {
        var funcsNoBanco = await db.Funcionalidades
            .Where(f => dto.FuncionalidadesIds.Contains(f.Id))
            .ToListAsync();
        
        novoPerfil.Funcionalidades = funcsNoBanco;
    }

    db.Perfis.Add(novoPerfil);
    await db.SaveChangesAsync();

    return Results.Created($"/perfis/{novoPerfil.Id}", novoPerfil);
}).RequireAuthorization();

app.MapPut("/perfis/{id}", async (int id, UpdatePerfilRequest request, HotelDbContext db) => {
    var perfil = await db.Perfis
        .Include(p => p.Funcionalidades) // OBRIGATÓRIO carregar as relações
        .FirstOrDefaultAsync(p => p.Id == id);

    if (perfil == null) return Results.NotFound();

    perfil.Nome = request.Nome;

    // Busca as funcionalidades enviadas pelo Flutter
    var funcsSelecionadas = await db.Funcionalidades
        .Where(f => request.FuncionalidadesIds.Contains(f.Id))
        .ToListAsync();

    // O Entity Framework gerencia a tabela intermediária aqui:
    perfil.Funcionalidades = funcsSelecionadas; 

    await db.SaveChangesAsync();
    return Results.NoContent();
});


// FUNCIONALIDADES
app.MapGet("/funcionalidades", async (HotelDbContext db) => 
    await db.Funcionalidades.ToListAsync())
    .RequireAuthorization();

app.MapPost("/funcionalidades", async (Funcionalidade func, HotelDbContext db) => {
    db.Funcionalidades.Add(func);
    await db.SaveChangesAsync();
    return Results.Created($"/funcionalidades/{func.Id}", func);
}).RequireAuthorization();

// --- 6. FUNÇÕES AUXILIARES ---

string GenerateJwtToken(Usuario user, string secretKey)
{
    var tokenHandler = new JwtSecurityTokenHandler();
    var key = Encoding.ASCII.GetBytes(secretKey);
    
    var claims = new List<Claim>
    {
        new Claim(ClaimTypes.Name, user.Email),
        new Claim("Perfil", user.Perfil?.Nome ?? "Sem Perfil")
    };

    if (user.Perfil?.Funcionalidades != null)
    {
        foreach (var func in user.Perfil.Funcionalidades)
        {
            claims.Add(new Claim("Permissao", func.Nome));
        }
    }

    var tokenDescriptor = new SecurityTokenDescriptor
    {
        Subject = new ClaimsIdentity(claims),
        Expires = DateTime.UtcNow.AddHours(8),
        SigningCredentials = new SigningCredentials(
            new SymmetricSecurityKey(key), 
            SecurityAlgorithms.HmacSha256Signature)
    };

    var token = tokenHandler.CreateToken(tokenDescriptor);
    return tokenHandler.WriteToken(token);
}

app.Run();