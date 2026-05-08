using System.Text;
using System.Text.Json;
using Exodus.Models.Events;
using Exodus.Services.Email;
using Microsoft.Extensions.Options;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;

namespace Exodus.Infrastructure.Messaging;

public class PaymentSuccessConsumer : BackgroundService
{
    private readonly RabbitMQSettings _settings;
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly ILogger<PaymentSuccessConsumer> _logger;

    public PaymentSuccessConsumer(
        IOptions<RabbitMQSettings> settings,
        IServiceScopeFactory scopeFactory,
        ILogger<PaymentSuccessConsumer> logger)
    {
        _settings = settings.Value;
        _scopeFactory = scopeFactory;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("PaymentSuccessConsumer starting...");

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                await ConsumeAsync(stoppingToken);
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
            {
                break;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "PaymentSuccessConsumer connection failed, retrying in 5 seconds...");
                await Task.Delay(TimeSpan.FromSeconds(5), stoppingToken);
            }
        }
    }

    private async Task ConsumeAsync(CancellationToken stoppingToken)
    {
        var factory = new ConnectionFactory
        {
            HostName = _settings.Host,
            Port = _settings.Port,
            UserName = _settings.Username,
            Password = _settings.Password
        };

        await using var connection = await factory.CreateConnectionAsync(stoppingToken);
        await using var channel = await connection.CreateChannelAsync(cancellationToken: stoppingToken);

        await channel.QueueDeclareAsync(
            queue: _settings.PaymentQueueName,
            durable: true,
            exclusive: false,
            autoDelete: false,
            arguments: null,
            cancellationToken: stoppingToken);

        await channel.BasicQosAsync(prefetchSize: 0, prefetchCount: 1, global: false, cancellationToken: stoppingToken);

        var consumer = new AsyncEventingBasicConsumer(channel);
        consumer.ReceivedAsync += async (_, ea) =>
        {
            try
            {
                var body = Encoding.UTF8.GetString(ea.Body.ToArray());
                var paymentEvent = JsonSerializer.Deserialize<PaymentSuccessEvent>(body);

                if (paymentEvent != null)
                {
                    using var scope = _scopeFactory.CreateScope();
                    var emailService = scope.ServiceProvider.GetRequiredService<IEmailService>();

                    await emailService.SendPaymentSuccessAsync(
                        paymentEvent.UserEmail,
                        paymentEvent.OrderId,
                        paymentEvent.Amount,
                        paymentEvent.Currency);

                    _logger.LogInformation(
                        "Payment success email sent for OrderId={OrderId}, Email={Email}",
                        paymentEvent.OrderId, paymentEvent.UserEmail);
                }

                await channel.BasicAckAsync(ea.DeliveryTag, multiple: false);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to process payment success message");
                await channel.BasicNackAsync(ea.DeliveryTag, multiple: false, requeue: true);
            }
        };

        await channel.BasicConsumeAsync(
            queue: _settings.PaymentQueueName,
            autoAck: false,
            consumer: consumer,
            cancellationToken: stoppingToken);

        _logger.LogInformation("PaymentSuccessConsumer listening on queue: {Queue}", _settings.PaymentQueueName);

        await Task.Delay(Timeout.Infinite, stoppingToken);
    }
}
