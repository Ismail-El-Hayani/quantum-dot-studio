using System.IO;
using System.Windows;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using HelixToolkit.Wpf;

namespace QuantumDotStudio.WPF.Helpers;

/// <summary>
/// Hilfsklasse zum Erstellen eines Screenshots eines HelixViewport3D als PNG.
/// </summary>
public static class ScreenshotHelper
{
    /// <summary>
    /// Rendert den Inhalt des Viewports in ein PNG-Bild und speichert es unter dem angegebenen Pfad.
    /// </summary>
    /// <param name="viewport">Der HelixViewport3D, der gerendert werden soll.</param>
    /// <param name="filePath">Zielpfad für die PNG-Datei.</param>
    /// <param name="width">Breite des gerenderten Bildes in Pixeln.</param>
    /// <param name="height">Höhe des gerenderten Bildes in Pixeln.</param>
    /// <returns>True, wenn der Screenshot erfolgreich erstellt wurde.</returns>
    public static bool CaptureViewport(HelixViewport3D viewport, string filePath, int width = 1280, int height = 720)
    {
        if (viewport == null)
            return false;

        try
        {
            var bitmap = new RenderTargetBitmap(
                (int)(width * GetDpiScale()),
                (int)(height * GetDpiScale()),
                96 * GetDpiScale(),
                96 * GetDpiScale(),
                PixelFormats.Pbgra32);

            // Sicherstellen, dass das Element gemessen und angeordnet ist
            viewport.Measure(new Size(double.PositiveInfinity, double.PositiveInfinity));
            viewport.Arrange(new Rect(viewport.DesiredSize));

            bitmap.Render(viewport);

            var encoder = new PngBitmapEncoder();
            encoder.Frames.Add(BitmapFrame.Create(bitmap));

            Directory.CreateDirectory(Path.GetDirectoryName(filePath)!);
            using var stream = File.Create(filePath);
            encoder.Save(stream);

            return true;
        }
        catch
        {
            return false;
        }
    }

    private static double GetDpiScale()
    {
        // Für konsistente Ausgabe auf verschiedenen Displays verwenden wir 1.0 als Basis-Skala
        // und skalieren später über die Pixelabmessungen.
        return 1.0;
    }
}
