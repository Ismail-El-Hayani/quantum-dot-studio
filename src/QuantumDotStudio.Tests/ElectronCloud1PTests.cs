using QuantumDotStudio.Core.Data;
using QuantumDotStudio.Core.Models;
using QuantumDotStudio.Renderer;
using QuantumDotStudio.Solver;

namespace QuantumDotStudio.Tests;

/// <summary>
/// Tests für die 1P-Elektronenwolke (n=1, l=1, m=0) — FR-005:
/// Grundzustand UND erster angeregter Zustand müssen berechenbar sein.
/// </summary>
public class ElectronCloud1PTests
{
    private static readonly Material CdSe = MaterialDatabase.Defaults.Single(m => m.Name == "CdSe");

    [Fact]
    public void Cloud1P_Is_Not_Empty()
    {
        var gen = new ProbabilityCloudGenerator();
        var cloud = gen.GenerateElectronCloud1P(3.0).ToList();

        Assert.NotEmpty(cloud);
        Assert.All(cloud, p => Assert.InRange(p.Probability, 0.05, 1.0));
    }

    [Fact]
    public void Cloud1P_Has_Zero_Density_At_Origin()
    {
        // j_1(0) = 0: die 1P-Dichte verschwindet am Ursprung (Knotenfläche).
        var gen = new ProbabilityCloudGenerator { GridResolution = 20 };
        var cloud = gen.GenerateElectronCloud1P(3.0).ToList();

        // Kein Punkt darf näher als 0.3 nm am Ursprung liegen (Näherung des Knotens).
        Assert.All(cloud, p => Assert.True(p.Position.Length() > 0.3,
            $"Punkt bei |r|={p.Position.Length()} liegt zu nah am Ursprung"));
    }

    [Fact]
    public void Cloud1P_Peak_Lies_In_Middle_Radial_Range()
    {
        // Maximum von |R(r)|² für 1P: r ≈ 0.645·R — also im mittleren Radiusbereich,
        // weder am Zentrum noch an der Oberfläche.
        var gen = new ProbabilityCloudGenerator { GridResolution = 24 };
        var cloud = gen.GenerateElectronCloud1P(3.0).ToList();
        double R = 3.0;

        var peak = cloud.OrderByDescending(p => p.Probability).First();
        double rPeak = peak.Position.Length();

        Assert.InRange(rPeak / R, 0.4, 0.85);
    }

    [Fact]
    public void Cloud1P_Is_Symmetric_Under_Z_Flip()
    {
        // m=0-Zustand: Dichte invariant unter z → −z.
        var gen = new ProbabilityCloudGenerator { GridResolution = 20 };
        var cloud = gen.GenerateElectronCloud1P(3.0).ToList();

        double densityAbove = cloud.Where(p => p.Position.Z > 0.5).Sum(p => p.Probability);
        double densityBelow = cloud.Where(p => p.Position.Z < -0.5).Sum(p => p.Probability);

        Assert.True(Math.Abs(densityAbove - densityBelow) / Math.Max(densityAbove, densityBelow) < 0.05,
            $"Asymmetrie: oben {densityAbove:F2} vs. unten {densityBelow:F2}");
    }

    [Fact]
    public void Cloud1P_Density_Vanishes_In_XY_Plane()
    {
        // cos²(θ)=0 in der Äquatorebene: die 1P(m=0)-Dichte hat einen Knoten in z=0.
        var gen = new ProbabilityCloudGenerator { GridResolution = 20 };
        var cloud = gen.GenerateElectronCloud1P(3.0).ToList();

        var equatorPoints = cloud.Where(p => Math.Abs(p.Position.Z) < 0.2).ToList();
        Assert.Empty(equatorPoints);
    }

    [Fact]
    public void EnsureElectronCloud1P_Is_Lazy_And_Cached()
    {
        var service = new QuantumDotService();
        var dot = service.BuildQuantumDot(CdSe, 2.0);

        Assert.Empty(dot.ElectronCloud1P); // lazy: nicht automatisch erzeugt

        service.EnsureElectronCloud1P(dot);
        int countAfterFirst = dot.ElectronCloud1P.Count;
        Assert.True(countAfterFirst > 0);

        // Zweiter Aufruf darf nichts ändern (cached im Objekt).
        service.EnsureElectronCloud1P(dot);
        Assert.Equal(countAfterFirst, dot.ElectronCloud1P.Count);
    }

    [Fact]
    public void Renderer_MeshMerge_Produces_Child_Per_Element()
    {
        // Statt einem GeometryModel3D pro Atom (tausende Kinder) entsteht
        // jetzt genau ein Kind pro Element (+ ggf. Wolken).
        var service = new QuantumDotService();
        var dot = service.BuildQuantumDot(CdSe, 3.0);

        var renderer = new QuantumDotRenderer3D();
        var model = renderer.BuildModel(dot, showLattice: true, showCloud: false, showCloud1P: false);

        // CdSe: 2 Elemente -> 2 Kinder (statt ~4000).
        Assert.Equal(2, model.Children.Count);
        Assert.True(model.Children.Count < 100, "Mesh-Merge ineffektiv: zu viele Kinder");
    }

    [Fact]
    public void Renderer_Renders_1P_Cloud_As_Extra_Child()
    {
        var service = new QuantumDotService();
        var dot = service.BuildQuantumDot(CdSe, 2.0);
        service.EnsureElectronCloud1P(dot);

        var renderer = new QuantumDotRenderer3D();
        var without1P = renderer.BuildModel(dot, showLattice: false, showCloud: false, showCloud1P: false);
        var with1P = renderer.BuildModel(dot, showLattice: false, showCloud: false, showCloud1P: true);

        Assert.Empty(without1P.Children);
        Assert.Single(with1P.Children); // genau die 1P-Wolke
    }
}