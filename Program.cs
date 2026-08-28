#region Usings
using System.Text;
using System.Security.Claims;
using System.IdentityModel.Tokens.Jwt;
using Microsoft.IdentityModel.Tokens;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.OpenApi.Models;
using HotelariaApi.Data;
using HotelariaApi.Domain;
#endregion

var builder = WebApplication.CreateBuilder(args);

#region JWT Configuration
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
#endregion

 #region Database Configuration
var connectionString = builder.Configuration.GetConnectionString("DefaultConnection");
if (connectionString != null && connectionString.Contains("://")) {
    var databaseUri = new Uri(connectionString);
    var userInfo = databaseUri.UserInfo.Split(':');
    int port = databaseUri.Port == -1 ? 5432 : databaseUri.Port;
    connectionString = $"Host={databaseUri.Host};Port={port};Database={databaseUri.AbsolutePath.TrimStart('/')};Username={userInfo[0]};Password={userInfo[1]};SSL Mode=Require;Trust Server Certificate=true;";
}

builder.Services.AddDbContext<HotelDbContext>(options => options.UseNpgsql(connectionString));
#endregion

 #region SWAGGER Configuration
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
#endregion

var app = builder.Build();

#region Middlewares
app.UseCors();
app.UseSwagger();
app.UseSwaggerUI(c =>
{
    c.SwaggerEndpoint("/swagger/v1/swagger.json", "Hotelaria API v1");
    c.RoutePrefix = "swagger";
});

app.UseAuthentication();
app.UseAuthorization();
#endregion

#region Endpoints
app.MapGet("/", () => "HotelariaPro API v1 - Online");

// AUTH - Login
app.MapPost("/auth/login", async (LoginRequest login, HotelDbContext db) => {
    var user = await db.Usuarios
        .Include(u => u.Pousadas)
        .ThenInclude(up => up.Pousada)
        .Include(u => u.Pousadas)
        .ThenInclude(up => up.Perfil)
        .ThenInclude(p => p.Funcionalidades)
        .FirstOrDefaultAsync(u => u.Email == login.Email);

    if (user == null) return Results.Unauthorized();
    bool senhaValida = BCrypt.Net.BCrypt.Verify(login.Senha, user.SenhaHash);
    
    if (!senhaValida) return Results.Unauthorized();

    var token = GenerateJwtToken(user, jwtKey);
    var pousadas = user.EhDono
        ? await db.Pousadas
            .Where(p => p.ContaId == user.ContaId)
            .Select(p => new { p.Id, p.ContaId, p.NomeFantasia, p.Cidade })
            .ToListAsync()
        : user.Pousadas
            .Select(up => new { up.Pousada.Id, up.Pousada.ContaId, up.Pousada.NomeFantasia, up.Pousada.Cidade })
            .ToList();
    var perfilInicial = user.Pousadas.FirstOrDefault()?.Perfil;
    return Results.Ok(new { token, user = new { user.Id, user.Nome, user.Email, user.EhDono, perfilId = perfilInicial?.Id ?? 0, perfil = perfilInicial, pousadas } });
});

// QUARTOS
app.MapGet("/quartos", async (int pousadaId, HttpContext http, HotelDbContext db) => {
    var pousadaAutorizada = await GetAuthorizedPousadaId(http.User, pousadaId, db);
    return pousadaAutorizada is null ? Results.BadRequest("Informe uma pousada válida em pousadaId.") :
        Results.Ok(await db.Quartos
            .Where(q => q.PousadaId == pousadaAutorizada)
            .Select(q => new { q.Id, q.PousadaId, q.Numero, q.Tipo, q.Status, q.Capacidade, q.PrecoBase })
            .ToListAsync());
}).RequireAuthorization();

app.MapPost("/quartos", async (int pousadaId, HttpContext http, QuartoCreateRequest request, HotelDbContext db) => {
    var pousadaAutorizada = await GetAuthorizedPousadaId(http.User, pousadaId, db);
    if (pousadaAutorizada is null) return Results.BadRequest("Informe uma pousada válida em pousadaId.");
    var quarto = new Quarto
    {
        PousadaId = pousadaAutorizada.Value,
        Numero = request.Numero,
        Tipo = request.Tipo,
        Status = request.Status,
        Capacidade = request.Capacidade,
        PrecoBase = request.PrecoBase
    };
    db.Quartos.Add(quarto);
    await db.SaveChangesAsync();
    return Results.Created($"/quartos/{quarto.Id}", new { quarto.Id, quarto.PousadaId, quarto.Numero, quarto.Tipo, quarto.Status, quarto.Capacidade, quarto.PrecoBase });
}).RequireAuthorization();

