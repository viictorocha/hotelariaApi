using System.Text.Json.Serialization;
namespace HotelariaApi.Domain;

public class Usuario {
    public int Id { get; set; }
    public int ContaId { get; set; }
    public Conta Conta { get; set; } = null!;
    public bool EhDono { get; set; }
    public string Nome { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;

    [JsonPropertyName("senha")]
    public string SenhaHash { get; set; } = string.Empty;

    public List<UsuarioPousada> Pousadas { get; set; } = new();
}

public record LoginRequest(string Email, string Senha);
public record UsuarioCreateRequest(string Nome, string Email, string Senha, int PerfilId);
public record UsuarioUpdateRequest(string Nome, string Email, string? Senha, int PerfilId);

public class UsuarioPousada
{
    public int UsuarioId { get; set; }
    public Usuario Usuario { get; set; } = null!;
    public int PousadaId { get; set; }
    public Pousada Pousada { get; set; } = null!;
    public int PerfilId { get; set; }
    public Perfil Perfil { get; set; } = null!;
}