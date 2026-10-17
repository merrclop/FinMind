using System.ComponentModel;
using System.Diagnostics;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Navigation;
using FinMind.ViewModels;

namespace FinMind.Views;

public partial class SettingsView : UserControl
{
    private SettingsViewModel? _vm;

    public SettingsView()
    {
        InitializeComponent();
        DataContextChanged += (_, _) => Attach();
        Loaded += (_, _) => { Attach(); SyncPassword(); };
        Unloaded += (_, _) => { if (_vm != null) _vm.PropertyChanged -= OnVmChanged; _vm = null; };
    }

    // PasswordBox не підтримує прив'язку, тому синхронізуємо його з ViewModel вручну.
    private void Attach()
    {
        if (_vm != null) _vm.PropertyChanged -= OnVmChanged;
        _vm = DataContext as SettingsViewModel;
        if (_vm != null) _vm.PropertyChanged += OnVmChanged;
        SyncPassword();
    }

    private void OnVmChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (e.PropertyName == nameof(SettingsViewModel.ApiKey)) SyncPassword();
    }

    private void SyncPassword()
    {
        if (_vm != null && KeyBox.Password != _vm.ApiKey) KeyBox.Password = _vm.ApiKey;
    }

    private void OnPasswordChanged(object sender, RoutedEventArgs e)
    {
        if (_vm != null && _vm.ApiKey != KeyBox.Password) _vm.ApiKey = KeyBox.Password;
    }

    private void OnLinkClick(object sender, RequestNavigateEventArgs e)
    {
        try { Process.Start(new ProcessStartInfo(e.Uri.AbsoluteUri) { UseShellExecute = true }); }
        catch { /* браузер недоступний — ігноруємо */ }
        e.Handled = true;
    }
}
