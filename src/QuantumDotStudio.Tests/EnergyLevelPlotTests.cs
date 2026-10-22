using OxyPlot.Annotations;
using OxyPlot.Series;
using QuantumDotStudio.Core.Data;
using QuantumDotStudio.Core.Models;
using QuantumDotStudio.Solver;
using QuantumDotStudio.WPF.ViewModels;

namespace QuantumDotStudio.Tests;

/// <summary>
/// Tests für das Energieniveaus-Diagramm (FR-006): physikalisches Bandschema
/// (Valenzbandkante bei 0, Leitungsbandkante bei E_g, Niveaus relativ dazu)
/// mit Vakuum-Energie als Referenz.
/// </summary>
public class EnergyLevelPlotTests
{
    private static readonly Material CdSe = MaterialDatabase.Defaults.Single(m => m.Name == "CdSe");

    private static QuantumDot CdSe3nm() => new QuantumDotService().BuildQuantumDot(CdSe, 3.0);

    [Fact]
    public void Plot_Contains_Vacuum_Reference_Line()
    {
        var model = PlotFactory.CreateEnergyLevelPlot(CdSe3nm());

        var vacuum = Assert.Single(model.Annotations.OfType<LineAnnotation>(),
            a => a.Text?.StartsWith("Vakuum") == true);

        // E_vac = E_CB + Elektronenaffinität (Ionisierungsenergie).
        Assert.Equal(CdSe.BandGap_eV + CdSe.ElectronAffinity_eV, vacuum!.X, tolerance: 1e-9);
    }

    [Fact]
    public void Plot_Contains_Band_Edge_References()
    {
        var model = PlotFactory.CreateEnergyLevelPlot(CdSe3nm());

        var lines = model.Annotations.OfType<LineAnnotation>().ToList();

        Assert.Contains(lines, a => a.Text == "VBM" && Math.Abs(a.X) < 1e-12);
        Assert.Contains(lines, a => a.Text == "CBM" && Math.Abs(a.X - CdSe.BandGap_eV) < 1e-12);
    }

    [Fact]
    public void Electron_Levels_Sit_Above_Cbm_Hole_Levels_Below_Vbm()
    {
        var model = PlotFactory.CreateEnergyLevelPlot(CdSe3nm());

        var electron = model.Series.OfType<BarSeries>().Single(s => s.Title == "Elektron");
        var hole = model.Series.OfType<BarSeries>().Single(s => s.Title == "Loch");

        Assert.NotEmpty(electron.Items);
        Assert.NotEmpty(hole.Items);
        Assert.All(electron.Items, b => Assert.True(b.Value > CdSe.BandGap_eV,
            $"Elektronenniveau {b.Value:F3} eV muss über der Leitungsbandkante ({CdSe.BandGap_eV:F3} eV) liegen"));
        Assert.All(hole.Items, b => Assert.True(b.Value < 0.0,
            $"Lochniveau {b.Value:F3} eV muss unter der Valenzbandkante liegen"));
    }

    [Fact]
    public void Axis_Covers_Vacuum_Level_And_Hole_Levels()
    {
        var dot = CdSe3nm();
        var model = PlotFactory.CreateEnergyLevelPlot(dot);

        var axis = model.Axes.OfType<OxyPlot.Axes.LinearAxis>()
            .Single(a => a.Position == OxyPlot.Axes.AxisPosition.Bottom);

        double eVac = CdSe.BandGap_eV + CdSe.ElectronAffinity_eV;
        Assert.True(axis.Maximum >= eVac, $"Achse muss bis zum Vakuum-Niveau reichen ({axis.Maximum:F2} < {eVac:F2})");
        Assert.True(axis.Minimum < 0.0, "Achse muss unter die Valenzbandkante reichen (Lochniveaus)");
    }
}