using QuantumDotStudio.Core.Data;
using QuantumDotStudio.Core.Models;
using QuantumDotStudio.Solver;

namespace QuantumDotStudio.Tests;

/// <summary>
/// Tests für die Monte-Carlo-Weitergabe: `Perturb()` erzeugt die Stichproben
/// des Unsicherheitsbands. Felder, die es NICHT weitergibt (Umgebung/pH,
/// Interferent), verschwinden im Band — das Band wäre dann schmaler und
/// optimistischer als die Punkt-Schätzung. Genau das war der Defekt.
/// </summary>
public class FeasibilityMonteCarloTests
{
    private static Material Get(string name) =>
        MaterialDatabase.Defaults.Single(m => m.Name == name);

    private static SensorDesign BaseDesign() => new SensorDesign
    {
        CoreMaterial = Get("CdSe"),
        ShellMaterial = Get("CdS"),
        CoreRadius_nm = 3.0,
        ShellThickness_nm = 0.6,
        GradedInterface = true,
        EmissionWavelength_nm = QuantumSolver.WavelengthFromBandGap(
            QuantumSolver.BrusBandGap(Get("CdSe"), 3.0)),
        Ligand = SensorDatabase.Ligands.Single(l => l.LigandId == "MPA"),
        Analyte = SensorDatabase.Analytes.Single(a => a.AnalyteId == "Pb2+"),
        Application = new ApplicationTemplate
        {
            ApplicationId = "test",
            Medium = "aqueous",
            EmissionWindowMin_nm = 550.0,
            EmissionWindowMax_nm = 650.0
        },
        TargetConcentration_M = 1e-6
    };

    [Fact]
    public void MonteCarlo_Band_Reflects_The_Interferent()
    {
        // Hg(II) quencht staerker als Pb(II) -> Selektivitaetsfaktor < 1 ->
        // JEDE Stichprobe muss niedriger liegen. Ohne Weitergabe des
        // Interferenten in Perturb waeren die Bander identisch.
        var clean = BaseDesign();
        var interfered = BaseDesign();
        interfered.Interferent = SensorDatabase.Analytes.Single(a => a.AnalyteId == "Hg2+");
        interfered.InterferentConcentration_M = 1e-6;

        var rClean = FeasibilityEngine.EvaluateMonteCarlo(clean, n: 400, seed: 42);
        var rInterfered = FeasibilityEngine.EvaluateMonteCarlo(interfered, n: 400, seed: 42);

        Assert.True(rInterfered.McMedian < rClean.McMedian,
            $"MC-Median mit Interferent ({rInterfered.McMedian:F4}) muss kleiner sein als ohne ({rClean.McMedian:F4})");
    }

    [Fact]
    public void MonteCarlo_Band_Reflects_The_Environment_Ph()
    {
        // pH 3 liegt ausserhalb des MPA-Fensters (4-11) -> Faktor 5 x 0.25
        // in JEDER Stichprobe. Ohne Weitergabe der Umgebung in Perturb
        // waeren beide Bander identisch.
        var inWindow = BaseDesign();
        inWindow.Environment = new EnvironmentSettings(Ph: 7.0);

        var outOfWindow = BaseDesign();
        outOfWindow.Environment = new EnvironmentSettings(Ph: 3.0);

        var rIn = FeasibilityEngine.EvaluateMonteCarlo(inWindow, n: 400, seed: 42);
        var rOut = FeasibilityEngine.EvaluateMonteCarlo(outOfWindow, n: 400, seed: 42);

        Assert.True(rOut.McMedian < rIn.McMedian,
            $"MC-Median bei pH 3 ({rOut.McMedian:F4}) muss kleiner sein als bei pH 7 ({rIn.McMedian:F4})");
    }
}