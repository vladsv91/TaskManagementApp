using TaskStatus = TaskManagementApp.Entities.TaskStatus;

namespace TaskManagementApp.ServiceBus.Messages;

public abstract class BaseMessage
{
    public Guid MessageId { get; set; } = Guid.NewGuid();
    public DateTime Timestamp { get; set; } = DateTime.UtcNow;
}

public class TaskCreatedMessage : BaseMessage
{
    public int TaskId { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public string? AssignedTo { get; set; }
}

public class TaskUpdatedMessage : BaseMessage
{
    public int TaskId { get; set; }
    public TaskStatus OldStatus { get; set; }
    public TaskStatus NewStatus { get; set; }
} 