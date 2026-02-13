using System.Text.Json.Serialization;

namespace HotelariaApi.Domain;

[JsonConverter(typeof(JsonStringEnumConverter))]
public enum StatusQuarto { disponivel, ocupado, limpeza, manutencao }

[JsonConverter(typeof(JsonStringEnumConverter))]
public enum TipoQuarto { standard, luxo, master, flat }