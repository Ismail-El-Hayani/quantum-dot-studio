using OxyPlot;
using QuantumDotStudio.Core.Data;
using QuantumDotStudio.Core.Models;
using QuantumDotStudio.Core.Physics;
using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace QuantumDotStudio.WPF.ViewModels;

/// <summary>
/// ViewModel fuer den Sensor-Modus: Konfiguration eines QD-Sensors aus
/// Ligand + Analyt und Darstellung der transduzierten Antwort.
/// Verwendet die reine Physik aus SensorPhysics und die JSON-Datenbanken.
/// </summary>
public class SensorViewModel : INotifyPropertyChanged
{
    // ------------------------------------------------------------- Zustand

    private QuantumDot? _dot;
    private Ligand _selectedLigand;
    private Analyte _selectedAnalyte;
    private double _concentration_M = 1e-6;
    private double _receptorOffset_nm;
    private double _distanceUnboundExtra_nm;
    private double _distanceBoundExtra_nm;
    private PlotModel _responsePlot = new() { Title = "Sensor-Antwort" };

    /// <summary>Das aktive Quantum Dot (Donor bzw. Quench-Objekt).</summary>
    public QuantumDot? Dot
    {
        get => _dot;
        set { _dot = value; OnPropertyChanged(); UpdateResponse(); }
    }

    public List<Ligand> Ligands { get; } = new(SensorDatabase.Ligands);
    public List<Analyte> Analytes { get; } = new(SensorDatabase.Analytes);

    public Ligand SelectedLigand
    {
        get => _selectedLigand;
        set { _selectedLigand = value; OnPropertyChanged(); OnPropertyChanged(nameof(LigandInfo)); UpdateResponse(); }
    }

    public Analyte SelectedAnalyte
    {
        get => _selectedAnalyte;
        set { _selectedAnalyte = value; OnPropertyChanged(); OnPropertyChanged(nameof(AnalyteInfo)); UpdateResponse(); }
    }

    /// <summary>Analyt-Konzentration in M (Quenching) bzw. Aktivitaet (Charge).</summary>
    public double Concentration_M
    {
        get => _concentration_M;
        set { _concentration_M = value; OnPropertyChanged(); OnPropertyChanged(nameof(ConcentrationText)); UpdateResponse(); }
    }

    /// <summary>Zusaetzlicher Abstand des Rezeptors ueber dem Liganden (nm).</summary>
    public double ReceptorOffset_nm
    {
        get => _receptorOffset_nm;
        set { _receptorOffset_nm = value; OnPropertyChanged(); UpdateResponse(); }
    }

    /// <summary>Abstand Donor-Akzeptor im ungebundenen Zustand, zusaetzlich zur Geometrie (nm).</summary>
    public double DistanceUnboundExtra_nm
    {
        get => _distanceUnboundExtra_nm;
        set { _distanceUnboundExtra_nm = value; OnPropertyChanged(); UpdateResponse(); }
    }

    /// <summary>Abstand Donor-Akzeptor im analytgebundenen Zustand, zusaetzlich zur Geometrie (nm).</summary>
    public double DistanceBoundExtra_nm
    {
        get => _distanceBoundExtra_nm;
        set { _distanceBoundExtra_nm = value; OnPropertyChanged(); UpdateResponse(); }
    }

    public string ConcentrationText => Concentration_M switch
    {
        < 1e-9 => $"{Concentration_M:E1} M",
        < 1e-6 => $"{Concentration_M * 1e9:F1} nM",
        < 1e-3 => $"{Concentration_M * 1e6:F2} µM",
        _ => $"{Concentration_M * 1e3:F2} mM"
    };

    public string LigandInfo => _selectedLigand is null
        ? ""
        : $"{_selectedLigand.DisplayName} | Anker: {_selectedLigand.AnchorAtom} | Länge: {_selectedLigand.Length_nm:F1} nm | Ladung: {_selectedLigand.Charge_e:+0;-0}e | Solvens: {_selectedLigand.Solubility}";

    public string AnalyteInfo => _selectedAnalyte is null
        ? ""
        : $"{_selectedAnalyte.DisplayName} | Modus: {_selectedAnalyte.Mode}";

    public PlotModel ResponsePlot
    {
        get => _responsePlot;
        private set { _responsePlot = value; OnPropertyChanged(); }
    }

    /// <summary>Zusammenfassung des aktuellen Sensor-Readouts (Mehr-Zeilen-Text).</summary>
    public string ResponseSummary { get; private set; } = "";

    public SensorViewModel()
    {
        _selectedLigand = Ligands.FirstOrDefault() ?? new Ligand { LigandId = "-", DisplayName = "-" };
        _selectedAnalyte = Analytes.FirstOrDefault() ?? new Analyte { AnalyteId = "-", DisplayName = "-", Mode = "FRET" };
        UpdateResponse();
    }

    // ----------------------------------------------------------- Berechnung

