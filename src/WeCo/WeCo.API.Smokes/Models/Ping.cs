namespace WeCo.API.Smokes.Models;

public class Ping {
    public string Location { get; set; } = string.Empty;
    public DateTime TimeStamp { get; set; } = DateTime.MinValue;
}