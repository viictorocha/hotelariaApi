namespace HotelariaApi.Domain;

public class Perfil {
    public int Id { get; set; }
    public string Nome { get; set; } = string.Empty; // Ex: "Admin", "Recepcionista"
    public List<Funcionalidade> Funcionalidades { get; set; } = new();
}