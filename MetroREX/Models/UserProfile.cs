namespace MetroREX.Models;

public class UserProfile
{
    public string Name { get; set; } = "";
    public string Address { get; set; } = "";
    public string Email { get; set; } = "";
    public string Phone { get; set; } = "";

    public UserProfile Clone() => new()
    {
        Name = Name,
        Address = Address,
        Email = Email,
        Phone = Phone,
    };

    public UserProfile Normalized() => new()
    {
        Name = Name.Trim(),
        Address = Address.Trim(),
        Email = Email.Trim(),
        Phone = Phone.Trim(),
    };
}
