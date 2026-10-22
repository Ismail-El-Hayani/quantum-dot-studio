using System.Reflection;
using QuantumDotStudio.Core.Data;
using QuantumDotStudio.Core.Models;

namespace QuantumDotStudio.Tests;

/// <summary>
/// Mirror-Tests für die eingebauten Fallback-Listen: JEDER JSON-Eintrag muss
/// auch im eingebauten Fallback stehen und dieselben Werte tragen. Tests, die
/// ohne deploytes Data/-Verzeichnis laufen, bewerten sonst still den
/// Fallback — ein fehlender Eintrag (z. B. GOx, ProteinG, Acetamiprid,
/// Glucose, IgG) scheitert dann erst zur Laufzeit im Template-Resolver.
/// </summary>
public class SensorDatabaseMirrorTests
{
    private static List<Ligand> FallbackLigands()
    {
        var prop = typeof(SensorDatabase).GetProperty(
            "FallbackLigands", BindingFlags.NonPublic | BindingFlags.Static);
        Assert.NotNull(prop);
        return (List<Ligand>)prop.GetValue(null)!;
    }

    private static List<Analyte> FallbackAnalytes()
    {
        var prop = typeof(SensorDatabase).GetProperty(
            "FallbackAnalytes", BindingFlags.NonPublic | BindingFlags.Static);
        Assert.NotNull(prop);
        return (List<Analyte>)prop.GetValue(null)!;
    }

    [Fact]
    public void Fallback_Contains_Exactly_The_Json_Ligands()
    {
        var json = SensorDatabase.Ligands;
        var fallback = FallbackLigands();

        var jsonIds = json.Select(l => l.LigandId).OrderBy(id => id).ToArray();
        var fallbackIds = fallback.Select(l => l.LigandId).OrderBy(id => id).ToArray();

        Assert.Equal(jsonIds, fallbackIds);
    }

    [Fact]
    public void Fallback_Ligand_Values_Match_The_Json()
    {
        var fallback = FallbackLigands().ToDictionary(l => l.LigandId);

        foreach (var ligand in SensorDatabase.Ligands)
        {
            Assert.True(fallback.ContainsKey(ligand.LigandId),
                $"Ligand {ligand.LigandId} fehlt im eingebauten Fallback");

            var fb = fallback[ligand.LigandId];
            Assert.Equal(ligand.DisplayName, fb.DisplayName);
            Assert.Equal(ligand.AnchorAtom, fb.AnchorAtom);
            Assert.Equal(ligand.Length_nm, fb.Length_nm, tolerance: 1e-9);
            Assert.Equal(ligand.Charge_e, fb.Charge_e);
            Assert.Equal(ligand.Solubility, fb.Solubility);
            Assert.Equal(ligand.StabilityPhMin, fb.StabilityPhMin);
            Assert.Equal(ligand.StabilityPhMax, fb.StabilityPhMax);
        }
    }

    [Fact]
    public void Fallback_Contains_Exactly_The_Json_Analytes()
    {
        var json = SensorDatabase.Analytes;
        var fallback = FallbackAnalytes();

        var jsonIds = json.Select(a => a.AnalyteId).OrderBy(id => id).ToArray();
        var fallbackIds = fallback.Select(a => a.AnalyteId).OrderBy(id => id).ToArray();

        Assert.Equal(jsonIds, fallbackIds);
    }

    [Fact]
    public void Fallback_Analyte_Physics_Matches_The_Json()
    {
        var fallback = FallbackAnalytes().ToDictionary(a => a.AnalyteId);

        foreach (var analyte in SensorDatabase.Analytes)
        {
            Assert.True(fallback.ContainsKey(analyte.AnalyteId),
                $"Analyt {analyte.AnalyteId} fehlt im eingebauten Fallback");

            var fb = fallback[analyte.AnalyteId];
            Assert.Equal(analyte.Mode, fb.Mode);
            Assert.Equal(analyte.ForsterRadius_nm, fb.ForsterRadius_nm, tolerance: 1e-9);
            Assert.Equal(analyte.SternVolmerConstant_M, fb.SternVolmerConstant_M, tolerance: 1e-9);
            Assert.Equal(analyte.NernstSlope_mV_per_decade, fb.NernstSlope_mV_per_decade, tolerance: 1e-9);
            Assert.Equal(analyte.RedoxPotential_V, fb.RedoxPotential_V, tolerance: 1e-9);
        }
    }
}