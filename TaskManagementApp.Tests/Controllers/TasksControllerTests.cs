using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Moq;
using TaskManagementApp.Controllers;
using TaskManagementApp.Models;
using TaskManagementApp.Services;
using Xunit;
using TaskStatus = TaskManagementApp.Entities.TaskStatus;

namespace TaskManagementApp.Tests.Controllers;

public class TasksControllerTests
{
    private readonly Mock<ITaskService> _mockTaskService;
    private readonly Mock<ILogger<TasksController>> _mockLogger;
    private readonly TasksController _controller;
    
    public TasksControllerTests()
    {
        _mockTaskService = new Mock<ITaskService>();
        _mockLogger = new Mock<ILogger<TasksController>>();
        _controller = new TasksController(_mockTaskService.Object, _mockLogger.Object);
    }
    
    [Fact]
    public async Task GetAllTasks_ShouldReturnOkResultWithTasks()
    {
        var tasks = new List<TaskItemVm>
        {
            new TaskItemVm 
            { 
                Id = 1, 
                Name = "Task 1", 
                Description = "Description 1",
                Status = TaskStatus.NotStarted,
                CreatedAt = DateTime.UtcNow
            },
            new TaskItemVm 
            { 
                Id = 2, 
                Name = "Task 2", 
                Description = "Description 2",
                Status = TaskStatus.InProgress,
                AssignedTo = "User 1",
                CreatedAt = DateTime.UtcNow
            }
        };
        
        _mockTaskService.Setup(s => s.GetAllTasksAsync())
            .ReturnsAsync(tasks);
        
        var result = await _controller.GetAllTasks();
        
        var okResult = Assert.IsType<OkObjectResult>(result);
        var returnedTasks = Assert.IsAssignableFrom<IEnumerable<TaskItemVm>>(okResult.Value);
        Assert.Equal(2, returnedTasks.Count());
    }
    
    [Fact]
    public async Task GetTaskById_WithValidId_ShouldReturnOkResultWithTask()
    {
        var task = new TaskItemVm 
        { 
            Id = 1, 
            Name = "Task 1", 
            Description = "Description 1",
            Status = TaskStatus.NotStarted,
            CreatedAt = DateTime.UtcNow
        };
        
        _mockTaskService.Setup(s => s.GetTaskByIdAsync(1))
            .ReturnsAsync(task);
        
        var result = await _controller.GetTaskById(1);
        
        var okResult = Assert.IsType<OkObjectResult>(result);
        var returnedTask = Assert.IsType<TaskItemVm>(okResult.Value);
        Assert.Equal(1, returnedTask.Id);
    }
    
    [Fact]
    public async Task GetTaskById_WithInvalidId_ShouldReturnNotFound()
    {
        _mockTaskService.Setup(s => s.GetTaskByIdAsync(99))
            .ReturnsAsync((TaskItemVm)null);
        
        var result = await _controller.GetTaskById(99);
        
        Assert.IsType<NotFoundResult>(result);
    }
    
    [Fact]
    public async Task CreateTask_WithValidData_ShouldReturnCreatedAtActionResult()
    {
        var createTaskVm = new CreateTaskVm
        {
            Name = "New Task",
            Description = "New Description",
            AssignedTo = "New User"
        };
        
        var createdTask = new TaskItemVm
        {
            Id = 3,
            Name = "New Task",
            Description = "New Description",
            AssignedTo = "New User",
            Status = TaskStatus.NotStarted,
            CreatedAt = DateTime.UtcNow
        };
        
        _mockTaskService.Setup(s => s.CreateTaskAsync(It.IsAny<CreateTaskVm>()))
            .ReturnsAsync(createdTask);
        
        var result = await _controller.CreateTask(createTaskVm);
        
        var createdAtActionResult = Assert.IsType<CreatedAtActionResult>(result);
        Assert.Equal(nameof(TasksController.GetTaskById), createdAtActionResult.ActionName);
        Assert.Equal(3, createdAtActionResult.RouteValues["id"]);
        var returnedTask = Assert.IsType<TaskItemVm>(createdAtActionResult.Value);
        Assert.Equal(3, returnedTask.Id);
    }
    
    [Fact]
    public async Task UpdateTaskStatus_WithValidIdAndStatus_ShouldReturnOkResultWithUpdatedTask()
    {
        var updateTaskVm = new UpdateTaskStatusVm
        {
            NewStatus = TaskStatus.Completed
        };
        
        var updatedTask = new TaskItemVm
        {
            Id = 1,
            Name = "Task 1",
            Description = "Description 1",
            Status = TaskStatus.Completed,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };
        
        _mockTaskService.Setup(s => s.UpdateTaskStatusAsync(1, It.IsAny<UpdateTaskStatusVm>()))
            .ReturnsAsync(updatedTask);
        
        var result = await _controller.UpdateTaskStatus(1, updateTaskVm);
        
        var okResult = Assert.IsType<OkObjectResult>(result);
        var returnedTask = Assert.IsType<TaskItemVm>(okResult.Value);
        Assert.Equal(1, returnedTask.Id);
        Assert.Equal(TaskStatus.Completed, returnedTask.Status);
    }
    
    [Fact]
    public async Task UpdateTaskStatus_WithInvalidId_ShouldReturnNotFound()
    {
        var updateTaskVm = new UpdateTaskStatusVm
        {
            NewStatus = TaskStatus.Completed
        };
        
        _mockTaskService.Setup(s => s.UpdateTaskStatusAsync(99, It.IsAny<UpdateTaskStatusVm>()))
            .ReturnsAsync((TaskItemVm)null);
        
        var result = await _controller.UpdateTaskStatus(99, updateTaskVm);
        
        Assert.IsType<NotFoundResult>(result);
    }
} 