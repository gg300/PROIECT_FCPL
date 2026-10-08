namespace MetroREX.Models;

public enum NotificationType
{
    PriceChanged,
    BackInStock,
    Info
}

public class Notification
{
    public NotificationType Type { get; set; }
    public string Message { get; set; } = "";
    public DateTime Date { get; set; }
    public bool IsRead { get; set; }
}
