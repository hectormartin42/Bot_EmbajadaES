using TelegramBot.Data.Entities;
using Microsoft.EntityFrameworkCore;

namespace TelegramBot.Data;

public class AppDbContext : DbContext
{
    public DbSet<AppUser> AppUsers { get; set; } = null!;
    public DbSet<AppMessage> AppMessages { get; set; } = null!;
    protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
    {
        string _toolPath = "/Users/Wo_0NDeR/Documents/NetCore_Projects/used_tools";
        optionsBuilder.UseSqlite($"Data Source={_toolPath}/app.db");
    }
}