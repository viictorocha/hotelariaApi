namespace HotelariaApi.Domain;
public class Pousada
{
    public int Id { get; set; }
    public int ContaId { get; set; }
    public Conta Conta { get; set; } = null!;
    public string NomeFantasia { get; set; } = string.Empty;
    public string RazaoSocial { get; set; } = string.Empty;
    public string Cnpj { get; set; } = string.Empty;
    public string Telefone { get; set; } = string.Empty;
    public string Endereco { get; set; } = string.Empty;
    public string Cidade { get; set; } = string.Empty;
    public string CheckInPadrao { get; set; } = "14:00";
    public string CheckOutPadrao { get; set; } = "12:00";
    public List<Quarto> Quartos { get; set; } = new();
    public List<UsuarioPousada> Usuarios { get; set; } = new();
}

public record PousadaCreateRequest(
    string NomeFantasia,
    string RazaoSocial,
    string Cnpj,
    string Telefone,
    string Endereco,
    string Cidade,
    string CheckInPadrao,
    string CheckOutPadrao);

public record PousadaUpdateRequest(
    string NomeFantasia,
    string RazaoSocial,
    string Cnpj,
    string Telefone,
    string Endereco,
    string Cidade,
    string CheckInPadrao,
    string CheckOutPadrao);