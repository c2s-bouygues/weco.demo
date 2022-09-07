namespace WeCo.API.Noises.Models;

public class NoiseSensor {
    public bool IsActive { get; set; }
    public string SensorId { get; set; } = string.Empty;

    public string LastLocation { get; set; } = string.Empty;
    public DateTime LastPing { get; set; }

    public string AlertLocation { get; set; } = string.Empty;
    public DateTime? AlertTimestamp { get; set; } = null;
}