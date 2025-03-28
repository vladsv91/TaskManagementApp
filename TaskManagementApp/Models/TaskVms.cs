using System.ComponentModel.DataAnnotations;
using TaskStatus = TaskManagementApp.Entities.TaskStatus;

namespace TaskManagementApp.Models;

public class CreateTaskVm
{
    [Required]
    [MaxLength(100)]
    public string Name { get; set; } = string.Empty;

    [MaxLength(500)]
    public string Description { get; set; } = string.Empty;

    [MaxLength(100)]
    public string? AssignedTo { get; set; }
}

public class UpdateTaskStatusVm
{
    [Required]
    // TODO: add enum value validation
    public TaskStatus NewStatus { get; set; }
}

public class TaskItemVm
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public TaskStatus Status { get; set; }
    public string? AssignedTo { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime? UpdatedAt { get; set; }
}