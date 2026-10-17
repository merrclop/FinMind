using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using FinMind.Models;
using FinMind.ViewModels;

namespace FinMind.Views;

public partial class TransactionsView : UserControl
{
    public TransactionsView() => InitializeComponent();

    /// <summary>
    /// DataGrid без власної прокрутки «з'їдає» колесо миші — передаємо його сторінці,
    /// щоб скрол працював над таблицею (Ctrl+колесо = масштаб обробляється вікном раніше).
    /// </summary>
    private void Grid_PreviewMouseWheel(object sender, MouseWheelEventArgs e)
    {
        if (e.Handled) return;
        e.Handled = true;
        var forwarded = new MouseWheelEventArgs(e.MouseDevice, e.Timestamp, e.Delta)
        {
            RoutedEvent = MouseWheelEvent,
            Source = sender
        };
        PageScroll.RaiseEvent(forwarded);
    }

    /// <summary>Подвійний клік (або подвійний тап) по рядку відкриває редагування.</summary>
    private void Grid_MouseDoubleClick(object sender, MouseButtonEventArgs e)
    {
        if (e.OriginalSource is not DependencyObject source) return;
        var row = ItemsControl.ContainerFromElement((DataGrid)sender, source) as DataGridRow;
        if (row?.Item is Transaction t && DataContext is TransactionsViewModel vm)
            vm.EditCommand.Execute(t);
    }
}
