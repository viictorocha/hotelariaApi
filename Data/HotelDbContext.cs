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
        var funcs = new List<Funcionalidade>
        {
            new Funcionalidade { Id = 1, Nome = "DASHBOARD", Descricao = "Visualizar indicadores e métricas" },
            new Funcionalidade { Id = 2, Nome = "MAPA_QUARTOS", Descricao = "Visualizar status e gerenciar quartos" },
            new Funcionalidade { Id = 3, Nome = "RESERVAS", Descricao = "Gerenciar lista e detalhes de reservas" },
            new Funcionalidade { Id = 4, Nome = "CONSUMO", Descricao = "Lançar e gerenciar itens de consumo" },
            new Funcionalidade { Id = 5, Nome = "FINANCEIRO", Descricao = "Acesso a contas e faturamento" },
            new Funcionalidade { Id = 6, Nome = "CONFIGURACOES", Descricao = "Acesso a configurações do sistema e perfis" }
        };

        modelBuilder.Entity<Funcionalidade>().HasData(funcs);

        // 2. Criar Perfil Admin e associar a TODAS as funcionalidades
        modelBuilder.Entity<Perfil>().HasData(new Perfil { Id = 1, Nome = "Admin" });

        // Tabela intermediária (Many-to-Many)
        // Se você estiver usando o mapeamento padrão do EF para Many-to-Many:
        // foreach (var f in funcs)
        // {
        //     modelBuilder.Entity("FuncionalidadePerfil").HasData(
        //         new { FuncionalidadesId = f.Id, PerfisId = 1 }
        //     );
        // }

        // 3. Usuário inicial
        modelBuilder.Entity<Usuario>().HasData(new Usuario
        {
            Id = 1,
            Email = "admin@hotel.com",
            SenhaHash = "123456", // Lembre-se de usar Hash em produção
            PerfilId = 1
        });
        
        // Configurações de conversão (Mapping)
        modelBuilder.Entity<Quarto>(entity => {
            entity.Property(e => e.Tipo).HasConversion<string>();
            entity.Property(e => e.Status).HasConversion<string>();
        });
    }
}