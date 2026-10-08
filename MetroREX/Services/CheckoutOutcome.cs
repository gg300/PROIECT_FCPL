using MetroREX.Models;

namespace MetroREX.Services;

internal sealed record CheckoutOutcome(Order Order, bool EmailSent, string? EmailError);
