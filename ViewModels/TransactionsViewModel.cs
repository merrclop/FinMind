using System.Collections.ObjectModel;
using System.Globalization;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using FinMind.Models;
using FinMind.Services;

namespace FinMind.ViewModels;

/// <summary>Список транзакцій: фільтри, пагінація, форма додавання/редагування, видалення, експорт у CSV.</summary>
public partial class TransactionsViewModel : ViewModelBase
{
    private const int PageSize = 12;
    private static readonly CultureInfo Ua = new("uk-UA");
    private static readonly Category AllCategories = new() { Id = 0, Name = "Усі категорії", Icon = "🗂", ColorHex = "#888888" };

    private readonly TransactionService _service = new();
    private readonly ExportService _export = new();
    private List<Transaction> _all = new();
    private int _page = 1;
    private int _totalPages = 1;
    private int? _editingId;
    private bool _suspend;

    public ObservableCollection<Transaction> PageItems { get; } = new();
    public ObservableCollection<Category> Categories { get; } = new();
    public ObservableCollection<Category> FilterCategories { get; } = new();

    // Фільтри
    [ObservableProperty] private DateTime? _filterFrom;
    [ObservableProperty] private DateTime? _filterTo;
    [ObservableProperty] private Category? _filterCategory;

    // Форма
    [ObservableProperty] private bool _isFormOpen;
    [ObservableProperty] private string _formTitle = "Нова транзакція";
    [ObservableProperty] private string _formAmount = string.Empty;
    [ObservableProperty] private string _formDescription = string.Empty;
    [ObservableProperty] private string _formError = string.Empty;
    [ObservableProperty] private Category? _formCategory;
    [ObservableProperty] private DateTime? _formDate = DateTime.Today;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(FormIsIncome))]
    private bool _formIsExpense = true;

    public bool FormIsIncome
    {
        get => !FormIsExpense;
        set => FormIsExpense = !value;
    }

    public bool HasItems => PageItems.Count > 0;
    public string PageInfo => $"Сторінка {_page} з {_totalPages}  ·  записів: {_all.Count}";

    partial void OnFilterFromChanged(DateTime? value) => Reload();
    partial void OnFilterToChanged(DateTime? value) => Reload();
    partial void OnFilterCategoryChanged(Category? value) => Reload();
    private void Reload() { if (!_suspend) _ = LoadAsync(); }

    public override async Task OnNavigatedToAsync()
    {
        try
        {
            _suspend = true;
            var cats = await _service.GetCategoriesAsync();
            var filterId = FilterCategory?.Id ?? 0;
            var formId = FormCategory?.Id;

            Categories.Clear();
            FilterCategories.Clear();
            FilterCategories.Add(AllCategories);
            foreach (var c in cats) { Categories.Add(c); FilterCategories.Add(c); }

            FilterCategory = FilterCategories.FirstOrDefault(c => c.Id == filterId) ?? AllCategories;
            FormCategory = Categories.FirstOrDefault(c => c.Id == formId)
                           ?? Categories.FirstOrDefault(c => c.Name == "Інше") ?? Categories.FirstOrDefault();
        }
        catch (Exception ex)
        {
            NotificationService.Error($"Не вдалося завантажити категорії: {ex.Message}");
        }
        finally
        {
            _suspend = false;
        }
        await LoadAsync();
    }

    private async Task LoadAsync()
    {
        try
        {
            IsBusy = true;
            int? categoryId = FilterCategory is { Id: > 0 } c ? c.Id : null;
            _all = await _service.GetTransactionsAsync(FilterFrom, FilterTo, categoryId);
            _page = 1;
            RebuildPage();
        }
        catch (Exception ex)
        {
            NotificationService.Error($"Не вдалося завантажити транзакції: {ex.Message}");
        }
        finally
        {
            IsBusy = false;
        }
    }

    private void RebuildPage()
    {
        _totalPages = Math.Max(1, (int)Math.Ceiling(_all.Count / (double)PageSize));
        _page = Math.Clamp(_page, 1, _totalPages);

        PageItems.Clear();
        foreach (var t in _all.Skip((_page - 1) * PageSize).Take(PageSize)) PageItems.Add(t);

        OnPropertyChanged(nameof(PageInfo));
        OnPropertyChanged(nameof(HasItems));
    }

    // ───────── Пагінація / фільтри ─────────
    [RelayCommand] private void PreviousPage() { if (_page > 1) { _page--; RebuildPage(); } }
    [RelayCommand] private void NextPage() { if (_page < _totalPages) { _page++; RebuildPage(); } }

    [RelayCommand]
    private async Task ClearFiltersAsync()
    {
        _suspend = true;
        FilterFrom = null;
        FilterTo = null;
        FilterCategory = AllCategories;
        _suspend = false;
        await LoadAsync();
    }

    // ───────── Форма ─────────
    [RelayCommand]
    private void AddNew()
    {
        _editingId = null;
        FormTitle = "Нова транзакція";
        FormAmount = string.Empty;
        FormDescription = string.Empty;
        FormError = string.Empty;
        FormIsExpense = true;
        FormDate = DateTime.Today;
        FormCategory ??= Categories.FirstOrDefault();
        IsFormOpen = true;
    }

    [RelayCommand]
    private void Edit(Transaction? t)
    {
        if (t == null) return;
        _editingId = t.Id;
        FormTitle = "Редагування транзакції";
        FormAmount = t.Amount.ToString("0.##", Ua);
        FormDescription = t.Description;
        FormError = string.Empty;
        FormIsExpense = t.Type == TransactionType.Expense;
        FormDate = t.Date;
        FormCategory = Categories.FirstOrDefault(c => c.Id == t.CategoryId);
        IsFormOpen = true;
    }

    [RelayCommand] private void CancelForm() => IsFormOpen = false;

    [RelayCommand]
    private async Task SaveAsync()
    {
        FormError = string.Empty;
        if (!AmountParser.TryParse(FormAmount, out var amount) || amount <= 0 || amount > 1_000_000_000m)
        {
            FormError = "Вкажіть коректну суму більше нуля.";
            return;
        }
        if (FormCategory == null)
        {
            FormError = "Оберіть категорію.";
            return;
        }

        try
        {
            IsBusy = true;
            var tx = new Transaction
            {
                Id = _editingId ?? 0,
                Amount = Math.Round(amount, 2),
                Type = FormIsExpense ? TransactionType.Expense : TransactionType.Income,
                CategoryId = FormCategory.Id,
                Date = (FormDate ?? DateTime.Today).Date,
                Description = FormDescription.Trim()
            };

            if (_editingId is null) await _service.AddAsync(tx);
            else await _service.UpdateAsync(tx);

            IsFormOpen = false;
            NotificationService.Info(_editingId is null ? "Транзакцію додано ✅" : "Зміни збережено ✅");
            await LoadAsync();
        }
        catch (Exception ex)
        {
            NotificationService.Error($"Не вдалося зберегти: {ex.Message}");
        }
        finally
        {
            IsBusy = false;
        }
    }

    [RelayCommand]
    private async Task DeleteAsync(Transaction? t)
    {
        if (t == null) return;
        if (!DialogService.Confirm($"Видалити транзакцію «{t.Description}» на {t.Amount:N2}?")) return;

        try
        {
            IsBusy = true;
            await _service.DeleteAsync(t.Id);
            NotificationService.Info("Транзакцію видалено");
            await LoadAsync();
        }
        catch (Exception ex)
        {
            NotificationService.Error($"Не вдалося видалити: {ex.Message}");
        }
        finally
        {
            IsBusy = false;
        }
    }

    // ───────── Експорт ─────────
    [RelayCommand]
    private async Task ExportAsync()
    {
        if (_all.Count == 0)
        {
            NotificationService.Error("Немає даних для експорту.");
            return;
        }

        var path = DialogService.PickCsvSavePath($"finmind_{DateTime.Today:yyyyMMdd}.csv");
        if (path == null) return;

        try
        {
            IsBusy = true;
            await _export.ExportCsvAsync(_all, path);
            NotificationService.Info($"Експортовано {_all.Count} записів у CSV ✅");
        }
        catch (Exception ex)
        {
            NotificationService.Error($"Не вдалося зберегти файл: {ex.Message}");
        }
        finally
        {
            IsBusy = false;
        }
    }
}
