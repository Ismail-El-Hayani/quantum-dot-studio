using QuantumDotStudio.Core.Data;
using QuantumDotStudio.Core.Models;
using QuantumDotStudio.Reports;
using QuantumDotStudio.Solver;

namespace QuantumDotStudio.Tests;

/// <summary>
/// Tests fuer die Phase-3-Exit-Kriterien: Machbarkeits-Kapitel im LaTeX-Export
/// (Roadmap 3.3) und benutzer-tunbare Gewichte aus Data/feasibility_weights.json
/// (Roadmap 3.1, AC-004).
/// </summary>
public class FeasibilityExportTests
{
    private static SensorDesign SampleDesign()
    {
        var cdse = MaterialDatabase.Defaults.First(m => m.Name == "CdSe");
        return new SensorDesign
        {
            CoreMaterial = cdse,
            ShellMaterial = MaterialDatabase.Defaults.First(m => m.Name == "CdS"),
            CoreRadius_nm = 3.0,
            ShellThickness_nm = 0.6,
            EmissionWavelength_nm = QuantumSolver.WavelengthFromBandGap(QuantumSolver.BrusBandGap(cdse, 3.0)),
            Ligand = SensorDatabase.Ligands.First(l => l.LigandId == "MPA"),
            Analyte = SensorDatabase.Analytes.First(a => a.AnalyteId == "Fluorescein"),
            Application = TemplateDatabase.Templates.First(t => t.ApplicationId == "visible-imaging"),
            TargetConcentration_M = 1e-9,
            ReceptorKd_M = 1e-9,
            FretDistanceUnbound_nm = 5.1,
            FretDistanceBound_nm = 3.6
        };
    }

    private static QuantumDot SampleDot()
    {
        var service = new QuantumDotService();
        var cdse = MaterialDatabase.Defaults.First(m => m.Name == "CdSe");
        return service.BuildQuantumDot(cdse, 3.0);
    }

    [Fact]
    public void Feasibility_Chapter_Appears_In_Latex()
    {
        var dot = SampleDot();
        var result = FeasibilityEngine.Evaluate(SampleDesign());

        string latex = LatexReportGenerator.Generate(dot, feasibility: result);

        Assert.Contains("Machbarkeit des Sensor-Entwurfs", latex);
        Assert.Contains("P(Erfolg)", latex);
        Assert.Contains("Emissionsfenster", latex);
        Assert.Contains("Verspannung", latex);
        Assert.Contains("Quanteneffizienz", latex);
        Assert.Contains("Transduktion", latex);
        Assert.Contains("Stabilitaet", latex);       // Faktor-Zeilen
        Assert.Contains("Bioconjugation", latex);
        Assert.Contains("Geometrisches Mittel", latex);
    }

    [Fact]
    public void Feasibility_Chapter_Includes_MonteCarlo_Band()
    {
        var dot = SampleDot();
        var result = FeasibilityEngine.EvaluateMonteCarlo(SampleDesign(), n: 500, seed: 42);

        string latex = LatexReportGenerator.Generate(dot, feasibility: result);

        Assert.Contains("Monte-Carlo", latex);
        Assert.Contains("Median", latex);
        Assert.Contains("95", latex); // Band-Angabe
        Assert.Contains("Varianzzerlegung", latex);
    }

    [Fact]
    public void Feasibility_Chapter_Omitted_When_No_Result()
    {
        string latex = LatexReportGenerator.Generate(SampleDot());
        Assert.DoesNotContain("Machbarkeit", latex);
    }

    [Fact]
    public void Weights_Are_Loaded_From_Deployed_Json()
    {
        Assert.NotEmpty(TemplateDatabase.Templates);
        Assert.NotNull(TemplateDatabase.LoadedFrom);
        Assert.True(File.Exists(TemplateDatabase.LoadedFrom!), "JSON muss physisch existieren");

        // Schwermetall-Template hat vom Standard abweichende Gewichte:
        // Transduktion 0.35 statt 0.15 — beweist, dass die JSON wirklich gelesen wird.
        var hm = TemplateDatabase.Templates.First(t => t.ApplicationId == "heavy-metal-sensing");
        Assert.Equal(0.35, hm.WeightTransduction, 2);
        Assert.Equal(0.15, hm.WeightEmissionWindow, 2);
    }

    [Fact]
    public void Template_Weights_Sum_To_One()
    {
        foreach (var t in TemplateDatabase.Templates)
        {
            double sum = t.WeightEmissionWindow + t.WeightStrain + t.WeightQuantumYield
                       + t.WeightTransduction + t.WeightStability + t.WeightBioconjugation;
            Assert.Equal(1.0, sum, 2);
        }
    }
}