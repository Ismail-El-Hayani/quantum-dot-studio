using QuantumDotStudio.Core.Data;
using QuantumDotStudio.Core.Models;
using QuantumDotStudio.Reports;
using QuantumDotStudio.Solver;

namespace QuantumDotStudio.Tests;

/// <summary>
/// Tests für die robuste Report-API (LatexBuilder/ReportResult/TryGenerate):
/// TryGenerate wirft nie, sammelt recoverbare Probleme und liefert trotzdem
/// ein Dokument; Generate delegiert und wirft nur noch bei echten Fehlern.
/// </summary>
public class LatexReportApiTests
{
    private static readonly Material CdSe = MaterialDatabase.Defaults.Single(m => m.Name == "CdSe");

    private static QuantumDot CdSe3nm() => new QuantumDotService().BuildQuantumDot(CdSe, 3.0);

    [Fact]
    public void TryGenerate_With_Null_Dot_Returns_Failure()
    {
        var result = LatexReportGenerator.TryGenerate(null!);

        Assert.False(result.Success);
        Assert.Contains("QuantumDot ist null.", result.Errors);
        Assert.Equal(string.Empty, result.Document);
    }

    [Fact]
    public void TryGenerate_With_Missing_Material_Produces_Document_And_Error()
    {
        var dot = CdSe3nm();
        dot.Material = null!;

        var result = LatexReportGenerator.TryGenerate(dot);

        Assert.False(result.Success);
        Assert.Contains(result.Errors, e => e.Contains("Material"));
        Assert.Contains(@"\documentclass", result.Document);
    }

    [Fact]
    public void TryGenerate_With_Missing_Collections_Reports_Them()
    {
        var dot = CdSe3nm();
        dot.Atoms = null!;
        dot.EnergyLevels = null!;

        var result = LatexReportGenerator.TryGenerate(dot);

        Assert.False(result.Success);
        Assert.Contains(result.Errors, e => e.Contains("Atoms"));
        Assert.Contains(result.Errors, e => e.Contains("EnergyLevels"));
        Assert.Contains(@"\documentclass", result.Document);
    }

    [Fact]
    public void Generate_Throws_Only_When_The_Report_Carries_Errors()
    {
        var broken = CdSe3nm();
        broken.Material = null!;

        Assert.Throws<InvalidOperationException>(() => LatexReportGenerator.Generate(broken));
    }

    [Fact]
    public void Generate_And_TryGenerate_Produce_The_Same_Document()
    {
        var dot = CdSe3nm();

        var result = LatexReportGenerator.TryGenerate(dot);
        Assert.True(result.Success);
        Assert.Equal(LatexReportGenerator.Generate(dot), result.Document);
    }
}