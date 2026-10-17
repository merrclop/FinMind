using CommunityToolkit.Mvvm.ComponentModel;

namespace FinMind.ViewModels;

/// <summary>Базовий клас для сторінок: індикатор завантаження + хук навігації.</summary>
public abstract partial class ViewModelBase : ObservableObject
{
    [ObservableProperty]
    private bool _isBusy;

    /// <summary>Викликається щоразу, коли користувач відкриває сторінку (оновлення даних).</summary>
    public virtual Task OnNavigatedToAsync() => Task.CompletedTask;
}
