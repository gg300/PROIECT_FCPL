using System.Text.Json.Serialization;

namespace MetroREX.Models;

/// <summary>Un produs din magazin. Se citește/scrie în fișierul Excel.</summary>
public class Product
{
    public string Id { get; set; } = "";
    public string Name { get; set; } = "";
    public string Category { get; set; } = "";

    /// <summary>Prețul de bază din Excel (nu se modifică niciodată de aplicație).</summary>
    public decimal BasePrice { get; set; }

    /// <summary>Prețul de azi: prețul de bază ±20%, schimbat în fiecare zi.</summary>
    public decimal CurrentPrice { get; set; }

    public int Quantity { get; set; }

    /// <summary>Stocul de ieri (pentru notificarea „produsul e din nou disponibil”).</summary>
    public int QuantityYesterday { get; set; }

    public string Description { get; set; } = "";
    public string Seller { get; set; } = "";
    public List<string> Colors { get; set; } = new();

    /// <summary>Specificațiile, afișate ca tabel cu două coloane (nume / valoare).</summary>
    public List<ProductSpec> Specs { get; set; } = new();

    // Rating-ul se păstrează ca sumă + număr de voturi, ca să putem actualiza media ușor
    public int RatingSum { get; set; }
    public int RatingCount { get; set; }

    [JsonIgnore]
    public double Rating => RatingCount == 0 ? 0 : (double)RatingSum / RatingCount;

    [JsonIgnore]
    public bool InStock => Quantity > 0;

    public void AddRating(int stars)
    {
        if (stars is < 1 or > 5) throw new ArgumentOutOfRangeException(nameof(stars), "Rating între 1 și 5.");
        RatingSum += stars;
        RatingCount++;
    }
}

/// <summary>Un rând din tabelul de specificații.</summary>
public class ProductSpec
{
    public string Name { get; set; } = "";
    public string Value { get; set; } = "";
}
