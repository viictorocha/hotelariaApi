namespace HotelariaApi.Domain;

public class Conta
{
    public int Id { get; set; }
    public string Nome { get; set; } = string.Empty;
    public List<Pousada> Pousadas { get; set; } = new();
}