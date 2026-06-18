using System.Windows;
using QuantumDotStudio.Core.Models;
using QuantumDotStudio.Renderer;
using QuantumDotStudio.WPF.Helpers;
using QuantumDotStudio.WPF.ViewModels;

namespace QuantumDotStudio.WPF;

/// <summary>
/// Interaction logic for MainWindow.xaml
/// </summary>
public partial class MainWindow : Window
{
    private readonly QuantumDotRenderer3D _renderer = new();

    public MainWindow()
    {
        InitializeComponent();
        DataContextChanged += OnDataContextChanged;
        Loaded += OnLoaded;
    }

    private void OnLoaded(object sender, RoutedEventArgs e)
    {
        // Kamera erst nach dem ersten Render setzen, damit das Viewport seine Größe kennt.
        // Danach bleibt die Kamera stehen, damit Radius-Änderungen das QD sichtbar wachsen/schrumpfen lassen.
        Dispatcher.BeginInvoke(FitToMaxRadius, System.Windows.Threading.DispatcherPriority.Render);
    }

    private void OnDataContextChanged(object sender, DependencyPropertyChangedEventArgs e)
    {
        if (e.NewValue is MainViewModel mainVm)
        {
            mainVm.Export.ScreenshotProvider = CaptureScreenshot;
        }
    }

    private string CaptureScreenshot(string targetPath)
    {
        ScreenshotHelper.CaptureViewport(Viewport3D, targetPath, 1280, 720);
        return targetPath;
    }

    private void Reset3DView_Click(object sender, RoutedEventArgs e)
    {
        FitToMaxRadius();
    }

    private void FitToMaxRadius()
    {
        if (!Viewport3D.IsLoaded)
            return;

        Viewport3D.UpdateLayout();
        var bounds = _renderer.GetBounds(new QuantumDot { Radius_nm = 10.0 });
        if (bounds.SizeX > 0 && bounds.SizeY > 0 && bounds.SizeZ > 0)
        {
            Viewport3D.ZoomExtents(bounds);
        }
    }
}
