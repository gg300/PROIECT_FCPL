using System.Text.Json.Serialization;

namespace MetroREX.Models;

public enum ReturnStatus
{
    Requested,
    Approved,
    Completed
}

public enum RefundMethod
{
    Original,

    LoyaltyPoints
}

public class ReturnLine
{
    public string ProductId { get; set; } = "";
    public string ProductName { get; set; } = "";
    public decimal UnitPrice { get; set; }
    public int Quantity { get; set; }
    public string Reason { get; set; } = "";
}

public class ReturnRequest
{
    public string Id { get; set; } = Guid.NewGuid().ToString("N");

    [JsonIgnore]
    public string ShortId => "R-" + (Id.Length <= 6 ? Id : Id[..6]).ToUpperInvariant();

    public string OrderId { get; set; } = "";
    public string OrderShortId { get; set; } = "";

    public DateTime RequestDate { get; set; }
    public DateTime? ApprovedDate { get; set; }
    public DateTime? CompletedDate { get; set; }

    public ReturnStatus Status { get; set; } = ReturnStatus.Requested;
    public RefundMethod RefundMethod { get; set; }

    public List<ReturnLine> Lines { get; set; } = new();

    public decimal RefundAmount { get; set; }

    public int RefundPoints { get; set; }
}
