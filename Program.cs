using Microsoft.EntityFrameworkCore;
using HotelariaApi.Data;
using HotelariaApi.Domain;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using System.Text;
using System.IdentityModel.Tokens.Jwt;

var builder = WebApplication.CreateBuilder(args);
var jwtKey = builder.Configuration["JWT_SECRET_KEY"];

// Validação de segurança: Impede a API de subir se a chave estiver vazia
if (string.IsNullOrEmpty(jwtKey))
{
    // Localmente você pode definir uma fixa para não quebrar o projeto
    jwtKey = "chave_temporaria_desenvolvimento_123456789"; 
    
    if (app.Environment.IsProduction()) {
        throw new Exception("ERRO CRÍTICO: Variável de ambiente JWT_SECRET_KEY não definida!");
    }
}

var key = Encoding.ASCII.GetBytes(jwtKey);

builder.Services.AddAuthentication(x => {
    x.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
    x.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
})
.AddJwtBearer(x => {
    x.RequireHttpsMetadata = false;
    x.SaveToken = true;
    x.TokenValidationParameters = new TokenValidationParameters {
        ValidateIssuerSigningKey = true,
        IssuerSigningKey = new SymmetricSecurityKey(key),
        ValidateIssuer = false,
        ValidateAudience = false
    };
});

builder.Services.AddAuthorization();

// Configuração do Banco de Dados
var connectionString = builder.Configuration.GetConnectionString("DefaultConnection");
if (connectionString != null && connectionString.Contains("://")) {
    var databaseUri = new Uri(connectionString);
    var userInfo = databaseUri.UserInfo.Split(':');
    int port = databaseUri.Port == -1 ? 5432 : databaseUri.Port;
    connectionString = $"Host={databaseUri.Host};Port={port};Database={databaseUri.AbsolutePath.TrimStart('/')};Username={userInfo[0]};Password={userInfo[1]};SSL Mode=Require;Trust Server Certificate=true;";
}

builder.Services.AddDbContext<HotelDbContext>(options => options.UseNpgsql(connectionString));
builder.Services.AddCors(opt => opt.AddDefaultPolicy(p => p.AllowAnyOrigin().AllowAnyHeader().AllowAnyMethod()));
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

var app = builder.Build();

app.UseCors();

app.UseSwagger();
app.UseSwaggerUI(c =>
{
    c.SwaggerEndpoint("/swagger/v1/swagger.json", "Hotelaria API v1");
    c.RoutePrefix = "swagger";
});

// Endpoints
app.MapGet("/", () => "HotelariaPro API v1 - Online");

app.MapGet("/quartos", async (HotelDbContext db) => await db.Quartos.ToListAsync());

app.MapPost("/quartos", async (Quarto quarto, HotelDbContext db) => {
    db.Quartos.Add(quarto);
    await db.SaveChangesAsync();
    return Results.Created($"/quartos/{quarto.Id}", quarto);
});

// AUTH - Login
app.MapPost("/auth/login", async (Usuario login, HotelDbContext db) => {
    var user = await db.Usuarios.Include(u => u.Perfil).ThenInclude(p => p.Funcionalidades)
        .FirstOrDefaultAsync(u => u.Email == login.Email && u.SenhaHash == login.SenhaHash);

    if (user == null) return Results.Unauthorized();

    var token = GenerateJwtToken(user); // Método para gerar o token
    return Results.Ok(new { token, user });
});

// USUARIOS
app.MapGet("/usuarios", async (HotelDbContext db) => await db.Usuarios.Include(u => u.Perfil).ToListAsync());

app.MapPost("/usuarios", async (Usuario user, HotelDbContext db) => {
    db.Usuarios.Add(user);
    await db.SaveChangesAsync();
    return Results.Created($"/usuarios/{user.Id}", user);
});

// PERFIS
app.MapGet("/perfis", async (HotelDbContext db) => 
    await db.Perfis.Include(p => p.Funcionalidades).ToListAsync());

app.MapPost("/perfis", async (Perfil perfil, HotelDbContext db) => {
    db.Perfis.Add(perfil);
    await db.SaveChangesAsync();
    return Results.Created($"/perfis/{perfil.Id}", perfil);
});

// FUNCIONALIDADES
app.MapGet("/funcionalidades", async (HotelDbContext db) => await db.Funcionalidades.ToListAsync());

string GenerateJwtToken(Usuario user)
{
    var tokenHandler = new JwtSecurityTokenHandler();
    var key = Encoding.ASCII.GetBytes(builder.Configuration["JWT_SECRET_KEY"] ?? "chave_temporaria_123456789");
    
    var claims = new List<Claim>
    {
        new Claim(ClaimTypes.Name, user.Email),
        new Claim("Perfil", user.Perfil.Nome)
    };

    // Adiciona as funcionalidades como Claims
    foreach (var func in user.Perfil.Funcionalidades)
    {
        claims.Add(new Claim("Permissao", func.Nome));
    }

    var tokenDescriptor = new SecurityTokenDescriptor
    {
        Subject = new ClaimsIdentity(claims),
        Expires = DateTime.UtcNow.AddHours(8),
        SigningCredentials = new SigningCredentials(new SymmetricSecurityKey(key), SecurityAlgorithms.HmacSha256Signature)
    };

    var token = tokenHandler.CreateToken(tokenDescriptor);
    return tokenHandler.WriteToken(token);
}





app.Run();