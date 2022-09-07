namespace WeCo.API.Smokes.Models;

public class SmokeSensor {
    public bool IsActive { get; set; }
    public string SensorId { get; set; } = string.Empty;

    public string LastLocation { get; set; } = string.Empty;
    public DateTime LastPing { get; set; }

    public string AlertLocation { get; set; } = string.Empty;
    public DateTime? AlertTimestamp { get; set; } = null;
}