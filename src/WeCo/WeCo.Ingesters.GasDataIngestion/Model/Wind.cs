using System.Text.Json.Serialization;

namespace WeCo.Ingesters.GasDataIngestion.Model;

public class Wind {

    [JsonPropertyName("speed")]
    public double Speed { get; set; }

    [JsonPropertyName("deg")]
    public long Deg { get; set; }
}