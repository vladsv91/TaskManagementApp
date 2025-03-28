using System.Text;
using System.Net.Sockets;
using Microsoft.Extensions.Options;
using Newtonsoft.Json;
using Polly;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;
using RabbitMQ.Client.Exceptions;

namespace TaskManagementApp.ServiceBus;

public interface IServiceBusHandler
{
    void SendMessage<T>(string queueName, T message) where T : class;
    void SubscribeToQueue<T>(string queueName, Func<T, Task> handler) where T : class;
}

public class ServiceBusHandler : IServiceBusHandler, IDisposable
{
    private readonly RabbitMqConfig _config;
    private readonly ILogger<ServiceBusHandler> _logger;
    private readonly IConnection _connection;
    private readonly IModel _channel;
    private bool _disposed;

    public ServiceBusHandler(IOptions<RabbitMqConfig> config, ILogger<ServiceBusHandler> logger)
    {
        _config = config.Value;
        _logger = logger;

        // Create retry policy
        Policy
            .Handle<BrokerUnreachableException>()
            .Or<SocketException>()
            .Or<AlreadyClosedException>()
            .WaitAndRetryAsync(
                _config.RetryCount,
                retryAttempt =>
                    TimeSpan.FromMilliseconds(_config.RetryIntervalMs *
                                              Math.Pow(2, retryAttempt - 1)), // Exponential backoff delay
                (exception, timeSpan, retryCount, context) =>
                {
                    _logger.LogWarning(exception,
                        "RabbitMQ connection attempt {RetryCount} failed after {TimeSpan}ms. Retrying...",
                        retryCount, timeSpan.TotalMilliseconds);
                });

        var factory = new ConnectionFactory
        {
            HostName = _config.HostName,
            Port = _config.Port,
            UserName = _config.UserName,
            Password = _config.Password,
            VirtualHost = _config.VirtualHost,
            DispatchConsumersAsync = true
        };

        try
        {
            _connection = factory.CreateConnection();
            _channel = _connection.CreateModel();
            _logger.LogInformation("Successfully connected to RabbitMQ");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to establish connection to RabbitMQ");
            throw;
        }
    }

    public void SendMessage<T>(string queueName, T message) where T : class
    {
        try
        {
            // Ensure queue exists
            _channel.QueueDeclare(
                queue: queueName,
                durable: true,
                exclusive: false,
                autoDelete: false);

            var json = JsonConvert.SerializeObject(message);
            var body = Encoding.UTF8.GetBytes(json);

            var properties = _channel.CreateBasicProperties();
            properties.Persistent = true;

            _channel.BasicPublish(
                exchange: string.Empty,
                routingKey: queueName,
                basicProperties: properties,
                body: body);

            _logger.LogInformation("Message sent to queue {QueueName}: {Message}", queueName, json);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error sending message to queue {QueueName}", queueName);
            throw;
        }
    }

    public void SubscribeToQueue<T>(string queueName, Func<T, Task> handler) where T : class
    {
        try
        {
            // Ensure queue exists
            _channel.QueueDeclare(
                queue: queueName,
                durable: true,
                exclusive: false,
                autoDelete: false);

            _channel.BasicQos(
                prefetchSize: 0,
                prefetchCount: 1,
                global: false);

            var consumer = new AsyncEventingBasicConsumer(_channel);

            consumer.Received += async (sender, args) =>
            {
                try
                {
                    var body = args.Body.ToArray();
                    var json = Encoding.UTF8.GetString(body);

                    _logger.LogInformation("Message received from queue {QueueName}: {Message}", queueName, json);

                    var message = JsonConvert.DeserializeObject<T>(json);
                    if (message != null)
                    {
                        await handler(message);
                        _channel.BasicAck(args.DeliveryTag, false);
                    }
                    else
                    {
                        _logger.LogWarning("Received null message from deserialization");
                        _channel.BasicNack(args.DeliveryTag, false, false);
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error processing message from queue {QueueName}", queueName);
                    _channel.BasicNack(args.DeliveryTag, false, true);
                }
            };

            _channel.BasicConsume(
                queue: queueName,
                autoAck: false,
                consumer: consumer);

            _logger.LogInformation("Successfully subscribed to queue {QueueName}", queueName);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error subscribing to queue {QueueName}", queueName);
            throw;
        }
    }

    public void Dispose()
    {
        Dispose(true);
        GC.SuppressFinalize(this);
    }

    protected virtual void Dispose(bool disposing)
    {
        if (_disposed)
            return;

        if (disposing)
        {
            try
            {
                if (_channel != null && _channel.IsOpen)
                {
                    _channel.Close();
                    _channel.Dispose();
                }

                if (_connection != null && _connection.IsOpen)
                {
                    _connection.Close();
                    _connection.Dispose();
                }

                _logger.LogInformation("RabbitMQ connection closed");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error disposing RabbitMQ connections");
            }
        }

        _disposed = true;
    }
}