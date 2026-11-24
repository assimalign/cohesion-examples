using System;

namespace BookStore.Users;

public sealed class User
{
    public UserId Id { get; init; }

    public required string Email { get; set; }

    public string? DisplayName { get; set; }

    public DateTimeOffset CreatedAt { get; init; }
}
