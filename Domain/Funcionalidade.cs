using System.Text.Json.Serialization;

namespace HotelariaApi.Domain;

public class Funcionalidade {
    public int Id { get; set; }
    public string Nome { get; set; } = string.Empty; // Ex: "GERAR_RELATORIO", "EDITAR_QUARTO"
    public string Descricao { get; set; } = string.Empty;

    [JsonIgnore]
    public List<Perfil> Perfis { get; set; } = new();
}