namespace MetroREX.Models;

/// <summary>Un cont. Toată clasa se salvează în fișierul JSON.</summary>
public class User
{
    public string Username { get; set; } = "";

    // Parola nu se salvează niciodată în clar
    public string PasswordHash { get; set; } = "";
    public string PasswordSalt { get; set; } = "";

    public UserProfile Profile { get; set; } = new();

    /// <summary>Id-urile produselor din wish list.</summary>
    public List<string> WishList { get; set; } = new();

    public List<Coupon> Coupons { get; set; } = new();
    public List<Order> Orders { get; set; } = new();
    public List<Notification> Notifications { get; set; } = new();
}
