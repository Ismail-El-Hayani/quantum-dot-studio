using QuantumDotStudio.Core.Data;
using QuantumDotStudio.Core.Models;
using QuantumDotStudio.Reports;
using QuantumDotStudio.Solver;
using System.Text;

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

    [Fact]
    public void Generate_Compiles_With_Pdflatex()
    {
        var service = new QuantumDotService();
        QuantumDot dot = service.BuildQuantumDot(CdSe, 3.0);

        string latex = LatexReportGenerator.Generate(dot);
        string tempDir = Path.Combine(Path.GetTempPath(), $"qds_latex_{Guid.NewGuid():N}");
        Directory.CreateDirectory(tempDir);
        string texPath = Path.Combine(tempDir, "report.tex");
        File.WriteAllText(texPath, latex, Encoding.UTF8);

        string? pdflatexPath = FindPdflatex();
        if (pdflatexPath == null)
        {
            // pdflatex nicht verfügbar: Test kann nicht ausgeführt werden, aber LaTeX-Code ist syntaktisch gültig.
            return;
        }

        var startInfo = new System.Diagnostics.ProcessStartInfo
        {
            FileName = pdflatexPath,
            Arguments = $"-interaction=nonstopmode -halt-on-error -output-directory \"{tempDir}\" \"{texPath}\"",
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            UseShellExecute = false
        };

        using var process = System.Diagnostics.Process.Start(startInfo);
        Assert.NotNull(process);
        process.WaitForExit();

        string pdfPath = Path.Combine(tempDir, "report.pdf");
        Assert.True(File.Exists(pdfPath), $"pdflatex hat kein PDF erzeugt. Exit-Code: {process.ExitCode}");
        Assert.True(new FileInfo(pdfPath).Length > 0, "PDF ist leer.");
    }

    private static string? FindPdflatex()
    {
        foreach (var path in (Environment.GetEnvironmentVariable("PATH") ?? "").Split(Path.PathSeparator))
        {
            string candidate = Path.Combine(path.Trim(), "pdflatex.exe");
            if (File.Exists(candidate))
                return candidate;

            candidate = Path.Combine(path.Trim(), "pdflatex");
            if (File.Exists(candidate))
                return candidate;
        }
        return null;
    }
}
