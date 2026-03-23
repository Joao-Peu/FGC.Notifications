using Microsoft.Azure.Functions.Worker;
using Microsoft.Extensions.Logging;
using Shared.Events;
using System.Text.Json;

namespace NotificationsAPI.Functions;

public class PaymentProcessedFunction(ILogger<PaymentProcessedFunction> logger)
{
    [Function(nameof(PaymentProcessedFunction))]
    public Task Run(
        [ServiceBusTrigger("notifications-payment-processed", Connection = "SERVICEBUS_CONNECTION")]
        string message,
        FunctionContext context)
    {
        var bindingData = context.BindingContext.BindingData;
        var correlationId = bindingData.TryGetValue("CorrelationId", out var cid) && !string.IsNullOrEmpty(cid?.ToString())
            ? cid.ToString()!
            : Guid.NewGuid().ToString();

        using var logScope = logger.BeginScope(new Dictionary<string, object> { ["CorrelationId"] = correlationId });

        var paymentEvent = JsonSerializer.Deserialize<PaymentProcessedEvent>(
            message,
            new JsonSerializerOptions { PropertyNameCaseInsensitive = true });

        if (paymentEvent is null)
        {
            logger.LogWarning("[NotificationsAPI] Received null or invalid payment event");
            return Task.CompletedTask;
        }

        if (paymentEvent.Status == "Approved")
        {
            logger.LogInformation(
                "[NotificationsAPI] Purchase approved for UserId={UserId}, GameId={GameId}, Price={Price}, CorrelationId={CorrelationId}",
                paymentEvent.UserId, paymentEvent.GameId, paymentEvent.Price, correlationId);

            logger.LogInformation(
                "[NotificationsAPI] Purchase confirmation email sent to UserId={UserId}",
                paymentEvent.UserId);
        }
        else
        {
            logger.LogInformation(
                "[NotificationsAPI] Purchase rejected for UserId={UserId}, GameId={GameId}, CorrelationId={CorrelationId}",
                paymentEvent.UserId, paymentEvent.GameId, correlationId);
        }

        return Task.CompletedTask;
    }
}
