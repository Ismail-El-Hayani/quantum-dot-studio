using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Input;
using System.Windows.Media.Media3D;
using QuantumDotStudio.Renderer;
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
            Update3DView(mainVm.Simulation.ActiveDot);
        }
        else if (viewModel is SimulationViewModel simVm)
        {
            simVm.PropertyChanged += Simulation_PropertyChanged;
            Update3DView(simVm.ActiveDot);
        }
    }

    private void Simulation_PropertyChanged(object? sender, System.ComponentModel.PropertyChangedEventArgs e)
    {
        if (sender is not SimulationViewModel vm)
            return;

        if (e.PropertyName == nameof(SimulationViewModel.ActiveDot))
            Update3DView(vm.ActiveDot);
    }

    private void Update3DView(QuantumDotStudio.Core.Models.QuantumDot? dot)
    {
        if (dot == null)
            return;

        var model = _renderer.BuildModel(dot);
        AtomModelHost.Content = model;

        var bounds = _renderer.GetBounds(dot);
        if (bounds.SizeX > 0 && bounds.SizeY > 0 && bounds.SizeZ > 0)
        {
            Viewport3D.ZoomExtents(bounds);
        }
    }
}
