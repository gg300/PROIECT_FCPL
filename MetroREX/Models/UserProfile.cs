namespace MetroREX.Models;

/// <summary>Datele de profil cerute la crearea contului (și la checkout pentru vizitatori).</summary>
public class UserProfile
{
    public string Name { get; set; } = "";
    public string Address { get; set; } = "";
    public string Email { get; set; } = "";
    public string Phone { get; set; } = "";
}
