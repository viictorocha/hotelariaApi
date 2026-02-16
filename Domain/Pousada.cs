namespace HotelariaApi.Domain;
public class Pousada
{
    public int Id { get; set; }
    public string NomeFantasia { get; set; } = string.Empty;
    public string RazaoSocial { get; set; } = string.Empty;
    public string Cnpj { get; set; } = string.Empty;
    public string Telefone { get; set; } = string.Empty;
    public string Endereco { get; set; } = string.Empty;
    public string Cidade { get; set; } = string.Empty;
    public string CheckInPadrao { get; set; } = "14:00";
    public string CheckOutPadrao { get; set; } = "12:00";
}