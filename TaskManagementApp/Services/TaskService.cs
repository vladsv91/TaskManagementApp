using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using TaskManagementApp.Data;
using TaskManagementApp.Entities;
using TaskManagementApp.Models;
using TaskManagementApp.ServiceBus;
using TaskManagementApp.ServiceBus.Messages;
using TaskStatus = TaskManagementApp.Entities.TaskStatus;

namespace TaskManagementApp.Services;

public interface ITaskService
{
    Task<IEnumerable<TaskItemVm>> GetAllTasksAsync();
    Task<TaskItemVm?> GetTaskByIdAsync(int id);
    Task<TaskItemVm> CreateTaskAsync(CreateTaskVm createTaskVm);
    Task<TaskItemVm?> UpdateTaskStatusAsync(int id, UpdateTaskStatusVm updateTaskVm);
}

public class TaskService(
    ApplicationDbContext dbContext,
    IServiceBusHandler serviceBusHandler,
    ILogger<TaskService> logger,
    IOptions<RabbitMqConfig> rabbitMqConfig)
    : ITaskService
{
    private readonly RabbitMqConfig _rabbitMqConfig = rabbitMqConfig.Value;

    public async Task<IEnumerable<TaskItemVm>> GetAllTasksAsync()
    {
        var tasks = await dbContext.Tasks.ToListAsync();

        return tasks.Select(MapToVm);
    }

    public async Task<TaskItemVm?> GetTaskByIdAsync(int id)
    {
        var task = await dbContext.Tasks.FindAsync(id);

        return task != null ? MapToVm(task) : null;
    }

    public async Task<TaskItemVm> CreateTaskAsync(CreateTaskVm createTaskVm)
    {
        var task = new TaskItem
        {
            Name = createTaskVm.Name,
            Description = createTaskVm.Description,
            AssignedTo = createTaskVm.AssignedTo,
            Status = TaskStatus.NotStarted,
            CreatedAt = DateTime.UtcNow
        };

        dbContext.Tasks.Add(task);
        await dbContext.SaveChangesAsync();

        var message = new TaskCreatedMessage
        {
            TaskId = task.Id,
            Name = task.Name,
            Description = task.Description,
            AssignedTo = task.AssignedTo
        };

        serviceBusHandler.SendMessage(_rabbitMqConfig.TaskCreatedQueueName, message);

        logger.LogInformation("Task created: {TaskId}", task.Id);

        return MapToVm(task);
    }

    public async Task<TaskItemVm?> UpdateTaskStatusAsync(int id, UpdateTaskStatusVm updateTaskVm)
    {
        var task = await dbContext.Tasks.FindAsync(id);
        if (task == null)
        {
            return null;
        }

        var oldStatus = task.Status;
        task.Status = updateTaskVm.NewStatus;
        task.UpdatedAt = DateTime.UtcNow;

        await dbContext.SaveChangesAsync();

        var message = new TaskUpdatedMessage
        {
            TaskId = task.Id,
            OldStatus = oldStatus,
            NewStatus = task.Status
        };

        serviceBusHandler.SendMessage(_rabbitMqConfig.TaskUpdatedQueueName, message);

        logger.LogInformation("Task updated: {TaskId}, Status: {Status}", task.Id, task.Status);

        return MapToVm(task);
    }

    private static TaskItemVm MapToVm(TaskItem taskItem)
    {
        return new TaskItemVm
        {
            Id = taskItem.Id,
            Name = taskItem.Name,
            Description = taskItem.Description,
            Status = taskItem.Status,
            AssignedTo = taskItem.AssignedTo,
            CreatedAt = taskItem.CreatedAt,
            UpdatedAt = taskItem.UpdatedAt
        };
    }
}