using System.Text.Json.Serialization;

namespace WeCo.Ingesters.GasDataIngestion.Model;

public class Measure {

    [JsonPropertyName("id")]
    public string Id { get; set; }

    [JsonPropertyName("type")]
    public string Type { get; set; }

    [JsonPropertyName("value")]
    public double Value { get; set; }

    [JsonPropertyName("unit")]
    public string Unit { get; set; }

    [JsonPropertyName("timestamp")]
    public DateTime Timestamp { get; set; }
}