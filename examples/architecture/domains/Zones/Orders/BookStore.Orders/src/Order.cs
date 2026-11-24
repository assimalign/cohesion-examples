using System;

namespace BookStore.Orders;

public readonly record struct OrderId(Guid Value)
{
    public static OrderId New() => new(Guid.CreateVersion7());
}

public sealed class Order
{
    public OrderId Id { get; init; }

    /// <summary>The customer, a user id from the Users application. Orders stores the id and never the user.</summary>
    public Guid CustomerId { get; init; }

    public decimal Total { get; set; }

    public string Status { get; set; } = "Placed";

    public DateTimeOffset PlacedAt { get; init; }
}
