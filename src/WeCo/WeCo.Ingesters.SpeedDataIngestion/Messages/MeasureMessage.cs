using System.Text.Json.Serialization;

namespace WeCo.Ingesters.SpeedDataIngestion.Messages;

public class MeasureMessage {

    [JsonPropertyName("externalId")]
    public long ExternalId { get; set; }

    [JsonPropertyName("type")]
    public string Type { get; set; }

    [JsonPropertyName("value")]
    public double Value { get; set; }

    [JsonPropertyName("unit")]
    public string Unit { get; set; }

    [JsonPropertyName("timestamp")]
    public DateTime Timestamp { get; set; }
}