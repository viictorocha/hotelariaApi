namespace HotelariaApi.Domain;

public enum StatusQuarto { Disponivel = 0, Ocupado = 1, Limpeza = 2, Manutencao = 3 }
public enum TipoQuarto { Standard = 0, Luxo = 1, Suite = 2 }

public class Quarto
{
    public int Id { get; set; }
    public int PousadaId { get; set; }
    public Pousada Pousada { get; set; } = null!;
    public string Numero { get; set; } = string.Empty;
    public TipoQuarto Tipo { get; set; }
    public StatusQuarto Status { get; set; }
    public int Capacidade { get; set; }
    public decimal PrecoBase { get; set; }
}

public record QuartoCreateRequest(
    string Numero,
    TipoQuarto Tipo,
    StatusQuarto Status,
    int Capacidade,
    decimal PrecoBase);

public record QuartoUpdateRequest(
    string Numero,
    TipoQuarto Tipo,
    StatusQuarto Status,
    int Capacidade,
    decimal PrecoBase);