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
        if (e.OldValue is MainViewModel oldVm)
            oldVm.PropertyChanged -= ViewModel_PropertyChanged;

        if (e.NewValue is MainViewModel newVm)
        {
            newVm.PropertyChanged += ViewModel_PropertyChanged;
            Update3DView(newVm);
        }
    }

    private void ViewModel_PropertyChanged(object? sender, System.ComponentModel.PropertyChangedEventArgs e)
    {
        if (sender is not MainViewModel vm)
            return;

        if (e.PropertyName == nameof(MainViewModel.ActiveDot))
            Update3DView(vm);
    }

    private void Update3DView(MainViewModel vm)
    {
        if (vm.ActiveDot == null)
            return;

        var model = _renderer.BuildModel(vm.ActiveDot);
        AtomModelHost.Content = model;

        var bounds = _renderer.GetBounds(vm.ActiveDot);
        if (bounds.SizeX > 0 && bounds.SizeY > 0 && bounds.SizeZ > 0)
        {
            Viewport3D.ZoomExtents(bounds);
        }
    }
}
