using System.Windows;
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
    }

    private void OnDataContextChanged(object sender, DependencyPropertyChangedEventArgs e)
    {
        Unsubscribe(e.OldValue);
        Subscribe(e.NewValue);
    }

    private void Unsubscribe(object? viewModel)
    {
        if (viewModel is MainViewModel mainVm)
        {
            mainVm.Simulation.PropertyChanged -= Simulation_PropertyChanged;
        }
        else if (viewModel is SimulationViewModel simVm)
        {
            simVm.PropertyChanged -= Simulation_PropertyChanged;
        }
    }

    private void Subscribe(object? viewModel)
    {
        if (viewModel is MainViewModel mainVm)
        {
            mainVm.Simulation.PropertyChanged += Simulation_PropertyChanged;
            mainVm.Export.ScreenshotProvider = CaptureScreenshot;
            Update3DView(mainVm.Simulation);
        }
        else if (viewModel is SimulationViewModel simVm)
        {
            simVm.PropertyChanged += Simulation_PropertyChanged;
            Update3DView(simVm);
        }
    }

    private string CaptureScreenshot(string targetPath)
    {
        ScreenshotHelper.CaptureViewport(Viewport3D, targetPath, 1280, 720);
        return targetPath;
    }

    private void Simulation_PropertyChanged(object? sender, System.ComponentModel.PropertyChangedEventArgs e)
    {
        if (sender is not SimulationViewModel vm)
            return;

        if (e.PropertyName is nameof(SimulationViewModel.ActiveDot)
                         or nameof(SimulationViewModel.ShowLattice)
                         or nameof(SimulationViewModel.ShowCloud))
        {
            Update3DView(vm);
        }
    }

    private void Update3DView(SimulationViewModel vm)
    {
        if (vm.ActiveDot == null)
            return;

        var model = _renderer.BuildModel(vm.ActiveDot, vm.ShowLattice, vm.ShowCloud);
        AtomModelHost.Content = model;

        var bounds = _renderer.GetBounds(vm.ActiveDot);
        if (bounds.SizeX > 0 && bounds.SizeY > 0 && bounds.SizeZ > 0)
        {
            Viewport3D.ZoomExtents(bounds);
        }
    }
}
