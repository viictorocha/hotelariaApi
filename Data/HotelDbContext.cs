using Microsoft.EntityFrameworkCore;
using HotelariaApi.Domain;

namespace HotelariaApi.Data;

public class HotelDbContext : DbContext {
    public HotelDbContext(DbContextOptions<HotelDbContext> options) : base(options) { }
    
    public DbSet<Quarto> Quartos => Set<Quarto>();
    public DbSet<Usuario> Usuarios => Set<Usuario>();
    public DbSet<Perfil> Perfis => Set<Perfil>();
    public DbSet<Funcionalidade> Funcionalidades => Set<Funcionalidade>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        // Configurações de conversão (Mapping)
        modelBuilder.Entity<Quarto>(entity => {
            entity.Property(e => e.Tipo).HasConversion<string>();
            entity.Property(e => e.Status).HasConversion<string>();
        });
    }
}