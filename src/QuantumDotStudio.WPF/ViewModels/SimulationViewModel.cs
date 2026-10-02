using QuantumDotStudio.Core.Data;
using QuantumDotStudio.Core.Models;
using QuantumDotStudio.Renderer;
using QuantumDotStudio.Solver;
using OxyPlot;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Windows.Input;
using System.Windows.Threading;

namespace QuantumDotStudio.WPF.ViewModels;

/// <summary>
/// ViewModel für Simulationsparameter, Validierung und Ergebnisse.
/// Unterstützt homogene Quantum Dots und Core/Shell-Heterostrukturen:
/// bei aktivem Shell-Modus wird statt BuildQuantumDot die Kern/Hüllen-
/// Berechnung mit endlichem Potentialtopf, Band-Offsets und Strain genutzt.
/// </summary>
public class SimulationViewModel : INotifyPropertyChanged
{
    private readonly QuantumDotService _service;
    private readonly DispatcherTimer _recalcTimer;
    private Material _selectedMaterial;
    private Material? _selectedShellMaterial;
    private double _shellThickness_nm;
    private bool _useShell;
    private double _radius_nm = 3.0;
    private QuantumDot _activeDot = new();
    private string? _validationMessage;
    private bool _showLattice = true;
    private bool _showCloud = true;

    public SimulationViewModel()
    {
        _service = new QuantumDotService();
        _recalcTimer = CreateRecalcTimer();
        Materials = new List<Material>(MaterialDatabase.Defaults);
        // Auswahl aus derselben Listeninstanz initialisieren, damit die
        // ComboBox-Referenz (SelectedItem) mit dem Feld uebereinstimmt.
        _selectedMaterial = Materials[0];
        _selectedShellMaterial = Materials.Count > 1 ? Materials[1] : Materials[0];
        RecalculateCommand = new RelayCommand(_ => { _recalcTimer?.Stop(); Recalculate(); }, _ => CanRecalculate());

        Recalculate();
    }

    public SimulationViewModel(QuantumDotService service)
    {
        _service = service ?? throw new ArgumentNullException(nameof(service));
        _recalcTimer = CreateRecalcTimer();
        Materials = new List<Material>(MaterialDatabase.Defaults);
        _selectedMaterial = Materials[0];
        _selectedShellMaterial = Materials.Count > 1 ? Materials[1] : Materials[0];
        RecalculateCommand = new RelayCommand(_ => { _recalcTimer?.Stop(); Recalculate(); }, _ => CanRecalculate());

        Recalculate();
    }

    private DispatcherTimer CreateRecalcTimer()
    {
        var timer = new DispatcherTimer(TimeSpan.FromMilliseconds(150), DispatcherPriority.Background, OnRecalcTimerTick, Dispatcher.CurrentDispatcher);
        timer.Stop();
        return timer;
    }

    private void OnRecalcTimerTick(object? sender, EventArgs e)
    {
        _recalcTimer?.Stop();
        Recalculate();
    }

    /// <summary>
    /// Verfügbare Halbleitermaterialien (Core und Shell).
    /// </summary>
    public List<Material> Materials { get; }

    /// <summary>
    /// Aktuell gewähltes Core-Material.
    /// </summary>
    public Material SelectedMaterial
    {
        get => _selectedMaterial;
        set
        {
            if (_selectedMaterial != value)
            {
                _selectedMaterial = value ?? throw new ArgumentNullException(nameof(value));
                OnPropertyChanged(nameof(SelectedMaterial));
                Recalculate();
            }
        }
    }

    /// <summary>
    /// Aktuell gewähltes Shell-Material (nur relevant bei aktivem Shell-Modus).
    /// </summary>
    public Material? SelectedShellMaterial
    {
        get => _selectedShellMaterial;
        set
        {
            if (_selectedShellMaterial != value)
            {
                _selectedShellMaterial = value;
                OnPropertyChanged(nameof(SelectedShellMaterial));
                Recalculate();
            }
        }
    }

    /// <summary>
    /// Schalendicke in nm (0..5).
    /// </summary>
    public double ShellThickness_nm
    {
        get => _shellThickness_nm;
        set
        {
            if (Math.Abs(_shellThickness_nm - value) > 1e-6)
            {
                _shellThickness_nm = value;
                OnPropertyChanged(nameof(ShellThickness_nm));
                RestartRecalcTimer();
            }
        }
    }

