using System.Text.Json.Serialization;

namespace WeCo.Ingesters.GasDataIngestion.Model;

public partial class Gas {

    [JsonPropertyName("deviceId")]
    public string DeviceId { get; set; }

    [JsonPropertyName("deviceName")]
    public string DeviceName { get; set; }

    [JsonPropertyName("category")]
    public string Category { get; set; }

    [JsonPropertyName("coord")]
    public Coordinates Coordinates { get; set; }

    [JsonPropertyName("measures")]
    public List<Measure> Measures { get; set; }

    [JsonPropertyName("wind")]
    public Wind Wind { get; set; }
}