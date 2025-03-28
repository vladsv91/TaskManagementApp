using Microsoft.Extensions.Options;
using TaskManagementApp.ServiceBus.Messages;

namespace TaskManagementApp.ServiceBus;

public class TaskMessageProcessor(
    IServiceBusHandler serviceBusHandler,
    ILogger<TaskMessageProcessor> logger,
    IOptions<RabbitMqConfig> rabbitMqConfig) : BackgroundService
{
    private readonly RabbitMqConfig _rabbitMqConfig = rabbitMqConfig.Value;

    protected override Task ExecuteAsync(CancellationToken ct)
    {
        logger.LogInformation("Task Message Processor starting...");

        serviceBusHandler.SubscribeToQueue<TaskCreatedMessage>(
            _rabbitMqConfig.TaskCreatedQueueName,
            async message => await ProcessTaskCreatedMessage(message, ct));

        serviceBusHandler.SubscribeToQueue<TaskUpdatedMessage>(
            _rabbitMqConfig.TaskUpdatedQueueName,
            async message => await ProcessTaskUpdatedMessage(message, ct));

        return Task.CompletedTask;
    }

    private async Task ProcessTaskCreatedMessage(TaskCreatedMessage message, CancellationToken ct)
    {
        logger.LogInformation("Processing task created message: TaskId={TaskId}", message.TaskId);

        await Task.CompletedTask;
    }

    private async Task ProcessTaskUpdatedMessage(TaskUpdatedMessage message, CancellationToken ct)
    {
        logger.LogInformation(
            "Processing task updated message: TaskId={TaskId}, Status changed from {OldStatus} to {NewStatus}",
            message.TaskId, message.OldStatus, message.NewStatus);

        await Task.CompletedTask;
    }
}