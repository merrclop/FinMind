using System.IO;
using System.Windows.Media;
using System.Windows.Media.Imaging;

namespace FinMind.Services;

/// <summary>Підготовка фото чека: зменшення та стиснення в JPEG перед відправкою в Gemini.</summary>
public static class ImageHelper
{
    public static (byte[] Jpeg, BitmapSource Preview) Prepare(string path, int maxSide = 1600)
    {
        var bitmap = new BitmapImage();
        bitmap.BeginInit();
        bitmap.CacheOption = BitmapCacheOption.OnLoad;
        bitmap.CreateOptions = BitmapCreateOptions.IgnoreColorProfile;
        bitmap.UriSource = new Uri(path);
        bitmap.EndInit();
        bitmap.Freeze();                       // щоб можна було використати з UI-потоку

        BitmapSource source = bitmap;
        var longest = Math.Max(bitmap.PixelWidth, bitmap.PixelHeight);
        if (longest > maxSide)
        {
            var k = (double)maxSide / longest;
            var scaled = new TransformedBitmap(bitmap, new ScaleTransform(k, k));
            scaled.Freeze();
            source = scaled;
        }

        var encoder = new JpegBitmapEncoder { QualityLevel = 85 };
        encoder.Frames.Add(BitmapFrame.Create(source));
        using var ms = new MemoryStream();
        encoder.Save(ms);
        return (ms.ToArray(), source);
    }
}
