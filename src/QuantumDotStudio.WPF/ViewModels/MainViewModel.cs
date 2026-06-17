using QuantumDotStudio.Core.Models;
using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace QuantumDotStudio.WPF.ViewModels;

/// <summary>
/// Haupt-ViewModel: kapselt Simulation und Export und synchronisiert das aktive Quantum Dot.
/// </summary>
public class MainViewModel : INotifyPropertyChanged
{
    public SimulationViewModel Simulation { get; }
    public ExportViewModel Export { get; }

    public MainViewModel()
    {
        Simulation = new SimulationViewModel();
        Export = new ExportViewModel();

        // Export erhält initial das berechnete Quantum Dot.
        Export.ActiveDot = Simulation.ActiveDot;

        Simulation.PropertyChanged += OnSimulationPropertyChanged;
    }

    public MainViewModel(SimulationViewModel simulation, ExportViewModel export)
    {
        Simulation = simulation ?? throw new ArgumentNullException(nameof(simulation));
        Export = export ?? throw new ArgumentNullException(nameof(export));

        Export.ActiveDot = Simulation.ActiveDot;
        Simulation.PropertyChanged += OnSimulationPropertyChanged;
    }

    private void OnSimulationPropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (e.PropertyName == nameof(SimulationViewModel.ActiveDot))
        {
            Export.ActiveDot = Simulation.ActiveDot;
        }
    }

    public event PropertyChangedEventHandler? PropertyChanged;

    private void OnPropertyChanged([CallerMemberName] string propertyName = "")
    {
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }
}
