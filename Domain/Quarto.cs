namespace HotelariaApi.Domain;

public enum StatusQuarto { Disponivel = 0, Ocupado = 1, Limpeza = 2, Manutencao = 3 }
public enum TipoQuarto { Standard = 0, Luxo = 1, Suite = 2 }

public class Quarto
{
    public int Id { get; set; }
    public string Numero { get; set; } = string.Empty;
    public TipoQuarto Tipo { get; set; }
    public StatusQuarto Status { get; set; }
    public int Capacidade { get; set; }
    public decimal PrecoBase { get; set; }
}