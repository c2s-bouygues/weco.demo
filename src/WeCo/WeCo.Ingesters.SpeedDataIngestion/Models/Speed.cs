using System.Text.Json.Serialization;

namespace WeCo.Ingesters.SpeedDataIngestion.Models;

public class Speed {

    [JsonPropertyName("deviceId")]
    public string DeviceId { get; set; }

    [JsonPropertyName("deviceName")]
    public string DeviceName { get; set; }

    [JsonPropertyName("category")]
    public string Category { get; set; }

    [JsonPropertyName("coord")]
    public Coordinates Coordinates { get; set; }

    [JsonPropertyName("velocity")]
    public List<Measure> Velocity { get; set; }

    [JsonPropertyName("temperatures")]
    public List<Measure> Temperatures { get; set; }
}