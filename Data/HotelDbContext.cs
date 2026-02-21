using Microsoft.EntityFrameworkCore;
using HotelariaApi.Domain;

namespace HotelariaApi.Data;

public class HotelDbContext : DbContext {
    public HotelDbContext(DbContextOptions<HotelDbContext> options) : base(options) { }
    
    public DbSet<Pousada> Pousada { get; set; }
    public DbSet<Quarto> Quartos => Set<Quarto>();
    public DbSet<Usuario> Usuarios => Set<Usuario>();
    public DbSet<Perfil> Perfis => Set<Perfil>();
    public DbSet<Consumo> Consumos => Set<Consumo>();
    public DbSet<Funcionalidade> Funcionalidades => Set<Funcionalidade>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        var funcs = new List<Funcionalidade>
        {
            new Funcionalidade { Id = 1, Nome = "Dashboard", Descricao = "Visualizar indicadores e métricas" },
            new Funcionalidade { Id = 2, Nome = "MapaQuartos", Descricao = "Visualizar status e gerenciar quartos" },
            new Funcionalidade { Id = 3, Nome = "Reservas", Descricao = "Gerenciar lista e detalhes de reservas" },
            new Funcionalidade { Id = 4, Nome = "Consumo", Descricao = "Lançar e gerenciar itens de consumo" },
            new Funcionalidade { Id = 5, Nome = "Financeiro", Descricao = "Acesso a contas e faturamento" },
            new Funcionalidade { Id = 6, Nome = "Configuracoes", Descricao = "Acesso a configurações do sistema e perfis" }
        };

        modelBuilder.Entity<Funcionalidade>().HasData(funcs);

        modelBuilder.Entity<Perfil>().HasData(new Perfil { Id = 1, Nome = "Admin" });
        
        modelBuilder.Entity("FuncionalidadePerfil").HasData(
                new { FuncionalidadesId = 1, PerfisId = 1 },
                new { FuncionalidadesId = 2, PerfisId = 1 },
                new { FuncionalidadesId = 3, PerfisId = 1 },
                new { FuncionalidadesId = 4, PerfisId = 1 },
                new { FuncionalidadesId = 5, PerfisId = 1 },
                new { FuncionalidadesId = 6, PerfisId = 1 }
            );

        // Configurações de conversão (Mapping)
        modelBuilder.Entity<Quarto>(entity => {
            entity.Property(e => e.Tipo).HasConversion<string>();
            entity.Property(e => e.Status).HasConversion<string>();
        });
    }
}