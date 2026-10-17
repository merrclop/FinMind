using System.Windows.Threading;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using FinMind.Services;

namespace FinMind.ViewModels;

/// <summary>Навігація, масштаб, адаптивне бічне меню та «тости».</summary>
public partial class MainViewModel : ObservableObject
{
    private readonly Dictionary<string, ViewModelBase> _pages;
    private readonly DispatcherTimer _toastTimer = new();
    private bool? _lastAutoCompact;

    [ObservableProperty] private ViewModelBase? _currentViewModel;
    [ObservableProperty] private string _currentPage = string.Empty;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(SidebarWidth))]
    private bool _isSidebarCompact;

    [ObservableProperty] private string _toastText = string.Empty;
    [ObservableProperty] private bool _isToastVisible;
    [ObservableProperty] private bool _isToastError;

    public double SidebarWidth => IsSidebarCompact ? 76 : 230;
    public string ZoomText => $"{Math.Round(SettingsService.Instance.UiScale * 100)}%";

    public MainViewModel()
    {
        _pages = new Dictionary<string, ViewModelBase>
        {
            ["Dashboard"] = new DashboardViewModel(),
            ["Transactions"] = new TransactionsViewModel(),
            ["Ai"] = new AiAssistantViewModel(),
            ["Settings"] = new SettingsViewModel()
        };

        SettingsService.Instance.UiScaleChanged += () => OnPropertyChanged(nameof(ZoomText));
        NotificationService.Raised += ShowToast;
        _toastTimer.Tick += (_, _) => { _toastTimer.Stop(); IsToastVisible = false; };
    }

    [RelayCommand]
    private async Task NavigateAsync(string page)
    {
        if (!_pages.TryGetValue(page, out var vm)) return;
        CurrentPage = page;
        CurrentViewModel = vm;
        await vm.OnNavigatedToAsync();
    }

    [RelayCommand] private void ZoomIn() => SettingsService.Instance.UiScale += 0.1;
    [RelayCommand] private void ZoomOut() => SettingsService.Instance.UiScale -= 0.1;
    [RelayCommand] private void ResetZoom() => SettingsService.Instance.UiScale = 1.0;
    [RelayCommand] private void ToggleSidebar() => IsSidebarCompact = !IsSidebarCompact;

    [RelayCommand]
    private void CloseToast()
    {
        _toastTimer.Stop();
        IsToastVisible = false;
    }

    /// <summary>Автоматично згортає меню у вузькому вікні (враховує поточний масштаб).</summary>
    public void UpdateLayoutMode(double effectiveWidth)
    {
        var compact = effectiveWidth < 900;
        if (_lastAutoCompact == compact) return;
        _lastAutoCompact = compact;
        IsSidebarCompact = compact;
    }

    private void ShowToast(string message, bool isError)
    {
        ToastText = message;
        IsToastError = isError;
        IsToastVisible = true;
        _toastTimer.Interval = TimeSpan.FromSeconds(isError ? 7 : 4);
        _toastTimer.Stop();
        _toastTimer.Start();
    }
}
