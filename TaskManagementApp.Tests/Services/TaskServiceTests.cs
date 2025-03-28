using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Moq;
using TaskManagementApp.Data;
using TaskManagementApp.Entities;
using TaskManagementApp.Models;
using TaskManagementApp.ServiceBus;
using TaskManagementApp.ServiceBus.Messages;
using TaskManagementApp.Services;
using Xunit;
using TaskStatus = TaskManagementApp.Entities.TaskStatus;

namespace TaskManagementApp.Tests.Services;

public class TaskServiceTests
{
    private readonly Mock<IServiceBusHandler> _mockServiceBusHandler;
    private readonly Mock<ILogger<TaskService>> _mockLogger;
    private readonly Mock<IOptions<RabbitMqConfig>> _mockOptions;
    private readonly ApplicationDbContext _dbContext;
    private readonly RabbitMqConfig _config;
    private readonly ITaskService _taskService;

    public TaskServiceTests()
    {
        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;

        _dbContext = new ApplicationDbContext(options);
        
        _mockServiceBusHandler = new Mock<IServiceBusHandler>();
        _mockLogger = new Mock<ILogger<TaskService>>();
        
        _config = new RabbitMqConfig
        {
            TaskCreatedQueueName = "test-task-created",
            TaskUpdatedQueueName = "test-task-updated",
            HostName = null!,
            UserName = null!,
            Password = null!,
            VirtualHost = null!,

        };
        
        _mockOptions = new Mock<IOptions<RabbitMqConfig>>();
        _mockOptions.Setup(o => o.Value).Returns(_config);
        
        _taskService = new TaskService(
            _dbContext,
            _mockServiceBusHandler.Object,
            _mockLogger.Object,
            _mockOptions.Object);
            
        SeedDatabase();
    }
    
    private void SeedDatabase()
    {
        _dbContext.Tasks.AddRange(
            new TaskItem()
            {
                Id = 1,
                Name = "Task 1",
                Description = "Description 1",
                Status = TaskStatus.NotStarted,
                CreatedAt = DateTime.UtcNow.AddDays(-1)
            },
            new TaskItem
            {
                Id = 2,
                Name = "Task 2",
                Description = "Description 2",
                Status = TaskStatus.InProgress,
                AssignedTo = "User 1",
                CreatedAt = DateTime.UtcNow.AddDays(-2)
            });
            
        _dbContext.SaveChanges();
    }
    
    [Fact]
    public async Task GetAllTasksAsync_ShouldReturnAllTasks()
    {
        var result = await _taskService.GetAllTasksAsync();
        
        var tasks = result.ToList();
        Assert.Equal(2, tasks.Count);
        Assert.Contains(tasks, t => t.Id == 1);
        Assert.Contains(tasks, t => t.Id == 2);
    }
    
    [Fact]
    public async Task GetTaskByIdAsync_WithValidId_ShouldReturnTask()
    {
        var result = await _taskService.GetTaskByIdAsync(1);
        
        Assert.NotNull(result);
        Assert.Equal(1, result.Id);
        Assert.Equal("Task 1", result.Name);
    }
    
    [Fact]
    public async Task GetTaskByIdAsync_WithInvalidId_ShouldReturnNull()
    {
        var result = await _taskService.GetTaskByIdAsync(99);
        
        Assert.Null(result);
    }
    
    [Fact]
    public async Task CreateTaskAsync_ShouldCreateTaskAndSendMessage()
    {
        var createTaskVm = new CreateTaskVm
        {
            Name = "New Task",
            Description = "New Description",
            AssignedTo = "New User"
        };
        
        var result = await _taskService.CreateTaskAsync(createTaskVm);
        
        Assert.NotNull(result);
        Assert.Equal("New Task", result.Name);
        Assert.Equal("New Description", result.Description);
        Assert.Equal("New User", result.AssignedTo);
        Assert.Equal(TaskStatus.NotStarted, result.Status);
        
        _mockServiceBusHandler.Verify(
            x => x.SendMessage(
                _config.TaskCreatedQueueName,
                It.Is<object>(o => o.GetType() == typeof(TaskCreatedMessage))),
            Times.Once);
    }
    
    [Fact]
    public async Task UpdateTaskStatusAsync_WithValidId_ShouldUpdateStatusAndSendMessage()
    {
        var updateTaskVm = new UpdateTaskStatusVm
        {
            NewStatus = TaskStatus.Completed
        };
        
        var result = await _taskService.UpdateTaskStatusAsync(1, updateTaskVm);
        
        Assert.NotNull(result);
        Assert.Equal(1, result.Id);
        Assert.Equal(TaskStatus.Completed, result.Status);
        Assert.NotNull(result.UpdatedAt);
        
        _mockServiceBusHandler.Verify(
            x => x.SendMessage(
                _config.TaskUpdatedQueueName,
                It.Is<object>(o => o.GetType() == typeof(TaskUpdatedMessage))),
            Times.Once);
    }
    
    [Fact]
    public async Task UpdateTaskStatusAsync_WithInvalidId_ShouldReturnNull()
    {
        var updateTaskVm = new UpdateTaskStatusVm
        {
            NewStatus = TaskStatus.Completed
        };
        
        var result = await _taskService.UpdateTaskStatusAsync(99, updateTaskVm);
        
        Assert.Null(result);
        
        _mockServiceBusHandler.Verify(
            x => x.SendMessage(It.IsAny<string>(), It.IsAny<object>()),
            Times.Never);
    }
} 