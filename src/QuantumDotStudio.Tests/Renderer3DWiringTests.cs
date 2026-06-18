using QuantumDotStudio.Core.Data;
using QuantumDotStudio.Core.Models;
using QuantumDotStudio.Renderer;
using QuantumDotStudio.Solver;
using Xunit;

namespace QuantumDotStudio.Tests;

/// <summary>
/// Tests für die Verdrahtung zwischen Simulationsparametern und 3D-Rendering.
/// </summary>
public class Renderer3DWiringTests
{
    [Fact]
    public void BuildModel_LargerRadius_YieldsLargerBounds()
    {
        var renderer = new QuantumDotRenderer3D();
        var service = new QuantumDotService();
        var cdSe = MaterialDatabase.Defaults.First(m => m.Name == "CdSe");

        var dotSmall = service.BuildQuantumDot(cdSe, 2.0);
        var dotLarge = service.BuildQuantumDot(cdSe, 8.0);

        var boundsSmall = renderer.GetBounds(dotSmall);
        var boundsLarge = renderer.GetBounds(dotLarge);

        Assert.True(boundsLarge.SizeX > boundsSmall.SizeX);
        Assert.True(boundsLarge.SizeY > boundsSmall.SizeY);
        Assert.True(boundsLarge.SizeZ > boundsSmall.SizeZ);
    }

    [Fact]
    public void BuildModel_DifferentMaterials_ProduceDifferentAtomElements()
    {
        var service = new QuantumDotService();
        var cdSe = MaterialDatabase.Defaults.First(m => m.Name == "CdSe");
        var inP = MaterialDatabase.Defaults.First(m => m.Name == "InP");

        var dotCdSe = service.BuildQuantumDot(cdSe, 3.0);
        var dotInP = service.BuildQuantumDot(inP, 3.0);

        var elementsCdSe = dotCdSe.Atoms.Select(a => a.Element).Distinct().OrderBy(e => e).ToList();
        var elementsInP = dotInP.Atoms.Select(a => a.Element).Distinct().OrderBy(e => e).ToList();

        Assert.Equal(new[] { "Cd", "Se" }, elementsCdSe);
        Assert.Equal(new[] { "In", "P" }, elementsInP);
    }

    [Fact]
    public void BuildModel_MaterialSwitch_ChangesAtomColors()
    {
        var renderer = new QuantumDotRenderer3D();
        var service = new QuantumDotService();
        var cdSe = MaterialDatabase.Defaults.First(m => m.Name == "CdSe");
        var inP = MaterialDatabase.Defaults.First(m => m.Name == "InP");

        var dotCdSe = service.BuildQuantumDot(cdSe, 3.0);
        var dotInP = service.BuildQuantumDot(inP, 3.0);

        var modelCdSe = renderer.BuildModel(dotCdSe, showLattice: true, showCloud: false);
        var modelInP = renderer.BuildModel(dotInP, showLattice: true, showCloud: false);

        var colorsCdSe = ExtractDiffuseColors(modelCdSe).Distinct().ToList();
        var colorsInP = ExtractDiffuseColors(modelInP).Distinct().ToList();

        Assert.NotEmpty(colorsCdSe);
        Assert.NotEmpty(colorsInP);
        Assert.NotEqual(colorsCdSe, colorsInP);
    }

    [Fact]
    public void BuildModel_ToggleCloud_HidesCloudGeometry()
    {
        var renderer = new QuantumDotRenderer3D();
        var service = new QuantumDotService();
        var cdSe = MaterialDatabase.Defaults.First(m => m.Name == "CdSe");
        var dot = service.BuildQuantumDot(cdSe, 3.0);

        var withCloud = renderer.BuildModel(dot, showLattice: false, showCloud: true);
        var withoutCloud = renderer.BuildModel(dot, showLattice: false, showCloud: false);

        Assert.NotEmpty(withCloud.Children);
        Assert.Empty(withoutCloud.Children);
    }

    [Fact]
    public void BuildModel_ToggleLattice_HidesAtomGeometry()
    {
        var renderer = new QuantumDotRenderer3D();
        var service = new QuantumDotService();
        var cdSe = MaterialDatabase.Defaults.First(m => m.Name == "CdSe");
        var dot = service.BuildQuantumDot(cdSe, 3.0);

        var withLattice = renderer.BuildModel(dot, showLattice: true, showCloud: false);
        var withoutLattice = renderer.BuildModel(dot, showLattice: false, showCloud: false);

        Assert.NotEmpty(withLattice.Children);
        Assert.Empty(withoutLattice.Children);
    }

    private static IEnumerable<System.Windows.Media.Color> ExtractDiffuseColors(System.Windows.Media.Media3D.Model3DGroup group)
    {
        foreach (var child in group.Children)
        {
            if (child is System.Windows.Media.Media3D.GeometryModel3D geometryModel)
            {
                foreach (var color in ExtractBrushColors(geometryModel.Material))
                    yield return color;
            }
        }
    }

    private static IEnumerable<System.Windows.Media.Color> ExtractBrushColors(System.Windows.Media.Media3D.Material? material)
    {
        if (material is System.Windows.Media.Media3D.DiffuseMaterial diffuse && diffuse.Brush is System.Windows.Media.SolidColorBrush brush)
        {
            yield return brush.Color;
        }
        else if (material is System.Windows.Media.Media3D.MaterialGroup group)
        {
            foreach (var child in group.Children)
            {
                foreach (var color in ExtractBrushColors(child))
                    yield return color;
            }
        }
    }
}