    private void UpdateResponse()
    {
        var dot = Dot;
        if (dot is null)
        {
            ResponsePlot = PlotFactory.CreateSensorPlaceholderPlot("Kein Quantum Dot aktiv");
            ResponseSummary = "Kein Quantum Dot aktiv.";
            return;
        }

        double radius = dot.Radius_nm;
        var ligand = SelectedLigand;
        var analyte = SelectedAnalyte;
        double rUnbound = radius + ligand.Length_nm + ReceptorOffset_nm + DistanceUnboundExtra_nm;
        double rBound = radius + ligand.Length_nm + ReceptorOffset_nm + DistanceBoundExtra_nm;

        ResponseSummary = analyte.Mode switch
        {
            "FRET" => BuildFretSummary(dot, analyte, rUnbound, rBound),
            "Quenching" => BuildQuenchingSummary(analyte, Concentration_M),
            "Charge" => BuildChargeSummary(analyte, Concentration_M),
            "PET" => BuildPetSummary(dot, analyte),
            _ => $"Unbekannter Modus: {analyte.Mode}"
        };
        ResponsePlot = analyte.Mode switch
        {
            "FRET" => PlotFactory.CreateFretResponsePlot(analyte.ForsterRadius_nm, rUnbound, rBound),
            "Quenching" => PlotFactory.CreateQuenchingResponsePlot(analyte.SternVolmerConstant_M, Concentration_M),
            "Charge" => PlotFactory.CreateChargeResponsePlot(analyte.NernstSlope_mV_per_decade),
            "PET" => PlotFactory.CreatePetResponsePlot(dot, analyte),
            _ => PlotFactory.CreateSensorPlaceholderPlot($"Unbekannter Modus {analyte.Mode}")
        };
    }

    private string BuildFretSummary(QuantumDot dot, Analyte analyte, double rUnbound, double rBound)
    {
        double eU = SensorPhysics.FretEfficiency(rUnbound, analyte.ForsterRadius_nm);
        double eB = SensorPhysics.FretEfficiency(rBound, analyte.ForsterRadius_nm);
        double contrast = SensorPhysics.FretContrast(rUnbound, rBound, analyte.ForsterRadius_nm);
        bool resolvable = Math.Abs(rBound - rUnbound) >= 1.0; // Roadmap-Design-Regel

        return $"FRET ({analyte.DisplayName})\n" +
               $"R₀ = {analyte.ForsterRadius_nm:F1} nm\n" +
               $"r(ungebunden) = {rUnbound:F2} nm → E = {eU * 100:F1} %\n" +
               $"r(gebunden)  = {rBound:F2} nm → E = {eB * 100:F1} %\n" +
               $"Kontrast |ΔE|/E = {contrast * 100:F1} %\n" +
               (resolvable
                   ? $"✓ Aufgelöst (Δr = {Math.Abs(rBound - rUnbound):F2} nm ≥ 1 nm Design-Regel)"
                   : $"⚠ Nicht aufgelöst (Δr = {Math.Abs(rBound - rUnbound):F2} nm < 1 nm Design-Regel)");
    }

    private string BuildQuenchingSummary(Analyte analyte, double c)
    {
        double i = SensorPhysics.SternVolmerIntensity(c, analyte.SternVolmerConstant_M);
        double lod = SensorPhysics.QuenchingLOD(analyte.SternVolmerConstant_M);
        bool detectable = c >= lod;

        return $"Quenching ({analyte.DisplayName})\n" +
               $"K_SV = {analyte.SternVolmerConstant_M:E2} M⁻¹\n" +
               $"I/I₀ bei [A] = {c:E2} M → {i * 100:F1} %\n" +
               $"LOD (10 % Signalverlust) = {lod:E2} M\n" +
               (detectable ? $"✓ Über LOD — detektierbar" : $"⚠ Unter LOD — nicht detektierbar");
    }

    private string BuildChargeSummary(Analyte analyte, double c)
    {
        // Konzentration hier als Aktivitaet des Ions interpretieren.
        double psi = SensorPhysics.NernstPotential_mV(c, analyte.NernstSlope_mV_per_decade);
        double shift = SensorPhysics.SurfacePotentialSpectralShift(psi);
        return $"Charge/pH ({analyte.DisplayName})\n" +
               $"Nernst-Steigung = {analyte.NernstSlope_mV_per_decade:F1} mV/Dekade\n" +
               $"Oberflächenpotential ψ = {psi:F1} mV (a = {c:E1})\n" +
               $"Geschätzter Spektralshift Δλ ≈ {shift:F1} nm";
    }

    private string BuildPetSummary(QuantumDot dot, Analyte analyte)
    {
        double ea = dot.Material.ElectronAffinity_eV;
        bool favorable = SensorPhysics.PetIsEnergeticallyFavorable(analyte.RedoxPotential_V, ea);
        double frac = SensorPhysics.PetQuenchFraction(analyte.RedoxPotential_V, ea);
        double cb = ea - 4.44;
        return $"PET ({analyte.DisplayName})\n" +
               $"CB-Kante (NHE) = {cb:F2} V\n" +
               $"E₀(Analyt) = {analyte.RedoxPotential_V:F2} V\n" +
               $"Quench-Fraktion ≈ {frac * 100:F0} %\n" +
               (favorable ? "✓ PET energetisch möglich (ON)" : "✗ Kein PET (OFF)");
    }

    public event PropertyChangedEventHandler? PropertyChanged;

    private void OnPropertyChanged([CallerMemberName] string propertyName = "")
    {
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }
}