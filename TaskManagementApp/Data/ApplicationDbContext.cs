using Microsoft.EntityFrameworkCore;
using TaskManagementApp.Entities;

namespace TaskManagementApp.Data;

public class ApplicationDbContext(DbContextOptions<ApplicationDbContext> options) : DbContext(options)
{
    public DbSet<TaskItem> Tasks { get; set; } = null!;
}