using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);

// 1. Configuração do Banco de Dados (PostgreSQL)
var connectionString = builder.Configuration.GetConnectionString("DefaultConnection");

if (connectionString != null && (connectionString.StartsWith("postgres://") || connectionString.StartsWith("postgresql://")))
{
    var databaseUri = new Uri(connectionString);
    var userInfo = databaseUri.UserInfo.Split(':');
    
    // Se a porta for -1 (não especificada na URL), usamos a 5432 por padrão
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

// Adicionar Quarto
app.MapPost("/quartos", async (Quarto quarto, HotelDbContext db) => {
    db.Quartos.Add(quarto);
    await db.SaveChangesAsync();
    return Results.Created($"/quartos/{quarto.Id}", quarto);
});

app.Run();

// 3. Modelos e Contexto
public class Quarto {
    public int Id { get; set; }
    public string Numero { get; set; } = string.Empty;
    public string Tipo { get; set; } = string.Empty;
    public decimal PrecoDiaria { get; set; }
    public bool EstaOcupado { get; set; }
}

class HotelDbContext : DbContext {
    public HotelDbContext(DbContextOptions<HotelDbContext> options) : base(options) { }
    public DbSet<Quarto> Quartos => Set<Quarto>();
}