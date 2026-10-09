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
    public FeasibilityViewModel Feasibility { get; }
    public ExportViewModel Export { get; }

    public MainViewModel()
    {
        Simulation = new SimulationViewModel();
        Sensor = new SensorViewModel();
        Feasibility = new FeasibilityViewModel();
        Export = new ExportViewModel();

        // Export, Sensor und Machbarkeit erhalten initial das berechnete Quantum Dot.
        Export.ActiveDot = Simulation.ActiveDot;
        Sensor.Dot = Simulation.ActiveDot;
        Feasibility.Dot = Simulation.ActiveDot;

        Simulation.PropertyChanged += OnSimulationPropertyChanged;
        Sensor.PropertyChanged += OnSensorPropertyChanged;
    }

    public MainViewModel(SimulationViewModel simulation, SensorViewModel sensor, FeasibilityViewModel feasibility, ExportViewModel export)
    {
        Simulation = simulation ?? throw new ArgumentNullException(nameof(simulation));
        Sensor = sensor ?? throw new ArgumentNullException(nameof(sensor));
        Feasibility = feasibility ?? throw new ArgumentNullException(nameof(feasibility));
        Export = export ?? throw new ArgumentNullException(nameof(export));

        Export.ActiveDot = Simulation.ActiveDot;
        Sensor.Dot = Simulation.ActiveDot;
        Feasibility.Dot = Simulation.ActiveDot;
        Simulation.PropertyChanged += OnSimulationPropertyChanged;
        Sensor.PropertyChanged += OnSensorPropertyChanged;
    }

    private void OnSimulationPropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (e.PropertyName == nameof(SimulationViewModel.ActiveDot))
        {
            Export.ActiveDot = Simulation.ActiveDot;
            Sensor.Dot = Simulation.ActiveDot;
            Feasibility.Dot = Simulation.ActiveDot;
        }
    }

    private void OnSensorPropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        // Ligand/Analyt/Konzentration aus dem Sensor-Tab fliessen in die
        // Machbarkeits-Bewertung ein.
        if (e.PropertyName == nameof(SensorViewModel.SelectedLigand))
            Feasibility.SelectedLigand = Sensor.SelectedLigand;
        else if (e.PropertyName == nameof(SensorViewModel.SelectedAnalyte))
            Feasibility.SelectedAnalyte = Sensor.SelectedAnalyte;
        else if (e.PropertyName == nameof(SensorViewModel.Concentration_M))
            Feasibility.TargetConcentration_M = Sensor.Concentration_M;
        else if (e.PropertyName == nameof(SensorViewModel.ReceptorOffset_nm))
            Feasibility.ReceptorOffset_nm = Sensor.ReceptorOffset_nm;
        else if (e.PropertyName == nameof(SensorViewModel.DistanceUnboundExtra_nm))
            Feasibility.DistanceUnboundExtra_nm = Sensor.DistanceUnboundExtra_nm;
        else if (e.PropertyName == nameof(SensorViewModel.DistanceBoundExtra_nm))
            Feasibility.DistanceBoundExtra_nm = Sensor.DistanceBoundExtra_nm;
    }

    public event PropertyChangedEventHandler? PropertyChanged;

    private void OnPropertyChanged([CallerMemberName] string propertyName = "")
    {
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }
}
