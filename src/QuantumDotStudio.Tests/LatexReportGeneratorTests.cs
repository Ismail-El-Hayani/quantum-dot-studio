using QuantumDotStudio.Core.Data;
using QuantumDotStudio.Core.Models;
using QuantumDotStudio.Reports;
using QuantumDotStudio.Solver;

namespace QuantumDotStudio.Tests;

public class LatexReportGeneratorTests
{
    private static readonly Material CdSe = MaterialDatabase.Defaults.Single(m => m.Name == "CdSe");

    [Fact]
    public void Generate_Contains_Material_Name()
    {
        var service = new QuantumDotService();
        QuantumDot dot = service.BuildQuantumDot(CdSe, 3.0);

        string latex = LatexReportGenerator.Generate(dot);

        Assert.Contains("CdSe", latex);
    }

    [Fact]
    public void Generate_Contains_Energy_Levels_Table()
    {
        var service = new QuantumDotService();
        QuantumDot dot = service.BuildQuantumDot(CdSe, 3.0);

        string latex = LatexReportGenerator.Generate(dot);

        Assert.Contains(@"\section{Berechnete Energieniveaus}", latex);
        Assert.Contains("1S", latex);
    }

    [Fact]
    public void Generate_Contains_Emission_Wavelength()
    {
        var service = new QuantumDotService();
        QuantumDot dot = service.BuildQuantumDot(CdSe, 3.0);

        string latex = LatexReportGenerator.Generate(dot);

        Assert.Contains(dot.EmissionWavelength_nm.ToString("F1"), latex);
        Assert.Contains("Emissionsfarbe", latex);
    }

    [Fact]
    public void Generate_Is_Valid_Latex_Document_Structure()
    {
        var service = new QuantumDotService();
        QuantumDot dot = service.BuildQuantumDot(CdSe, 3.0);

        string latex = LatexReportGenerator.Generate(dot);

        Assert.StartsWith(@"\documentclass", latex);
        Assert.Contains(@"\begin{document}", latex);
        Assert.Contains(@"\end{document}", latex);
    }

    [Theory]
    [InlineData(400)]
    [InlineData(550)]
    [InlineData(700)]
    public void WavelengthToRgb_Returns_Non_Empty_Triple(double wavelength_nm)
    {
        string rgb = LatexReportGenerator.WavelengthToRgb(wavelength_nm);

        Assert.False(string.IsNullOrWhiteSpace(rgb));
        Assert.Contains(",", rgb);
    }

    [Fact]
    public void Escape_Replaces_Latex_Special_Characters()
    {
        string escaped = LatexReportGenerator.Escape("100% & $R_0$ #1 {test}");

        Assert.Contains(@"\%", escaped);
        Assert.Contains(@"\&", escaped);
        Assert.Contains(@"\$R\_0\$", escaped);
        Assert.Contains(@"\#1", escaped);
        Assert.Contains(@"\{test\}", escaped);
    }
}
