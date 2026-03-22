namespace Shared.Events;

public record PaymentProcessedEvent(string OrderId, string UserId, string GameId, decimal Price, string Status);