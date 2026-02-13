namespace HotelariaApi.Domain;

public class Usuario {
    public int Id { get; set; }
    public string Email { get; set; } = string.Empty;
    public string SenhaHash { get; set; } = string.Empty;
    public int PerfilId { get; set; }
    public Perfil Perfil { get; set; } = null!;
}