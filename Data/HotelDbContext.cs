using Microsoft.EntityFrameworkCore;
using HotelariaApi.Domain;

namespace HotelariaApi.Data;

public class HotelDbContext : DbContext {
    public HotelDbContext(DbContextOptions<HotelDbContext> options) : base(options) { }
    
    public DbSet<Conta> Contas => Set<Conta>();
    public DbSet<Pousada> Pousadas => Set<Pousada>();
    public DbSet<Quarto> Quartos => Set<Quarto>();
    public DbSet<Usuario> Usuarios => Set<Usuario>();
    public DbSet<UsuarioPousada> UsuariosPousadas => Set<UsuarioPousada>();
    public DbSet<Perfil> Perfis => Set<Perfil>();
    public DbSet<Consumo> Consumos => Set<Consumo>();
    public DbSet<Funcionalidade> Funcionalidades => Set<Funcionalidade>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<UsuarioPousada>()
            .HasKey(up => new { up.UsuarioId, up.PousadaId });

        modelBuilder.Entity<UsuarioPousada>()
            .HasOne(up => up.Usuario)
            .WithMany(u => u.Pousadas)
            .HasForeignKey(up => up.UsuarioId);

        modelBuilder.Entity<UsuarioPousada>()
            .HasOne(up => up.Pousada)
            .WithMany(p => p.Usuarios)
            .HasForeignKey(up => up.PousadaId);

        modelBuilder.Entity<UsuarioPousada>()
            .HasOne(up => up.Perfil)
            .WithMany()
            .HasForeignKey(up => up.PerfilId);

        modelBuilder.Entity<Pousada>()
            .HasOne(p => p.Conta)
            .WithMany(c => c.Pousadas)
            .HasForeignKey(p => p.ContaId);

        modelBuilder.Entity<Usuario>()
            .HasOne(u => u.Conta)
            .WithMany()
            .HasForeignKey(u => u.ContaId);

        modelBuilder.Entity<Quarto>()
            .HasOne(q => q.Pousada)
            .WithMany(p => p.Quartos)
            .HasForeignKey(q => q.PousadaId);

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