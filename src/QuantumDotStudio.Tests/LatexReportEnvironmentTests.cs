using QuantumDotStudio.Core.Data;
using QuantumDotStudio.Core.Models;
using QuantumDotStudio.Core.Physics;
using QuantumDotStudio.Reports;
using QuantumDotStudio.Solver;

namespace QuantumDotStudio.Tests;

/// <summary>
/// Tests für die Umgebungsangaben im LaTeX-Report (Roadmap 2.3): ein Report
/// muss erkennbar machen, UNTER welchen Bedingungen (Temperatur, Solvens,
/// pH, effektive Dielektrizität) gerechnet wurde — und darf für einen
/// Core/Shell-Dot nicht den unendlichen Potentialtopf behaupten.
/// </summary>
public class LatexReportEnvironmentTests
{
    private static Material Get(string name) =>
        MaterialDatabase.Defaults.Single(m => m.Name == name);

    private static EnvironmentSettings WaterEnvironment() => new EnvironmentSettings(
        Solvent: SolventDatabase.Defaults.Single(s => s.Name == "Wasser"),
        Temperature_K: 373.0,
        Ph: 7.0);

    [Fact]
    public void Report_Contains_Environment_Parameters()
    {
        var service = new QuantumDotService();
        var dot = service.BuildQuantumDot(Get("CdSe"), 3.0, environment: WaterEnvironment());

        string latex = LatexReportGenerator.Generate(dot);

        Assert.Contains(@"Temperatur & 373\,K", latex);
        Assert.Contains("Solvens & Wasser", latex);
        // Deutsches Dezimalkomma: der Report formatiert deterministisch mit de-DE.
        Assert.Contains(@"pH & 7,0", latex);
        Assert.Contains(@"$\varepsilon_r^{\text{eff}}$", latex);
        Assert.Contains("Umgebung:", latex);
    }

    [Fact]
    public void Report_Without_Environment_Has_No_Environment_Rows()
    {
        var service = new QuantumDotService();
        var dot = service.BuildQuantumDot(Get("CdSe"), 3.0);

        string latex = LatexReportGenerator.Generate(dot);

        Assert.DoesNotContain("Solvens &", latex);
        Assert.DoesNotContain("Umgebung:", latex);
    }

    [Fact]
    public void CoreShell_Report_Does_Not_Claim_The_Infinite_Well()
    {
        var service = new QuantumDotService();
        var bare = service.BuildQuantumDot(Get("CdSe"), 3.0);
        var coreShell = service.BuildCoreShellQuantumDot(Get("CdSe"), Get("CdS"), 3.0, 1.0);

        string bareLatex = LatexReportGenerator.Generate(bare);
        string coreShellLatex = LatexReportGenerator.Generate(coreShell);

        Assert.Contains(@"Unendlich hoher sphärischer Potentialtopf", bareLatex);
        Assert.Contains(@"Endlicher sphärischer Potentialtopf im Core", coreShellLatex);
        Assert.DoesNotContain(@"Unendlich hoher sphärischer Potentialtopf", coreShellLatex);
    }
}