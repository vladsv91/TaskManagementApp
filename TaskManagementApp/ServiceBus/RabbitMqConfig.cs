namespace TaskManagementApp.ServiceBus;

public class RabbitMqConfig
{
    public required string HostName { get; set; }
    public int Port { get; set; }
    public required string UserName { get; set; }
    public required string Password { get; set; }
    public required string VirtualHost { get; set; }
    public required string TaskCreatedQueueName { get; set; }
    public required string TaskUpdatedQueueName { get; set; }
    public int RetryCount { get; set; } = 5;
    public int RetryIntervalMs { get; set; } = 1000;
}