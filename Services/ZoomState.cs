using System.Windows.Media;

namespace FinMind.Services;

/// <summary>
/// Єдиний ScaleTransform для масштабування інтерфейсу.
/// Його використовують і корінь вікна, і випадаючі списки/календарі (Popup не успадковує
/// трансформацію батька, тому йому підставляється той самий об'єкт).
/// </summary>
public static class ZoomState
{
    private static ScaleTransform _transform = new(1, 1);

    public static ScaleTransform Transform 
    { 
        get => _transform;
        set
        {
            if (_transform != value)
            {
                _transform = value;
                TransformChanged?.Invoke();
            }
        }
    }

    public static event Action? TransformChanged;

    public static void Apply(double scale)
    {
        // Завжди створюємо новий Transform, щоб уникнути проблеми зі замороженням
        Transform = new ScaleTransform(scale, scale);
    }
}
