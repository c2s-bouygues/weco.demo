namespace WeCo.API.Models;

public class Contact {
    public bool IsAlive { get; set; }
    public string FullName { get; set; } = string.Empty;

    public string BirthLocation { get; set; } = string.Empty;
    public DateTime BirthDay { get; set; }

    public string DeathLocation { get; set; } = string.Empty;
    public DateTime? DeathDay { get; set; } = null;
}