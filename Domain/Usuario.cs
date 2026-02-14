using System.Text.Json.Serialization;
namespace HotelariaApi.Domain;

public class Usuario {
    public int Id { get; set; }
    public string Nome { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    [JsonIgnore]
    public string SenhaHash { get; set; } = string.Empty;
    public int PerfilId { get; set; }
    public Perfil Perfil { get; set; } = null!;
}

public record LoginRequest(string Email, string Senha);