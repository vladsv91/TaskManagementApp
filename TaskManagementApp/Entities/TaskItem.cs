using System.ComponentModel.DataAnnotations;
using Microsoft.EntityFrameworkCore;

namespace TaskManagementApp.Entities;

[PrimaryKey(nameof(Id))]
public class TaskItem
{
    [Key]
    public int Id { get; set; }

    [Required]
    [MaxLength(100)]
    public string Name { get; set; } = string.Empty;

    [MaxLength(500)]
    public string Description { get; set; } = string.Empty;

    [Required]
    public TaskStatus Status { get; set; } = TaskStatus.NotStarted;

    [MaxLength(100)]
    public string? AssignedTo { get; set; }

    [Required]
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public DateTime? UpdatedAt { get; set; }
}

public enum TaskStatus
{
    NotStarted = 1,
    InProgress = 2,
    Completed = 3,
}
