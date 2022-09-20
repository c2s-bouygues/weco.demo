using System.Text.Json.Serialization;

namespace WeCo.ETL.TemperaturesToInfluxDB;

public class GasMeasure {

    [JsonPropertyName("devideId")]
    public string DeviceId { get; set; }

    [JsonPropertyName("deviceName")]
    public string DeviceName { get; set; }

    [JsonPropertyName("externalId")]
    public string ExternalId { get; set; }

    [JsonPropertyName("type")]
    public string Type { get; set; }

    [JsonPropertyName("value")]
    public double Value { get; set; }

    [JsonPropertyName("unit")]
    public string Unit { get; set; }

    [JsonPropertyName("timestamp")]
    public DateTime Timestamp { get; set; }
}