app.MapPut("/quartos/{id}", async (int id, int pousadaId, HttpContext http, QuartoUpdateRequest quartoAlterado, HotelDbContext db) => {
    var pousadaAutorizada = await GetAuthorizedPousadaId(http.User, pousadaId, db);
    if (pousadaAutorizada is null) return Results.BadRequest("Informe uma pousada válida em pousadaId.");
    var quarto = await db.Quartos.FirstOrDefaultAsync(q => q.Id == id && q.PousadaId == pousadaAutorizada);
    if (quarto == null) return Results.NotFound();

    quarto.Numero = quartoAlterado.Numero;
    quarto.Tipo = quartoAlterado.Tipo;
    quarto.Capacidade = quartoAlterado.Capacidade;
    quarto.PrecoBase = quartoAlterado.PrecoBase;
    quarto.Status = quartoAlterado.Status;

    await db.SaveChangesAsync();
    return Results.NoContent();
}).RequireAuthorization();

app.MapDelete("/quartos/{id}", async (int id, int pousadaId, HttpContext http, HotelDbContext db) => {
    var pousadaAutorizada = await GetAuthorizedPousadaId(http.User, pousadaId, db);
    if (pousadaAutorizada is null) return Results.BadRequest("Informe uma pousada válida em pousadaId.");
    var quarto = await db.Quartos.FirstOrDefaultAsync(q => q.Id == id && q.PousadaId == pousadaAutorizada);
    if (quarto == null) return Results.NotFound();

    db.Quartos.Remove(quarto);
    await db.SaveChangesAsync();
    return Results.Ok();
}).RequireAuthorization();

// USUARIOS
app.MapGet("/usuarios", async (int pousadaId, HttpContext http, HotelDbContext db) => {
    var pousadaAutorizada = await GetAuthorizedPousadaId(http.User, pousadaId, db);
    if (pousadaAutorizada is null) return Results.BadRequest("Informe uma pousada válida em pousadaId.");
    if (!await UserCanManagePousada(http.User, pousadaAutorizada.Value, db)) return Results.Forbid();
    var usuarios = await db.UsuariosPousadas
        .Where(up => up.PousadaId == pousadaAutorizada)
        .Select(up => new { up.Usuario.Id, up.Usuario.Nome, up.Usuario.Email, up.PerfilId, Perfil = up.Perfil.Nome })
        .ToListAsync();
    return Results.Ok(usuarios);
}).RequireAuthorization();

app.MapPost("/usuarios", async (int pousadaId, HttpContext http, UsuarioCreateRequest request, HotelDbContext db) => {
    var pousadaAutorizada = await GetAuthorizedPousadaId(http.User, pousadaId, db);
    if (pousadaAutorizada is null) return Results.BadRequest("Informe uma pousada válida em pousadaId.");
    if (!await UserCanManagePousada(http.User, pousadaAutorizada.Value, db)) return Results.Forbid();
    var pousada = await db.Pousadas.FindAsync(pousadaAutorizada.Value);
    if (pousada is null) return Results.NotFound();
    var existente = await db.Usuarios.FirstOrDefaultAsync(u => u.Email == request.Email);
    if (existente is not null && existente.ContaId != pousada.ContaId)
        return Results.Conflict("O usuário já pertence a outra conta.");
    var user = existente ?? new Usuario { ContaId = pousada.ContaId };
    if (existente is null) db.Usuarios.Add(user);
    user.Nome = request.Nome;
    user.Email = request.Email;
    user.SenhaHash = BCrypt.Net.BCrypt.HashPassword(request.Senha);
    user.ContaId = pousada.ContaId;

    await db.SaveChangesAsync();
    var vinculo = await db.UsuariosPousadas.FindAsync(user.Id, pousadaAutorizada.Value);
    if (vinculo is null)
        db.UsuariosPousadas.Add(new UsuarioPousada { UsuarioId = user.Id, PousadaId = pousadaAutorizada.Value, PerfilId = request.PerfilId });
    else
        vinculo.PerfilId = request.PerfilId;
    await db.SaveChangesAsync();

    return Results.Created($"/usuarios/{user.Id}", new { 
        id = user.Id,
        nome = user.Nome,
        email = user.Email,
        pousadaId = pousadaAutorizada.Value,
        perfilId = request.PerfilId
    });
}).RequireAuthorization();

