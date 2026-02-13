using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);

// 1. Configuração do Banco de Dados (PostgreSQL)
var connectionString = builder.Configuration.GetConnectionString("DefaultConnection");
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