    /// <summary>
    /// True, wenn eine Core/Shell-Heterostruktur simuliert werden soll.
    /// </summary>
    public bool UseShell
    {
        get => _useShell;
        set
        {
            if (_useShell != value)
            {
                _useShell = value;
                OnPropertyChanged(nameof(UseShell));
                Recalculate();
            }
        }
    }

    /// <summary>
    /// Aktueller Radius in nm. Wird auf gültigen Bereich [1, 10] validiert.
    /// Im Shell-Modus interpretiert als Core-Radius.
    /// </summary>
    public double Radius_nm
    {
        get => _radius_nm;
        set
        {
            if (Math.Abs(_radius_nm - value) > 1e-6)
            {
                _radius_nm = value;
                OnPropertyChanged(nameof(Radius_nm));
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
            OnPropertyChanged(nameof(ActiveDot));
            OnPropertyChanged(nameof(BandGapText));
            OnPropertyChanged(nameof(WavelengthText));
            OnPropertyChanged(nameof(AtomCountText));
            OnPropertyChanged(nameof(ShellInfoText));
            OnPropertyChanged(nameof(EnergyLevelPlot));
            OnPropertyChanged(nameof(SpectrumPlot));
            OnPropertyChanged(nameof(BandProfilePlot));
            OnPropertyChanged(nameof(IsResultAvailable));
            OnPropertyChanged(nameof(LegendItems));
        }
    }

    public bool IsResultAvailable => ActiveDot != null && !HasValidationError;

    public bool ShowLattice
    {
        get => _showLattice;
        set
        {
            if (_showLattice != value)
            {
                _showLattice = value;
                OnPropertyChanged(nameof(ShowLattice));
            }
        }
    }

    public bool ShowCloud
    {
        get => _showCloud;
        set
        {
            if (_showCloud != value)
            {
                _showCloud = value;
                OnPropertyChanged(nameof(ShowCloud));
            }
        }
    }

    /// <summary>
    /// Einträge für die Farblegende der 3D-Visualisierung.
    /// </summary>
    public List<LegendItem> LegendItems
    {
        get
        {
            if (ActiveDot?.Atoms == null)
                return new List<LegendItem>();

            return ActiveDot.Atoms
                .Where(a => !string.IsNullOrEmpty(a.Element))
                .Select(a => a.Element)
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .Select(el => new LegendItem
                {
                    Label = el,
                    Brush = new System.Windows.Media.SolidColorBrush(AtomPalette.GetColorByElement(el))
                })
                .ToList();
        }
    }

    public string BandGapText => $"Bandlücke: {ActiveDot.TotalBandGap_eV:F3} eV";
    public string WavelengthText => $"Wellenlänge: {ActiveDot.EmissionWavelength_nm:F1} nm";
    public string AtomCountText => $"Atome: {ActiveDot.Atoms.Count}";

    /// <summary>
    /// Kurzinfo zur Shell (Material, Dicke, Strain-Status) oder leer.
    /// </summary>
    public string ShellInfoText
    {
        get
        {
            if (ActiveDot is not CoreShellQuantumDot cs)
                return string.Empty;

            string strain = cs.IsStrainRelaxed
                ? "kritisch (überschreitet t_krit)"
                : "kohärent (< t_krit)";
            return $"Shell: {cs.ShellMaterial.Name} {cs.ShellThickness_nm:F1} nm | Missfit {cs.LatticeMismatch_f * 100:F1} % | {strain}";
        }
    }

    public PlotModel EnergyLevelPlot => PlotFactory.CreateEnergyLevelPlot(ActiveDot);
    public PlotModel SpectrumPlot => PlotFactory.CreateSpectrumPlot(ActiveDot);
    public PlotModel BandProfilePlot => PlotFactory.CreateBandProfilePlot(ActiveDot);

    public ICommand RecalculateCommand { get; }

    private bool CanRecalculate()
    {
        return !HasValidationError && SelectedMaterial != null;
    }

    private void RestartRecalcTimer()
    {
        if (HasValidationError)
            return;
        _recalcTimer?.Stop();
        _recalcTimer?.Start();
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
            _recalcTimer?.Stop();
            _recalcTimer?.Start();
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

        if (_useShell && _selectedShellMaterial != null)
        {
            ActiveDot = _service.BuildCoreShellQuantumDot(
                SelectedMaterial, _selectedShellMaterial,
                _radius_nm, Math.Max(0.0, _shellThickness_nm));
        }
        else
        {
            ActiveDot = _service.BuildQuantumDot(SelectedMaterial, _radius_nm);
        }
    }

    public event PropertyChangedEventHandler? PropertyChanged;

    private void OnPropertyChanged([CallerMemberName] string propertyName = "")
    {
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }
}