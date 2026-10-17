using System.IO;
using FinMind.Models;
using FinMind.Services;
using Microsoft.EntityFrameworkCore;

namespace FinMind.Data;

/// <summary>Контекст EF Core для локальної бази SQLite.</summary>
public class AppDbContext : DbContext
{
    public DbSet<Category> Categories => Set<Category>();
    public DbSet<Transaction> Transactions => Set<Transaction>();
    public DbSet<BudgetLimit> BudgetLimits => Set<BudgetLimit>();

    public static string DbPath { get; } = Path.Combine(AppPaths.Folder, "finmind.db");

    protected override void OnConfiguring(DbContextOptionsBuilder options)
        => options.UseSqlite($"Data Source={DbPath}");

    protected override void OnModelCreating(ModelBuilder mb)
    {
        mb.Entity<Category>(e =>
        {
            e.Property(c => c.Name).IsRequired().HasMaxLength(60);
            e.HasIndex(c => c.Name).IsUnique();
        });

        mb.Entity<Transaction>(e =>
        {
            e.Property(t => t.Amount).HasPrecision(18, 2);
            e.Property(t => t.Type).HasConversion<string>();   // зберігаємо "Income"/"Expense"
            e.Property(t => t.Description).HasMaxLength(300);
            e.HasIndex(t => t.Date);
            e.HasOne(t => t.Category).WithMany().HasForeignKey(t => t.CategoryId)
             .OnDelete(DeleteBehavior.Restrict);
        });

        mb.Entity<BudgetLimit>(e =>
        {
            e.Property(b => b.MonthlyLimit).HasPrecision(18, 2);
            e.HasOne(b => b.Category).WithMany().HasForeignKey(b => b.CategoryId);
        });
    }
}
