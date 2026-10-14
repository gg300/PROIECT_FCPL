namespace MetroREX.Models;

public class User
{
    public string Username { get; set; } = "";

    public string PasswordHash { get; set; } = "";
    public string PasswordSalt { get; set; } = "";

    public UserProfile Profile { get; set; } = new();

    public List<string> WishList { get; set; } = new();

    public List<string> Subscriptions { get; set; } = new();
    public List<Coupon> Coupons { get; set; } = new();
    public List<Order> Orders { get; set; } = new();
    public List<Notification> Notifications { get; set; } = new();

    public int LoyaltyPoints { get; set; }

    public int LifetimePoints { get; set; }

    public List<LoyaltyEntry> LoyaltyHistory { get; set; } = new();

    public List<ReturnRequest> Returns { get; set; } = new();
}
