using QuantumDotStudio.Core.Data;
using QuantumDotStudio.Core.Models;
using QuantumDotStudio.Solver;
using OxyPlot;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Windows.Input;

namespace QuantumDotStudio.WPF.ViewModels;

/// <summary>
/// ViewModel für Simulationsparameter, Validierung und Ergebnisse.
/// </summary>
public class SimulationViewModel : INotifyPropertyChanged
{
    private readonly QuantumDotService _service;
    private Material _selectedMaterial = MaterialDatabase.Defaults.First();
    private double _radius_nm = 3.0;
    private QuantumDot _activeDot = new();
    private string? _validationMessage;

    public SimulationViewModel()
    {
        _service = new QuantumDotService();
        Materials = new List<Material>(MaterialDatabase.Defaults);
        RecalculateCommand = new RelayCommand(_ => Recalculate(), _ => CanRecalculate());

        Recalculate();
    }

    public SimulationViewModel(QuantumDotService service)
    {
        _service = service ?? throw new ArgumentNullException(nameof(service));
        Materials = new List<Material>(MaterialDatabase.Defaults);
        RecalculateCommand = new RelayCommand(_ => Recalculate(), _ => CanRecalculate());

        Recalculate();
    }

    /// <summary>
    /// Verfügbare Halbleitermaterialien.
    /// </summary>
    public List<Material> Materials { get; }

    /// <summary>
    /// Aktuell gewähltes Material.
    /// </summary>
    public Material SelectedMaterial
    {
        get => _selectedMaterial;
        set
        {
            if (_selectedMaterial != value)
            {
                _selectedMaterial = value ?? throw new ArgumentNullException(nameof(value));
                OnPropertyChanged();
                Recalculate();
            }
        }
    }

    /// <summary>
    /// Aktueller Radius in nm. Wird auf gültigen Bereich [1, 10] validiert.
    /// </summary>
    public double Radius_nm
    {
        get => _radius_nm;
        set
        {
            if (Math.Abs(_radius_nm - value) > 1e-6)
            {
                _radius_nm = value;
                OnPropertyChanged();
                ValidateRadius();
            }
        }
    }

    /// <summary>
    /// Fehlermeldung bei ungültigem Radius, sonst null.
    /// </summary>
    public string? ValidationMessage
    {
        get => _validationMessage;
        private set
        {
            _validationMessage = value;
            OnPropertyChanged();
            OnPropertyChanged(nameof(HasValidationError));
        }
    }

    public bool HasValidationError => !string.IsNullOrEmpty(_validationMessage);

    /// <summary>
    /// Aktives Quantum Dot mit allen berechneten Eigenschaften.
    /// </summary>
    public QuantumDot ActiveDot
    {
        get => _activeDot;
        private set
        {
            _activeDot = value;
            OnPropertyChanged();
            OnPropertyChanged(nameof(BandGapText));
            OnPropertyChanged(nameof(WavelengthText));
            OnPropertyChanged(nameof(AtomCountText));
            OnPropertyChanged(nameof(EnergyLevelPlot));
            OnPropertyChanged(nameof(SpectrumPlot));
            OnPropertyChanged(nameof(IsResultAvailable));
        }
    }

    public bool IsResultAvailable => ActiveDot != null && !HasValidationError;

    public string BandGapText => $"Bandlücke: {ActiveDot.TotalBandGap_eV:F3} eV";
    public string WavelengthText => $"Wellenlänge: {ActiveDot.EmissionWavelength_nm:F1} nm";
    public string AtomCountText => $"Atome: {ActiveDot.Atoms.Count}";

    public PlotModel EnergyLevelPlot => PlotFactory.CreateEnergyLevelPlot(ActiveDot);
    public PlotModel SpectrumPlot => PlotFactory.CreateSpectrumPlot(ActiveDot);

    public ICommand RecalculateCommand { get; }

    private bool CanRecalculate()
    {
        return !HasValidationError && SelectedMaterial != null;
    }

    private void ValidateRadius()
    {
        if (_radius_nm < 1.0)
        {
            ValidationMessage = "Radius muss mindestens 1 nm betragen.";
        }
        else if (_radius_nm > 10.0)
        {
            ValidationMessage = "Radius darf maximal 10 nm betragen.";
        }
        else
        {
            ValidationMessage = null;
            Recalculate();
        }

        if (RecalculateCommand is RelayCommand cmd)
        {
            cmd.RaiseCanExecuteChanged();
        }
    }

    private void Recalculate()
    {
        if (HasValidationError || SelectedMaterial == null)
            return;

        ActiveDot = _service.BuildQuantumDot(SelectedMaterial, _radius_nm);
    }

    public event PropertyChangedEventHandler? PropertyChanged;

    private void OnPropertyChanged([CallerMemberName] string propertyName = "")
    {
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }
}
