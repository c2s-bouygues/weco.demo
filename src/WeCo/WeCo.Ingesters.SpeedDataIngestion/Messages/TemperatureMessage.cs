using System.Text.Json.Serialization;
using WeCo.Ingesters.SpeedDataIngestion.Models;

namespace WeCo.Ingesters.SpeedDataIngestion.Messages;

public class TemperatureMessage {

    public TemperatureMessage(Speed speed) {
        this.DeviceId = speed.DeviceId;
        this.DeviceName = speed.DeviceName;

        this.Measures = speed.Temperatures.Select(v => new MeasureMessage {
            ExternalId = v.Id,
            Type = v.Type,
            Value = v.Value,
            Unit = v.Unit,
            Timestamp = v.Timestamp
        }).ToList();
    }

    [JsonPropertyName("devideId")]
    public string DeviceId { get; set; }

    [JsonPropertyName("deviceName")]
    public string DeviceName { get; set; }

    [JsonPropertyName("measures")]
    public List<MeasureMessage> Measures { get; set; }
}