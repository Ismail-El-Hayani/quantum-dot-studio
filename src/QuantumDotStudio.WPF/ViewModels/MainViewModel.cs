using QuantumDotStudio.Core.Models;
using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace QuantumDotStudio.WPF.ViewModels;

/// <summary>
/// Haupt-ViewModel: kapselt Simulation, Sensor und Export und synchronisiert
/// das aktive Quantum Dot zwischen allen Ansichten.
/// </summary>
public class MainViewModel : INotifyPropertyChanged
{
    public SimulationViewModel Simulation { get; }
    public SensorViewModel Sensor { get; }
    public ExportViewModel Export { get; }

    public MainViewModel()
    {
        Simulation = new SimulationViewModel();
        Sensor = new SensorViewModel();
        Export = new ExportViewModel();

        // Export und Sensor erhalten initial das berechnete Quantum Dot.
        Export.ActiveDot = Simulation.ActiveDot;
        Sensor.Dot = Simulation.ActiveDot;

        Simulation.PropertyChanged += OnSimulationPropertyChanged;
    }

    public MainViewModel(SimulationViewModel simulation, SensorViewModel sensor, ExportViewModel export)
    {
        Simulation = simulation ?? throw new ArgumentNullException(nameof(simulation));
        Sensor = sensor ?? throw new ArgumentNullException(nameof(sensor));
        Export = export ?? throw new ArgumentNullException(nameof(export));

        Export.ActiveDot = Simulation.ActiveDot;
        Sensor.Dot = Simulation.ActiveDot;
        Simulation.PropertyChanged += OnSimulationPropertyChanged;
    }

    private void OnSimulationPropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (e.PropertyName == nameof(SimulationViewModel.ActiveDot))
        {
            Export.ActiveDot = Simulation.ActiveDot;
            Sensor.Dot = Simulation.ActiveDot;
        }
    }

    public event PropertyChangedEventHandler? PropertyChanged;

    private void OnPropertyChanged([CallerMemberName] string propertyName = "")
    {
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }
}
