using System.Text.Json.Serialization;

namespace WeCo.Ingesters.GasDataIngestion.Model;

public class Coordinates {

    [JsonPropertyName("lon")]
    public double Longitude { get; set; }

    [JsonPropertyName("lat")]
    public double Latitude { get; set; }
}