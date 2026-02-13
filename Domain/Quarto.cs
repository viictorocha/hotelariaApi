namespace HotelariaApi.Domain;

public class Quarto {
    public int Id { get; set; }
    public string Numero { get; set; } = string.Empty;
    public TipoQuarto Tipo { get; set; } = TipoQuarto.standard;
    public StatusQuarto Status { get; set; } = StatusQuarto.disponivel;
    public int Capacidade { get; set; }
    public decimal PrecoBase { get; set; } 
}