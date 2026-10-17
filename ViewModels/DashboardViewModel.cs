using System.Collections.ObjectModel;
using System.Globalization;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using FinMind.Converters;
using FinMind.Models;
using FinMind.Services;
using LiveChartsCore;
using LiveChartsCore.SkiaSharpView;
using LiveChartsCore.SkiaSharpView.Painting;
using SkiaSharp;

namespace FinMind.ViewModels;

/// <summary>Рядок розбивки витрат за категорією (праворуч від діаграми).</summary>
public record CategoryShare(string Name, string Icon, string ColorHex, decimal Amount, double Percent);

/// <summary>Дашборд: баланс, доходи/витрати за місяць, кругова діаграма, швидке додавання через ШІ.</summary>
public partial class DashboardViewModel : ViewModelBase
{
    private static readonly CultureInfo Ua = new("uk-UA");
    private readonly TransactionService _transactions = new();
    private readonly GeminiAiService _ai = new();

    [ObservableProperty] private decimal _balance;
    [ObservableProperty] private decimal _monthIncome;
    [ObservableProperty] private decimal _monthExpenses;
    [ObservableProperty] private string _quickText = string.Empty;
    [ObservableProperty] private bool _hasChartData;
    [ObservableProperty] private IEnumerable<ISeries> _series = Array.Empty<ISeries>();

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(MonthTitle))]
    private DateTime _selectedMonth = new(DateTime.Today.Year, DateTime.Today.Month, 1);

    public ObservableCollection<CategoryShare> Breakdown { get; } = new();

    public string MonthTitle
    {
        get
        {
            var s = SelectedMonth.ToString("MMMM yyyy", Ua);
            return char.ToUpper(s[0], Ua) + s[1..];
        }
    }

    public override Task OnNavigatedToAsync() => LoadAsync();

    partial void OnSelectedMonthChanged(DateTime value) => _ = LoadAsync();

    [RelayCommand] private void PreviousMonth() => SelectedMonth = SelectedMonth.AddMonths(-1);
    [RelayCommand] private void NextMonth() => SelectedMonth = SelectedMonth.AddMonths(1);

    private async Task LoadAsync()
    {
        try
        {
            IsBusy = true;
            Balance = await _transactions.GetBalanceAsync();
            var summary = await _transactions.GetMonthSummaryAsync(SelectedMonth);
            MonthIncome = summary.Income;
            MonthExpenses = summary.Expenses;

            Breakdown.Clear();
            var total = summary.ExpensesByCategory.Sum(x => x.Amount);
            foreach (var x in summary.ExpensesByCategory)
                Breakdown.Add(new CategoryShare(x.Category.Name, x.Category.Icon, x.Category.ColorHex,
                    x.Amount, total > 0 ? (double)(x.Amount / total * 100) : 0));

            // Кругова (кільцева) діаграма LiveCharts: по одній серії на категорію.
            Series = summary.ExpensesByCategory.Select(x => (ISeries)new PieSeries<double>
            {
                Values = new[] { (double)x.Amount },
                Name = x.Category.Name,
                Fill = new SolidColorPaint(SKColor.Parse(x.Category.ColorHex)),
                InnerRadius = 70
            }).ToList();
            HasChartData = Breakdown.Count > 0;
        }
        catch (Exception ex)
        {
            NotificationService.Error($"Не вдалося завантажити дані: {ex.Message}");
        }
        finally
        {
            IsBusy = false;
        }
    }

    /// <summary>«Кава та круасан 120 грн» → Gemini → збереження транзакції.</summary>
    [RelayCommand]
    private async Task AddViaAiAsync()
    {
        if (string.IsNullOrWhiteSpace(QuickText))
        {
            NotificationService.Error("Введіть опис, наприклад: «Кава та круасан 120 грн».");
            return;
        }

        try
        {
            IsBusy = true;
            var categories = await _transactions.GetCategoriesAsync();
            var parsed = await _ai.ParseTransactionAsync(QuickText.Trim(), categories.Select(c => c.Name));
            var category = TransactionService.Resolve(categories, parsed.Category);

            await _transactions.AddAsync(new Transaction
            {
                Amount = parsed.Amount,
                Type = parsed.IsIncome ? TransactionType.Income : TransactionType.Expense,
                CategoryId = category.Id,
                Date = parsed.Date ?? DateTime.Today,
                Description = parsed.Description
            });

            QuickText = string.Empty;
            NotificationService.Info($"Додано: {category.Icon} {category.Name} — {CurrencyConverter.Format(parsed.Amount)}");
            IsBusy = false;
            await LoadAsync();
        }
        catch (AiServiceException ex)
        {
            NotificationService.Error(ex.Message);
        }
        catch (Exception ex)
        {
            NotificationService.Error($"Не вдалося додати транзакцію: {ex.Message}");
        }
        finally
        {
            IsBusy = false;
        }
    }
}
