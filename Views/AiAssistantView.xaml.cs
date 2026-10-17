using System.Windows;
using System.Windows.Controls;
using FinMind.ViewModels;

namespace FinMind.Views;

public partial class AiAssistantView : UserControl
{
    public AiAssistantView() => InitializeComponent();

    /// <summary>Перетягування фото чека з Провідника просто у вікно.</summary>
    private void OnDrop(object sender, DragEventArgs e)
    {
        if (e.Data.GetData(DataFormats.FileDrop) is string[] { Length: > 0 } files &&
            DataContext is AiAssistantViewModel vm)
        {
            vm.ScanFileCommand.Execute(files[0]);
        }
    }
}
