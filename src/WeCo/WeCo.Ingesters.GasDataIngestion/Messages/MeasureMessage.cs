using System.Text.Json.Serialization;
using WeCo.Ingesters.GasDataIngestion.Model;

namespace WeCo.Ingesters.GasDataIngestion.Messages;

public class MeasureMessage {

    public MeasureMessage(Gas gas, Measure measure) {
        this.DeviceId = gas.DeviceId;
        this.DeviceName = gas.DeviceName;

        this.ExternalId = measure.Id;
        this.Type = measure.Type;
        this.Value = measure.Value;
        this.Unit = measure.Unit;
        this.Timestamp = measure.Timestamp;
    }

    [JsonPropertyName("devideId")]
    public string DeviceId { get; set; }

    [JsonPropertyName("deviceName")]
    public string DeviceName { get; set; }

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