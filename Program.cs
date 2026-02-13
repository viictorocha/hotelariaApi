using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);

// 1. Configuração do Banco de Dados (PostgreSQL)
var connectionString = builder.Configuration.GetConnectionString("DefaultConnection");

if (connectionString != null && (connectionString.StartsWith("postgres://") || connectionString.StartsWith("postgresql://")))
{
    var databaseUri = new Uri(connectionString);
    var userInfo = databaseUri.UserInfo.Split(':');
    int port = databaseUri.Port == -1 ? 5432 : databaseUri.Port;

    connectionString = $"Host={databaseUri.Host};" +
                       $"Port={port};" +
                       $"Database={databaseUri.AbsolutePath.TrimStart('/')};" +
                       $"Username={userInfo[0]};" +
                       $"Password={userInfo[1]};" +
                       "SSL Mode=Require;" +
                       "Trust Server Certificate=true;";
}

builder.Services.AddDbContext<HotelDbContext>(options =>
    options.UseNpgsql(connectionString));

builder.Services.AddDbContext<HotelDbContext>(options =>
    options.UseNpgsql(connectionString));

var app = builder.Build();

// 2. Endpoints (Rotas)

// Home de boas-vindas
app.MapGet("/", () => "HotelariaPro API v1 - Online");

// Listar Quartos
app.MapGet("/quartos", async (HotelDbContext db) => 
    await db.Quartos.ToListAsync());

app.MapPost("/quartos", async (Quarto quarto, HotelDbContext db) => {
    db.Quartos.Add(quarto);
    await db.SaveChangesAsync();
    return Results.Created($"/quartos/{quarto.Id}", quarto);
});

app.Run();