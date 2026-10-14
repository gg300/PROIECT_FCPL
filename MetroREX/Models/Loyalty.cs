namespace MetroREX.Models;

public enum LoyaltyTier
{
    Bronz,
    Argint,
    Aur
}

public class LoyaltyEntry
{
    public DateTime Date { get; set; }

    public int Points { get; set; }

    public string Reason { get; set; } = "";

    public string? OrderId { get; set; }

    public bool IsOrderEarning { get; set; }
}
