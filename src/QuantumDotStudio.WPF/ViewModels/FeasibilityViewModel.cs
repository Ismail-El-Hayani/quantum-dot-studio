using QuantumDotStudio.Core.Data;
using QuantumDotStudio.Core.Models;
using QuantumDotStudio.Solver;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Windows;
using System.Windows.Media;

namespace QuantumDotStudio.WPF.ViewModels;

/// <summary>
/// ViewModel fuer das Machbarkeits-Panel (Roadmap 3.3): bewertet den
/// aktuellen Sensor-Entwurf gegen ein Anwendungs-Template, zeigt Ampel-
/// Verdikt, Faktor-Balken mit Verbesserungsvorschlaegen und das
/// Monte-Carlo-Band mit Varianzzerlegung.
/// </summary>
public class FeasibilityViewModel : INotifyPropertyChanged
{
    private QuantumDot? _dot;
    private Ligand? _ligand;
    private Analyte? _analyte;
    private ApplicationTemplate _selectedTemplate;
    private double _targetConcentration_M = 1e-9;
    private double? _receptorKd_M = 1e-9;
    private double _receptorOffset_nm;
    private double _distanceUnboundExtra_nm;
    private double _distanceBoundExtra_nm;
    private FeasibilityResult? _result;
    private bool _isRunning;

    public List<ApplicationTemplate> Templates { get; } = new()
    {
        // AC-004-Pattern: diese Liste laesst sich durch Data/feasibility_weights.json
        // erweitern (Phase 4); die Defaults decken die Roadmap-3.4-Szenarien ab.
        new ApplicationTemplate
        {
            ApplicationId = "bio-imaging-nir",
            DisplayName = "Bio-Imaging (NIR-I, 650–900 nm)",
            EmissionWindowMin_nm = 650, EmissionWindowMax_nm = 900,
            Medium = "aqueous"
        },
        new ApplicationTemplate
        {
            ApplicationId = "visible-imaging",
            DisplayName = "Sichtbare Fluoreszenz (500–650 nm)",
            EmissionWindowMin_nm = 500, EmissionWindowMax_nm = 650,
            Medium = "aqueous"
        },
        new ApplicationTemplate
        {
            ApplicationId = "heavy-metal-sensing",
            DisplayName = "Schwermetall-Sensor (Pb²⁺/Hg²⁺, sichtbar)",
            EmissionWindowMin_nm = 480, EmissionWindowMax_nm = 680,
            Medium = "aqueous"
        }
    };

    public QuantumDot? Dot
    {
        get => _dot;
        set { _dot = value; OnPropertyChanged(); }
    }

    public Ligand? SelectedLigand
    {
        get => _ligand;
        set { _ligand = value; OnPropertyChanged(); }
    }

    public Analyte? SelectedAnalyte
    {
        get => _analyte;
        set { _analyte = value; OnPropertyChanged(); }
    }

    public ApplicationTemplate SelectedTemplate
    {
        get => _selectedTemplate;
        set { _selectedTemplate = value; OnPropertyChanged(); }
    }

    public double TargetConcentration_M
    {
        get => _targetConcentration_M;
        set { _targetConcentration_M = value; OnPropertyChanged(); }
    }

    public double? ReceptorKd_M
    {
        get => _receptorKd_M;
        set { _receptorKd_M = value; OnPropertyChanged(); }
    }

    public double ReceptorOffset_nm
    {
        get => _receptorOffset_nm;
        set { _receptorOffset_nm = value; OnPropertyChanged(); }
    }

    public double DistanceUnboundExtra_nm
    {
        get => _distanceUnboundExtra_nm;
        set { _distanceUnboundExtra_nm = value; OnPropertyChanged(); }
    }

    public double DistanceBoundExtra_nm
    {
        get => _distanceBoundExtra_nm;
        set { _distanceBoundExtra_nm = value; OnPropertyChanged(); }
    }