app.MapPut("/usuarios/{id}", async (int id, int pousadaId, HttpContext http, UsuarioUpdateRequest inputUser, HotelDbContext db) => {
    var pousadaAutorizada = await GetAuthorizedPousadaId(http.User, pousadaId, db);
    if (pousadaAutorizada is null) return Results.BadRequest("Informe uma pousada válida em pousadaId.");
    if (!await UserCanManagePousada(http.User, pousadaAutorizada.Value, db)) return Results.Forbid();
    var user = await db.Usuarios.FindAsync(id);

    if (user is null) return Results.NotFound();
    var vinculo = await db.UsuariosPousadas.FindAsync(id, pousadaAutorizada.Value);
    if (vinculo is null) return Results.NotFound();

    user.Nome = inputUser.Nome;
    user.Email = inputUser.Email;
    vinculo.PerfilId = inputUser.PerfilId;

    // Se houver uma nova senha no objeto enviado, atualiza o Hash
    if (!string.IsNullOrWhiteSpace(inputUser.Senha)) {
        user.SenhaHash = BCrypt.Net.BCrypt.HashPassword(inputUser.Senha);
    }

    await db.SaveChangesAsync();
    return Results.NoContent();
}).RequireAuthorization();

app.MapDelete("/usuarios/{id}", async (int id, int pousadaId, HttpContext http, HotelDbContext db, ClaimsPrincipal loggedInUser) => {
    var currentUserId = loggedInUser.FindFirst(ClaimTypes.NameIdentifier)?.Value;

    if (currentUserId == id.ToString()) {
        return Results.BadRequest("Você não pode excluir sua própria conta.");
    }

    var pousadaAutorizada = await GetAuthorizedPousadaId(http.User, pousadaId, db);
    if (pousadaAutorizada is null) return Results.BadRequest("Informe uma pousada válida em pousadaId.");
    if (!await UserCanManagePousada(http.User, pousadaAutorizada.Value, db)) return Results.Forbid();

    var user = await db.Usuarios.FindAsync(id);
    if (user is null) return Results.NotFound();
    var vinculo = await db.UsuariosPousadas.FindAsync(id, pousadaAutorizada.Value);
    if (vinculo is null) return Results.NotFound();

    db.UsuariosPousadas.Remove(vinculo);
    await db.SaveChangesAsync();
    return Results.Ok();
}).RequireAuthorization();

// PERFIS
app.MapGet("/perfis", async (HotelDbContext db) => 
    await db.Perfis.Include(p => p.Funcionalidades).ToListAsync())
    .RequireAuthorization();

