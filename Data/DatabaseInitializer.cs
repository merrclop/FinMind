using FinMind.Models;
using Microsoft.EntityFrameworkCore;

namespace FinMind.Data;

/// <summary>Створення БД при першому запуску та заповнення базовими категоріями.</summary>
public static class DatabaseInitializer
{
    public static async Task InitializeAsync()
    {
        await using var db = new AppDbContext();
        await db.Database.EnsureCreatedAsync();

        if (await db.Categories.AnyAsync()) return;

        db.Categories.AddRange(
            new Category { Name = "Їжа",       Icon = "🍔", ColorHex = "#F59E0B" },
            new Category { Name = "Транспорт", Icon = "🚌", ColorHex = "#3B82F6" },
            new Category { Name = "Житло",     Icon = "🏠", ColorHex = "#8B5CF6" },
            new Category { Name = "Розваги",   Icon = "🎮", ColorHex = "#EC4899" },
            new Category { Name = "Здоров'я",  Icon = "💊", ColorHex = "#10B981" },
            new Category { Name = "Зарплата",  Icon = "💼", ColorHex = "#14B8A6" },
            new Category { Name = "Інше",      Icon = "📦", ColorHex = "#94A3B8" });
        await db.SaveChangesAsync();
    }
}
