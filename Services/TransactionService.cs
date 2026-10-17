using System.Globalization;
using System.Text;
using FinMind.Data;
using FinMind.Models;
using Microsoft.EntityFrameworkCore;

namespace FinMind.Services;

public record CategoryTotal(Category Category, decimal Amount);
public record MonthSummary(decimal Income, decimal Expenses, IReadOnlyList<CategoryTotal> ExpensesByCategory);

/// <summary>
/// Уся робота з БД. Кожен метод створює короткоживучий DbContext.
/// Суми рахуємо на клієнті, бо SQLite у EF Core не підтримує SUM для decimal.
/// </summary>
public class TransactionService
{
    public async Task<List<Category>> GetCategoriesAsync()
    {
        await using var db = new AppDbContext();
        return await db.Categories.AsNoTracking().OrderBy(c => c.Id).ToListAsync();
    }

    public async Task<List<Transaction>> GetTransactionsAsync(DateTime? from, DateTime? to, int? categoryId)
    {
        await using var db = new AppDbContext();
        IQueryable<Transaction> query = db.Transactions.AsNoTracking().Include(t => t.Category);

        if (from.HasValue) query = query.Where(t => t.Date >= from.Value.Date);
        if (to.HasValue)
        {
            var end = to.Value.Date.AddDays(1);
            query = query.Where(t => t.Date < end);
        }
        if (categoryId.HasValue) query = query.Where(t => t.CategoryId == categoryId.Value);

        return await query.OrderByDescending(t => t.Date).ThenByDescending(t => t.Id).ToListAsync();
    }

    public async Task AddAsync(Transaction t)
    {
        await using var db = new AppDbContext();
        t.Id = 0;
        t.Category = null;
        t.CreatedAt = DateTime.Now;
        db.Transactions.Add(t);
        await db.SaveChangesAsync();
    }

    public async Task UpdateAsync(Transaction t)
    {
        await using var db = new AppDbContext();
        var entity = await db.Transactions.FindAsync(t.Id)
                     ?? throw new InvalidOperationException("Транзакцію не знайдено (можливо, її вже видалено).");
        entity.Amount = t.Amount;
        entity.Type = t.Type;
        entity.CategoryId = t.CategoryId;
        entity.Date = t.Date;
        entity.Description = t.Description;
        await db.SaveChangesAsync();
    }

    public async Task DeleteAsync(int id)
    {
        await using var db = new AppDbContext();
        await db.Transactions.Where(t => t.Id == id).ExecuteDeleteAsync();
    }

    public async Task<decimal> GetBalanceAsync()
    {
        await using var db = new AppDbContext();
        var rows = await db.Transactions.AsNoTracking().Select(t => new { t.Type, t.Amount }).ToListAsync();
        return rows.Sum(r => r.Type == TransactionType.Income ? r.Amount : -r.Amount);
    }

    public async Task<MonthSummary> GetMonthSummaryAsync(DateTime month)
    {
        var start = new DateTime(month.Year, month.Month, 1);
        var end = start.AddMonths(1);

        await using var db = new AppDbContext();
        var rows = await db.Transactions.AsNoTracking().Include(t => t.Category)
            .Where(t => t.Date >= start && t.Date < end).ToListAsync();

        var income = rows.Where(t => t.Type == TransactionType.Income).Sum(t => t.Amount);
        var expenses = rows.Where(t => t.Type == TransactionType.Expense).Sum(t => t.Amount);

        var byCategory = rows.Where(t => t.Type == TransactionType.Expense && t.Category != null)
            .GroupBy(t => t.CategoryId)
            .Select(g => new CategoryTotal(g.First().Category!, g.Sum(x => x.Amount)))
            .OrderByDescending(x => x.Amount)
            .ToList();

        return new MonthSummary(income, expenses, byCategory);
    }

    /// <summary>Підбір категорії за назвою, яку повернув ШІ (точний збіг → часткове входження → «Інше»).</summary>
    public static Category Resolve(IReadOnlyList<Category> categories, string? name)
    {
        if (!string.IsNullOrWhiteSpace(name))
        {
            var n = name.Trim();
            var exact = categories.FirstOrDefault(c => c.Name.Equals(n, StringComparison.CurrentCultureIgnoreCase));
            if (exact != null) return exact;
            var partial = categories.FirstOrDefault(c =>
                c.Name.Contains(n, StringComparison.CurrentCultureIgnoreCase) ||
                n.Contains(c.Name, StringComparison.CurrentCultureIgnoreCase));
            if (partial != null) return partial;
        }
        return categories.FirstOrDefault(c => c.Name == "Інше") ?? categories.First();
    }

    /// <summary>Агрегована статистика за останні N днів (з порівнянням із попереднім періодом) для фінансового радника.</summary>
    public async Task<string?> BuildAdviceStatsAsync(int days = 30)
    {
        var to = DateTime.Today.AddDays(1);
        var from = to.AddDays(-days);
        var prevFrom = from.AddDays(-days);

        await using var db = new AppDbContext();
        var rows = await db.Transactions.AsNoTracking().Include(t => t.Category)
            .Where(t => t.Date >= prevFrom && t.Date < to).ToListAsync();

        var current = rows.Where(t => t.Date >= from).ToList();
        if (current.Count == 0) return null;
        var previous = rows.Where(t => t.Date < from).ToList();

        var inv = CultureInfo.InvariantCulture;
        var currency = SettingsService.Instance.Current.Currency;
        var income = current.Where(t => t.Type == TransactionType.Income).Sum(t => t.Amount);
        var expenses = current.Where(t => t.Type == TransactionType.Expense).Sum(t => t.Amount);
        var prevExpenses = previous.Where(t => t.Type == TransactionType.Expense).Sum(t => t.Amount);

        var sb = new StringBuilder();
        sb.AppendLine($"Валюта: {currency}. Період: останні {days} днів ({from:dd.MM.yyyy}–{to.AddDays(-1):dd.MM.yyyy}).");
        sb.AppendLine($"Доходи: {income.ToString("0.00", inv)}; витрати: {expenses.ToString("0.00", inv)}; " +
                      $"залишок: {(income - expenses).ToString("0.00", inv)}.");
        sb.AppendLine($"Витрати попередніх {days} днів: {prevExpenses.ToString("0.00", inv)}.");
        sb.AppendLine("Витрати за категоріями (поточний період | попередній період):");

        var cur = current.Where(t => t.Type == TransactionType.Expense)
            .GroupBy(t => t.Category?.Name ?? "Інше").ToDictionary(g => g.Key, g => g.Sum(x => x.Amount));
        var prev = previous.Where(t => t.Type == TransactionType.Expense)
            .GroupBy(t => t.Category?.Name ?? "Інше").ToDictionary(g => g.Key, g => g.Sum(x => x.Amount));

        foreach (var (name, sum) in cur.OrderByDescending(kv => kv.Value))
        {
            var share = expenses > 0 ? sum / expenses * 100 : 0;
            prev.TryGetValue(name, out var p);
            sb.AppendLine($"- {name}: {sum.ToString("0.00", inv)} ({share.ToString("0", inv)}% витрат) | {p.ToString("0.00", inv)}");
        }

        sb.AppendLine("Найбільші окремі витрати:");
        foreach (var t in current.Where(t => t.Type == TransactionType.Expense)
                     .OrderByDescending(t => t.Amount).Take(5))
            sb.AppendLine($"- {t.Amount.ToString("0.00", inv)} — {t.Category?.Name}: {t.Description}");

        return sb.ToString();
    }
}
