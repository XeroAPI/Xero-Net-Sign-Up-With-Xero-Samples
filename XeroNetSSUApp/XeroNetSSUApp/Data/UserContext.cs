using Microsoft.EntityFrameworkCore;
using XeroNetSSUApp.Models;

public class UserContext : DbContext
{
  public UserContext()
  {
  }

  public UserContext(DbContextOptions<UserContext> options) : base(options)
  {

  }

  protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
  {
  }

  protected override void OnModelCreating(ModelBuilder modelBuilder)
  {
    base.OnModelCreating(modelBuilder);
  }

  public DbSet<User> User { get; set; }

}