app.MapPost("/perfis", async (PerfilCreateDTO dto, ClaimsPrincipal principal, HotelDbContext db) => {
    if (!await UserIsOwner(principal, db)) return Results.Forbid();
    
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

app.MapPut("/perfis/{id}", async (int id, UpdatePerfilRequest request, ClaimsPrincipal principal, HotelDbContext db) => {
    if (!await UserIsOwner(principal, db)) return Results.Forbid();
    var perfil = await db.Perfis
        .Include(p => p.Funcionalidades)
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
}).RequireAuthorization();


// FUNCIONALIDADES
app.MapGet("/funcionalidades", async (HotelDbContext db) => 
    await db.Funcionalidades.ToListAsync())
    .RequireAuthorization();

app.MapPost("/funcionalidades", async (FuncionalidadeCreateRequest request, ClaimsPrincipal principal, HotelDbContext db) => {
    if (!await UserIsOwner(principal, db)) return Results.Forbid();
    var func = new Funcionalidade { Nome = request.Nome, Descricao = request.Descricao };
    db.Funcionalidades.Add(func);
    await db.SaveChangesAsync();
    return Results.Created($"/funcionalidades/{func.Id}", func);
}).RequireAuthorization();

// Busca as pousadas da conta do usuário
app.MapGet("/pousadas", async (ClaimsPrincipal principal, HotelDbContext db) => {
    var userId = GetUserId(principal);
    var user = await db.Usuarios.FindAsync(userId);
    if (user is null) return Results.Unauthorized();
    var pousadas = user.EhDono
        ? await db.Pousadas
            .Where(p => p.ContaId == user.ContaId)
            .Select(p => new { p.Id, p.ContaId, p.NomeFantasia, p.Cidade })
            .ToListAsync()
        : await db.UsuariosPousadas
            .Where(up => up.UsuarioId == userId)
            .Select(up => new { up.Pousada.Id, up.Pousada.ContaId, up.Pousada.NomeFantasia, up.Pousada.Cidade })
            .ToListAsync();
    return Results.Ok(pousadas);
}).RequireAuthorization();

// Cadastro/Edição
app.MapPost("/pousadas", async (PousadaCreateRequest input, ClaimsPrincipal principal, HotelDbContext db) => {
    var user = await db.Usuarios.FindAsync(GetUserId(principal));
    if (user is null || !user.EhDono) return Results.Forbid();
    var pousada = new Pousada {
        ContaId = user.ContaId,
        NomeFantasia = input.NomeFantasia,
        RazaoSocial = input.RazaoSocial,
        Cnpj = input.Cnpj,
        Telefone = input.Telefone,
        Endereco = input.Endereco,
        Cidade = input.Cidade,
        CheckInPadrao = input.CheckInPadrao,
        CheckOutPadrao = input.CheckOutPadrao
    };
    db.Pousadas.Add(pousada);
    await db.SaveChangesAsync();
    return Results.Created($"/pousadas/{pousada.Id}", new { pousada.Id, pousada.ContaId, pousada.NomeFantasia, pousada.Cidade });
}).RequireAuthorization();

app.MapGet("/pousadas/{id}", async (int id, ClaimsPrincipal principal, HotelDbContext db) => {
    if (!await UserCanAccessPousada(principal, id, db)) return Results.Forbid();
    var pousada = await db.Pousadas
        .Where(p => p.Id == id)
        .Select(p => new { p.Id, p.ContaId, p.NomeFantasia, p.RazaoSocial, p.Cnpj, p.Telefone, p.Endereco, p.Cidade, p.CheckInPadrao, p.CheckOutPadrao })
        .FirstOrDefaultAsync();
    return pousada is null ? Results.NotFound() : Results.Ok(pousada);
}).RequireAuthorization();

app.MapPut("/pousadas/{id}", async (int id, PousadaUpdateRequest input, ClaimsPrincipal principal, HotelDbContext db) => {
    if (!await UserCanManagePousada(principal, id, db)) return Results.Forbid();
    var pousada = await db.Pousadas.FindAsync(id);
    if (pousada is null) return Results.NotFound();
    pousada.NomeFantasia = input.NomeFantasia;
    pousada.RazaoSocial = input.RazaoSocial;
    pousada.Cnpj = input.Cnpj;
    pousada.Telefone = input.Telefone;
    pousada.Endereco = input.Endereco;
    pousada.Cidade = input.Cidade;
    pousada.CheckInPadrao = input.CheckInPadrao;
    pousada.CheckOutPadrao = input.CheckOutPadrao;
    await db.SaveChangesAsync();
    return Results.NoContent();
}).RequireAuthorization();

app.MapDelete("/pousadas/{id}", async (int id, ClaimsPrincipal principal, HotelDbContext db) => {
    if (!await UserCanManagePousada(principal, id, db)) return Results.Forbid();
    var pousada = await db.Pousadas.FindAsync(id);
    if (pousada is null) return Results.NotFound();
    db.Pousadas.Remove(pousada);
    await db.SaveChangesAsync();
    return Results.NoContent();
}).RequireAuthorization();

// CONSUMO
app.MapPost("/consumo", async (int pousadaId, HttpContext http, ConsumoCreateRequest request, HotelDbContext db) => {
    var pousadaAutorizada = await GetAuthorizedPousadaId(http.User, pousadaId, db);
    if (pousadaAutorizada is null) return Results.BadRequest("Informe uma pousada válida em pousadaId.");
    if (!await db.Quartos.AnyAsync(q => q.Id == request.QuartoId && q.PousadaId == pousadaAutorizada)) return Results.Forbid();
    var c = new Consumo { QuartoId = request.QuartoId, Descricao = request.Descricao, Valor = request.Valor, Quantidade = request.Quantidade };
    db.Consumos.Add(c);
    await db.SaveChangesAsync();
    return Results.Ok();
}).RequireAuthorization();

// DASHBOARD
app.MapGet("/dashboard/stats", async (int pousadaId, HttpContext http, HotelDbContext db) => {
    var pousadaAutorizada = await GetAuthorizedPousadaId(http.User, pousadaId, db);
    if (pousadaAutorizada is null) return Results.BadRequest("Informe uma pousada válida em pousadaId.");
    var hoje = DateTime.Today;
    return Results.Ok(new {
        TotalFaturamento = await db.Consumos.Where(c => c.DataLancamento >= hoje && db.Quartos.Any(q => q.Id == c.QuartoId && q.PousadaId == pousadaAutorizada)).SumAsync(c => c.Valor),
        Ocupados = await db.Quartos.CountAsync(q => q.PousadaId == pousadaAutorizada && q.Status == StatusQuarto.Ocupado),
        TotalQuartos = await db.Quartos.CountAsync(q => q.PousadaId == pousadaAutorizada),
        CheckinsHoje = 4, // Aqui viria a lógica da sua tabela de Reservas
        Limpeza = await db.Quartos.CountAsync(q => q.PousadaId == pousadaAutorizada && q.Status == StatusQuarto.Limpeza)
    });
}).RequireAuthorization();


#endregion

#region Auxiliar Functions
int GetUserId(ClaimsPrincipal principal)
{
    return int.TryParse(principal.FindFirst(ClaimTypes.NameIdentifier)?.Value, out var userId) ? userId : 0;
}

async Task<int?> GetAuthorizedPousadaId(ClaimsPrincipal principal, int pousadaId, HotelDbContext db)
{
    return await UserCanAccessPousada(principal, pousadaId, db) ? pousadaId : null;
}

async Task<bool> UserCanAccessPousada(ClaimsPrincipal principal, int pousadaId, HotelDbContext db)
{
    var userId = GetUserId(principal);
    var user = await db.Usuarios.FindAsync(userId);
    if (user is null) return false;
    return user.EhDono
        ? await db.Pousadas.AnyAsync(p => p.Id == pousadaId && p.ContaId == user.ContaId)
        : await db.UsuariosPousadas.AnyAsync(up => up.UsuarioId == userId && up.PousadaId == pousadaId);
}

    async Task<bool> UserCanManagePousada(ClaimsPrincipal principal, int pousadaId, HotelDbContext db)
    {
        var userId = GetUserId(principal);
        var user = await db.Usuarios.FindAsync(userId);
        if (user is null) return false;
        return user.EhDono || await db.UsuariosPousadas
        .AnyAsync(up => up.UsuarioId == userId && up.PousadaId == pousadaId && up.Perfil.Nome == "Admin");
    }

async Task<bool> UserIsOwner(ClaimsPrincipal principal, HotelDbContext db)
{
    var user = await db.Usuarios.FindAsync(GetUserId(principal));
    return user?.EhDono == true;
}

string GenerateJwtToken(Usuario user, string secretKey)
{
    var tokenHandler = new JwtSecurityTokenHandler();
    var key = Encoding.ASCII.GetBytes(secretKey);
    
    var claims = new List<Claim>
    {
        new Claim(ClaimTypes.NameIdentifier, user.Id.ToString()),
        new Claim(ClaimTypes.Name, user.Email),
        new Claim("ContaId", user.ContaId.ToString()),
        new Claim("EhDono", user.EhDono.ToString())
    };

    foreach (var membership in user.Pousadas)
    {
        claims.Add(new Claim("PousadaId", membership.PousadaId.ToString()));
        claims.Add(new Claim("Perfil", membership.Perfil?.Nome ?? "Sem Perfil"));
        if (membership.Perfil?.Funcionalidades != null)
        {
            foreach (var func in membership.Perfil.Funcionalidades)
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

// Bloco de inicialização de dados (Seed)
using (var scope = app.Services.CreateScope())
{
    var services = scope.ServiceProvider;
    var context = services.GetRequiredService<HotelDbContext>();

    try
    {
        if (string.IsNullOrWhiteSpace(connectionString))
        {
            Console.WriteLine("Connection string vazia ou não configurada; pulando inicialização do banco.");
        }
        else
        {
            // Garante que o banco está criado e as migrations aplicadas
            context.Database.Migrate();

            var conta = context.Contas.FirstOrDefault();
            if (conta == null)
            {
                conta = new Conta { Nome = "Conta principal" };
                context.Contas.Add(conta);
                context.SaveChanges();
            }

            var pousada = context.Pousadas.FirstOrDefault(p => p.ContaId == conta.Id);
            if (pousada == null)
            {
                pousada = new Pousada { ContaId = conta.Id, NomeFantasia = "Pousada principal" };
                context.Pousadas.Add(pousada);
                context.SaveChanges();
            }

            var admin = context.Usuarios.FirstOrDefault(u => u.Email == "admin@hotel.com");
            if (admin == null)
            {
                admin = new Usuario
                {
                    Nome = "Administrador",
                    Email = "admin@hotel.com",
                    ContaId = conta.Id,
                    EhDono = true,
                    SenhaHash = BCrypt.Net.BCrypt.HashPassword("Mudar@123")
                };
                context.Usuarios.Add(admin);
                await context.SaveChangesAsync();
            }
            else if (app.Environment.IsDevelopment())
            {
                admin.ContaId = conta.Id;
                admin.EhDono = true;
                admin.SenhaHash = BCrypt.Net.BCrypt.HashPassword("Mudar@123");
                await context.SaveChangesAsync();
            }

            if (!context.UsuariosPousadas.Any(up => up.UsuarioId == admin.Id && up.PousadaId == pousada.Id))
            {
                context.UsuariosPousadas.Add(new UsuarioPousada { UsuarioId = admin.Id, PousadaId = pousada.Id, PerfilId = 1 });
                await context.SaveChangesAsync();
            }

            var pousadaDois = context.Pousadas.FirstOrDefault(p => p.ContaId == conta.Id && p.NomeFantasia == "Pousada Jardim");
            if (pousadaDois == null)
            {
                pousadaDois = new Pousada
                {
                    ContaId = conta.Id,
                    NomeFantasia = "Pousada Jardim",
                    Cidade = "Sao Paulo",
                    CheckInPadrao = "14:00",
                    CheckOutPadrao = "12:00"
                };
                context.Pousadas.Add(pousadaDois);
                await context.SaveChangesAsync();
            }

            var perfilFuncionario = context.Perfis.FirstOrDefault(p => p.Nome == "Funcionario");
            if (perfilFuncionario == null)
            {
                perfilFuncionario = new Perfil
                {
                    Nome = "Funcionario",
                    Funcionalidades = context.Funcionalidades
                        .Where(f => new[] { 1, 2, 3, 4 }.Contains(f.Id))
                        .ToList()
                };
                context.Perfis.Add(perfilFuncionario);
                await context.SaveChangesAsync();
            }

            var funcionarioCompartilhado = context.Usuarios.FirstOrDefault(u => u.Email == "funcionario@hotel.com");
            if (funcionarioCompartilhado == null)
            {
                funcionarioCompartilhado = new Usuario
                {
                    Nome = "Funcionario Compartilhado",
                    Email = "funcionario@hotel.com",
                    ContaId = conta.Id,
                    SenhaHash = BCrypt.Net.BCrypt.HashPassword("Mudar@123")
                };
                context.Usuarios.Add(funcionarioCompartilhado);
                await context.SaveChangesAsync();
            }

            foreach (var unidade in new[] { pousada, pousadaDois })
            {
                if (!context.UsuariosPousadas.Any(up => up.UsuarioId == funcionarioCompartilhado.Id && up.PousadaId == unidade.Id))
                    context.UsuariosPousadas.Add(new UsuarioPousada { UsuarioId = funcionarioCompartilhado.Id, PousadaId = unidade.Id, PerfilId = perfilFuncionario.Id });
            }
            await context.SaveChangesAsync();

            var recepcionista = context.Usuarios.FirstOrDefault(u => u.Email == "recepcao@hotel.com");
            if (recepcionista == null)
            {
                recepcionista = new Usuario
                {
                    Nome = "Recepcionista",
                    Email = "recepcao@hotel.com",
                    ContaId = conta.Id,
                    SenhaHash = BCrypt.Net.BCrypt.HashPassword("Mudar@123")
                };
                context.Usuarios.Add(recepcionista);
                await context.SaveChangesAsync();
            }

            if (!context.UsuariosPousadas.Any(up => up.UsuarioId == recepcionista.Id && up.PousadaId == pousada.Id))
            {
                context.UsuariosPousadas.Add(new UsuarioPousada { UsuarioId = recepcionista.Id, PousadaId = pousada.Id, PerfilId = perfilFuncionario.Id });
                await context.SaveChangesAsync();
            }
        }
    }
    catch (Exception ex)
    {
        Console.Error.WriteLine($"Falha na inicialização do banco: {ex}");
        throw;
    }
}

app.Run();

#endregion