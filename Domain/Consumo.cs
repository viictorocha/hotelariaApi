namespace HotelariaApi.Domain;
public class Consumo
{
    public int Id { get; set; }
    public int QuartoId { get; set; }
    public string Descricao { get; set; } = string.Empty;
    public decimal Valor { get; set; }
    public int Quantidade { get; set; } = 1;
    public DateTime DataLancamento { get; set; } = DateTime.Now;
}