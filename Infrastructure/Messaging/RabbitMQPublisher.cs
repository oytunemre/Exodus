using System.Text;
using System.Text.Json;
using Exodus.Models.Events;
using Microsoft.Extensions.Options;
using RabbitMQ.Client;

namespace Exodus.Infrastructure.Messaging;

public interface IRabbitMQPublisher : IAsyncDisposable
{
    Task PublishPaymentSuccessAsync(PaymentSuccessEvent paymentEvent, CancellationToken ct = default);
}

public class RabbitMQPublisher : IRabbitMQPublisher
{
    private readonly RabbitMQSettings _settings;
    private readonly ILogger<RabbitMQPublisher> _logger;
    private IConnection? _connection;
    private IChannel? _channel;
    private readonly SemaphoreSlim _lock = new(1, 1);

    public RabbitMQPublisher(IOptions<RabbitMQSettings> settings, ILogger<RabbitMQPublisher> logger)
    {
        _settings = settings.Value;
        _logger = logger;
    }

    public async Task PublishPaymentSuccessAsync(PaymentSuccessEvent paymentEvent, CancellationToken ct = default)
    {
        try
        {
            var channel = await GetChannelAsync(ct);

            var body = Encoding.UTF8.GetBytes(JsonSerializer.Serialize(paymentEvent));
            var props = new BasicProperties
            {
                ContentType = "application/json",
                DeliveryMode = DeliveryModes.Persistent,
                MessageId = Guid.NewGuid().ToString(),
                Timestamp = new AmqpTimestamp(DateTimeOffset.UtcNow.ToUnixTimeSeconds())
            };

            await channel.BasicPublishAsync(
                exchange: string.Empty,
                routingKey: _settings.PaymentQueueName,
                mandatory: false,
                basicProperties: props,
                body: body,
                cancellationToken: ct);

            _logger.LogInformation("Published PaymentSuccessEvent for OrderId={OrderId}", paymentEvent.OrderId);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to publish PaymentSuccessEvent for OrderId={OrderId}", paymentEvent.OrderId);
            throw;
        }
    }

    private async Task<IChannel> GetChannelAsync(CancellationToken ct)
    {
        if (_channel is { IsOpen: true })
            return _channel;

        await _lock.WaitAsync(ct);
        try
        {
            if (_channel is { IsOpen: true })
                return _channel;

            if (_connection is null or { IsOpen: false })
            {
                var factory = new ConnectionFactory
                {
                    HostName = _settings.Host,
                    Port = _settings.Port,
                    UserName = _settings.Username,
                    Password = _settings.Password
                };
                _connection = await factory.CreateConnectionAsync(ct);
            }

            _channel = await _connection.CreateChannelAsync(
                new CreateChannelOptions { PublisherConfirmationsEnabled = true },
                ct);

            await _channel.QueueDeclareAsync(
                queue: _settings.PaymentQueueName,
                durable: true,
                exclusive: false,
                autoDelete: false,
                arguments: null,
                cancellationToken: ct);

            return _channel;
        }
        finally
        {
            _lock.Release();
        }
    }

    public async ValueTask DisposeAsync()
    {
        if (_channel is not null)
            await _channel.CloseAsync();
        if (_connection is not null)
            await _connection.CloseAsync();
        _lock.Dispose();
        GC.SuppressFinalize(this);
    }
}

public class RabbitMQSettings
{
    public string Host { get; set; } = "localhost";
    public int Port { get; set; } = 5672;
    public string Username { get; set; } = "guest";
    public string Password { get; set; } = "guest";
    public string PaymentQueueName { get; set; } = "payment.success";
}
