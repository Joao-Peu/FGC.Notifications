using Azure.Messaging.ServiceBus;
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
        ServiceBusReceivedMessage message)
    {
        var paymentEvent = JsonSerializer.Deserialize<PaymentProcessedEvent>(
            message.Body.ToString(),
            new JsonSerializerOptions { PropertyNameCaseInsensitive = true });

        if (paymentEvent is null)
        {
            logger.LogWarning("[NotificationsAPI] Received null or invalid payment event");
            return Task.CompletedTask;
        }

        if (paymentEvent.Status == "Approved")
        {
            logger.LogInformation(
                "[NotificationsAPI] Purchase approved for UserId={UserId}, GameId={GameId}, Price={Price}",
                paymentEvent.UserId, paymentEvent.GameId, paymentEvent.Price);

            logger.LogInformation(
                "[NotificationsAPI] Purchase confirmation email sent to UserId={UserId}",
                paymentEvent.UserId);
        }
        else
        {
            logger.LogInformation(
                "[NotificationsAPI] Purchase rejected for UserId={UserId}, GameId={GameId}",
                paymentEvent.UserId, paymentEvent.GameId);
        }

        return Task.CompletedTask;
    }
}