    public FeasibilityResult? Result
    {
        get => _result;
        private set
        {
            _result = value;
            OnPropertyChanged();
            OnPropertyChanged(nameof(VerdictColor));
            OnPropertyChanged(nameof(VerdictText));
            OnPropertyChanged(nameof(ProbabilityText));
            OnPropertyChanged(nameof(MonteCarloText));
            OnPropertyChanged(nameof(HasMonteCarlo));
        }
    }

    public bool IsRunning
    {
        get => _isRunning;
        private set { _isRunning = value; OnPropertyChanged(); }
    }

    public Brush VerdictColor => Result?.Verdict switch
    {
        "green" => new SolidColorBrush(Colors.ForestGreen),
        "amber" => new SolidColorBrush(Colors.DarkOrange),
        "red" => new SolidColorBrush(Colors.Crimson),
        _ => new SolidColorBrush(Colors.Gray)
    };

    public string VerdictText => Result is null
        ? "—"
        : Result.Verdict switch
        {
            "green" => "GRÜN — machbar",
            "amber" => "GELB — riskant",
            _ => "ROT — nicht empfohlen"
        };

    public string ProbabilityText => Result is null
        ? "—"
        : $"P(Erfolg) = {Result.P * 100:F0} %";

    public bool HasMonteCarlo => Result?.HasMonteCarlo == true;

    public string MonteCarloText => Result is not { HasMonteCarlo: true }
        ? ""
        : $"Monte Carlo (N = 2000): Median {Result.McMedian * 100:F0} %, " +
          $"Band 5–95 %: {Result.McP05 * 100:F0}–{Result.McP95 * 100:F0} %";

    public FeasibilityViewModel()
    {
        _selectedTemplate = Templates[0];
        EvaluateCommand = new RelayCommand(_ => _ = EvaluateAsync(), _ => Dot is not null && SelectedLigand is not null && SelectedAnalyte is not null && !IsRunning);
    }

    /// <summary>Löst die Bewertung aus (Button im Machbarkeits-Tab).</summary>
    public System.Windows.Input.ICommand EvaluateCommand { get; }

    /// <summary>
    /// Bewertet den aktuellen Entwurf (Hintergrund-Thread, MC N=2000).
    /// Kann erneut aufgerufen werden, waehrend ein Lauf aktiv ist — der
    /// letzte Aufruf gewinnt (einfacher Sequenz-Guard).
    /// </summary>
    public async Task EvaluateAsync()
    {
        if (Dot is null || SelectedLigand is null || SelectedAnalyte is null)
            return;

        IsRunning = true;
        var design = BuildDesign(); // Momentaufnahme auf dem UI-Thread
        int seq = ++_sequence;

        var result = await Task.Run(() => FeasibilityEngine.EvaluateMonteCarlo(design, n: 2000, seed: 42));

        if (seq != _sequence)
            return; // veraltet

        Result = result;
        IsRunning = false;
    }

    private int _sequence;

    internal SensorDesign BuildDesign()
    {
        var dot = Dot!;
        var csDot = dot as CoreShellQuantumDot;

        double coreR = csDot?.CoreRadius_nm ?? dot.Radius_nm;
        double shellT = csDot?.ShellThickness_nm ?? 0.0;

        return new SensorDesign
        {
            CoreMaterial = dot.Material,
            ShellMaterial = csDot?.ShellMaterial,
            CoreRadius_nm = coreR,
            ShellThickness_nm = shellT,
            GradedInterface = false,
            EmissionWavelength_nm = dot.EmissionWavelength_nm,
            Ligand = SelectedLigand,
            Analyte = SelectedAnalyte,
            Application = SelectedTemplate,
            TargetConcentration_M = TargetConcentration_M,
            ReceptorKd_M = ReceptorKd_M,
            FretDistanceUnbound_nm = coreR + SelectedLigand!.Length_nm + ReceptorOffset_nm + DistanceUnboundExtra_nm,
            FretDistanceBound_nm = coreR + SelectedLigand.Length_nm + ReceptorOffset_nm + DistanceBoundExtra_nm
        };
    }

    public event PropertyChangedEventHandler? PropertyChanged;

    private void OnPropertyChanged([CallerMemberName] string propertyName = "")
    {
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }
}