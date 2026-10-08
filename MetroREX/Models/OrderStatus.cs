namespace MetroREX.Models;

public enum OrderStatus
{
    /// <summary>Plasată, dar încă nelivrată (apare la „Comenzi active”).</summary>
    Active,

    /// <summary>Livrată (apare în „Istoric comenzi”).</summary>
    Delivered
}
