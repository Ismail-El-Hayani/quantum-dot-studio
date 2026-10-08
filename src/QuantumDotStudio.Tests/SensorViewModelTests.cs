using QuantumDotStudio.Core.Data;
using QuantumDotStudio.Core.Models;
using QuantumDotStudio.Solver;
using QuantumDotStudio.WPF.ViewModels;

namespace QuantumDotStudio.Tests;

/// <summary>
/// Tests fuer die Sensor-ViewModel-Pipeline: aktives QD + Ligand + Analyt
/// muessen zu einem plausiblen Readout fuehren (Phase-2-Exit-Kriterium).
/// </summary>
public class SensorViewModelTests
{
    private static (SensorViewModel vm, QuantumDot dot) CreateConfigured()
    {
        var service = new QuantumDotService();
        var cdse = MaterialDatabase.Defaults.First(m => m.Name == "CdSe");
        var dot = service.BuildQuantumDot(cdse, 3.0);

        var vm = new SensorViewModel
        {
            Dot = dot,
            SelectedLigand = SensorDatabase.Ligands.First(l => l.LigandId == "MPA"),
            SelectedAnalyte = SensorDatabase.Analytes.First(a => a.AnalyteId == "Fluorescein")
        };
        return (vm, dot);
    }

    [Fact]
    public void Fret_Configuration_Produces_Resolvable_Summary()
    {
        // Roadmap-Exit-Kriterium: CdSe/ZnS-MPA-Fluorescein-FRET-Paar konfigurierbar,
        // ratiometrische Antwort darstellbar.
        var (vm, dot) = CreateConfigured();

        // Ungbunden: Ligand frei (extra 0); gebunden: Analyt zieht den Akzeptor
        // 1.5 nm naeher (konformativer Fall).
        vm.DistanceUnboundExtra_nm = 1.5;
        vm.DistanceBoundExtra_nm = 0.0;

        Assert.Contains("FRET", vm.ResponseSummary);
        Assert.Contains("R₀", vm.ResponseSummary);
        Assert.Contains("✓", vm.ResponseSummary); // aufgeloest: Δr = 1.5 >= 1
        Assert.True(vm.ResponsePlot.Series.Count > 0, "FRET-Plot muss eine Kurve haben");
    }

    [Fact]
    public void Fret_Unresolvable_Delta_Warns()
    {
        var (vm, _) = CreateConfigured();
        vm.DistanceUnboundExtra_nm = 0.0;
        vm.DistanceBoundExtra_nm = 0.3; // Δr = 0.3 < 1 nm

        Assert.Contains("⚠", vm.ResponseSummary);
    }

    [Fact]
    public void Quenching_Configuration_Shows_LOD_Verdict()
    {
        var service = new QuantumDotService();
        var cdse = MaterialDatabase.Defaults.First(m => m.Name == "CdSe");
        var vm = new SensorViewModel
        {
            Dot = service.BuildQuantumDot(cdse, 3.0),
            SelectedAnalyte = SensorDatabase.Analytes.First(a => a.AnalyteId == "Pb2+"),
            Concentration_M = 1e-6
        };

        Assert.Contains("K_SV", vm.ResponseSummary);
        // 1e-6 M >> LOD 2.6e-7 M -> detektierbar
        Assert.Contains("✓", vm.ResponseSummary);
    }

    [Fact]
    public void Charge_pH7_Shows_Nernst_Value()
    {
        var service = new QuantumDotService();
        var cdse = MaterialDatabase.Defaults.First(m => m.Name == "CdSe");
        var vm = new SensorViewModel
        {
            Dot = service.BuildQuantumDot(cdse, 3.0),
            SelectedAnalyte = SensorDatabase.Analytes.First(a => a.AnalyteId == "H+"),
            Concentration_M = 1e-7 // pH 7
        };

        Assert.Contains("Nernst", vm.ResponseSummary);
        Assert.Contains("-414", vm.ResponseSummary); // 59.16 * (-7) = -414.1 mV
    }

    [Fact]
    public void Dot_Change_Updates_Sensor_Response()
    {
        var (vm, dot) = CreateConfigured();
        string summaryBefore = vm.ResponseSummary;

        var service = new QuantumDotService();
        var inp = MaterialDatabase.Defaults.First(m => m.Name == "InP");
        vm.Dot = service.BuildQuantumDot(inp, 5.0);

        // Der FRET-Abstand haengt vom Radius ab -> Zusammenfassung muss sich aendern.
        Assert.NotEqual(summaryBefore, vm.ResponseSummary);
    }

    [Fact]
    public void MainViewModel_Syncs_Sensor_With_Simulation()
    {
        var sim = new SimulationViewModel(new QuantumDotService());
        var sensor = new SensorViewModel();
        var export = new ExportViewModel();
        _ = new MainViewModel(sim, sensor, export);

        Assert.NotNull(sensor.Dot);
        Assert.Same(sim.ActiveDot, sensor.Dot);
    }
}