using Microsoft.AspNetCore.Mvc;
using TaskManagementApp.Models;
using TaskManagementApp.Services;

namespace TaskManagementApp.Controllers;

[ApiController]
[Route("api/[controller]")]
public class TasksController : ControllerBase
{
    private readonly ITaskService _taskService;
    private readonly ILogger<TasksController> _logger;

    public TasksController(ITaskService taskService, ILogger<TasksController> logger)
    {
        _taskService = taskService;
        _logger = logger;
    }

    [HttpGet]
    [ProducesResponseType(typeof(IEnumerable<TaskItemVm>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetAllTasks()
    {
        _logger.LogInformation("Getting all tasks");

        var tasks = await _taskService.GetAllTasksAsync();

        return Ok(tasks);
    }

    [HttpGet("{id:int}")]
    [ProducesResponseType(typeof(TaskItemVm), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetTaskById(int id)
    {
        _logger.LogInformation("Getting task by ID: {Id}", id);

        var task = await _taskService.GetTaskByIdAsync(id);

        if (task == null)
        {
            return NotFound();
        }

        return Ok(task);
    }

    [HttpPost]
    [ProducesResponseType(typeof(TaskItemVm), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> CreateTask([FromBody] CreateTaskVm createTaskVm)
    {
        if (!ModelState.IsValid)
        {
            return BadRequest(ModelState);
        }

        _logger.LogInformation("Creating new task: {TaskName}", createTaskVm.Name);

        var createdTask = await _taskService.CreateTaskAsync(createTaskVm);

        return CreatedAtAction(nameof(GetTaskById), new { id = createdTask.Id }, createdTask);
    }

    [HttpPut("{id:int}/status")]
    [ProducesResponseType(typeof(TaskItemVm), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> UpdateTaskStatus(int id, [FromBody] UpdateTaskStatusVm updateTaskVm)
    {
        if (!ModelState.IsValid)
        {
            return BadRequest(ModelState);
        }

        _logger.LogInformation("Updating task status: {Id} to {Status}", id, updateTaskVm.NewStatus);

        var updatedTask = await _taskService.UpdateTaskStatusAsync(id, updateTaskVm);

        if (updatedTask == null)
        {
            return NotFound();
        }

        return Ok(updatedTask);
    }
}