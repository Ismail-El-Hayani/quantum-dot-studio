using QuantumDotStudio.Core.Data;
using QuantumDotStudio.Core.Models;

namespace QuantumDotStudio.Tests;

/// <summary>
/// Tests für die JSON-basierte Materialdatenbank (NFR-003, AC-004):
/// Neue Materialien müssen durch Ergänzen der Konfigurationsdatei
/// hinzufügbar sein, ohne Core-Code zu ändern.
/// </summary>
public class MaterialDatabaseTests
{
    [Fact]
    public void Defaults_Are_Loaded_From_Deployed_Json()
    {
        // Beweist, dass materials.json ins Ausgabeverzeichnis kopiert wird
        // und tatsächlich als Quelle dient (nicht der eingebaute Fallback).
        Assert.NotNull(MaterialDatabase.LoadedFrom);
        Assert.True(File.Exists(MaterialDatabase.LoadedFrom),
            $"materials.json wurde nicht ins Ausgabeverzeichnis kopiert: {MaterialDatabase.LoadedFrom}");
    }

    [Fact]
    public void Defaults_Contain_Standard_Materials()
    {
        var names = MaterialDatabase.Defaults.Select(m => m.Name).ToList();

        Assert.Contains("CdSe", names);
        Assert.Contains("InP", names);
        Assert.Contains("PbS", names);
    }

    [Fact]
    public void Defaults_CdSe_Has_Expected_Parameters()
    {
        var cdSe = MaterialDatabase.Defaults.Single(m => m.Name == "CdSe");

        Assert.Equal(1.74, cdSe.BandGap_eV, tolerance: 1e-9);
        Assert.Equal(0.13, cdSe.EffectiveMassElectron, tolerance: 1e-9);
        Assert.Equal(0.45, cdSe.EffectiveMassHole, tolerance: 1e-9);
        Assert.Equal(10.6, cdSe.DielectricConstant, tolerance: 1e-9);
        Assert.Equal(6.05, cdSe.LatticeConstant_A, tolerance: 1e-9);
    }

    [Fact]
    public void Custom_Material_Can_Be_Added_Via_Json_Config()
    {
        // AC-004: Ein neues Material (ZnS) nur durch Konfiguration hinzufügen.
        string tempJson = Path.Combine(Path.GetTempPath(), $"qds_materials_{Guid.NewGuid():N}.json");
        try
        {
            File.WriteAllText(tempJson, """
                [
                  {
                    "Name": "ZnS",
                    "BandGap_eV": 3.68,
                    "EffectiveMassElectron": 0.25,
                    "EffectiveMassHole": 0.59,
                    "DielectricConstant": 8.9,
                    "LatticeConstant_A": 5.41,
                    "Cation": "Zn",
                    "Anion": "S"
                  }
                ]
                """);

            var materials = MaterialDatabase.LoadFromFile(tempJson);

            var znS = Assert.Single(materials);
            Assert.Equal("ZnS", znS.Name);
            Assert.Equal(3.68, znS.BandGap_eV, tolerance: 1e-9);
            Assert.Equal("Zn", znS.Cation);
            Assert.Equal("S", znS.Anion);

            // Und das neue Material ist ohne Core-Code-Änderung voll rechenfähig.
            var solverDot = new QuantumDotStudio.Solver.QuantumDotService()
                .BuildQuantumDot(znS, 3.0);
            Assert.True(solverDot.TotalBandGap_eV > znS.BandGap_eV);
        }
        finally
        {
            File.Delete(tempJson);
        }
    }

    [Fact]
    public void LoadFromFile_Missing_File_Returns_Empty_List()
    {
        var materials = MaterialDatabase.LoadFromFile(
            Path.Combine(Path.GetTempPath(), $"does_not_exist_{Guid.NewGuid():N}.json"));

        Assert.Empty(materials);
    }

    [Fact]
    public void SaveToFile_LoadFromFile_Roundtrip_Preserves_Material()
    {
        var original = new List<Material>
        {
            new()
            {
                Name = "TestMaterial",
                BandGap_eV = 2.0,
                EffectiveMassElectron = 0.1,
                EffectiveMassHole = 0.4,
                DielectricConstant = 9.5,
                LatticeConstant_A = 5.5,
                Cation = "Xx",
                Anion = "Yy"
            }
        };

        string tempJson = Path.Combine(Path.GetTempPath(), $"qds_roundtrip_{Guid.NewGuid():N}.json");
        try
        {
            MaterialDatabase.SaveToFile(original, tempJson);
            var loaded = MaterialDatabase.LoadFromFile(tempJson);

            var material = Assert.Single(loaded);
            Assert.Equal("TestMaterial", material.Name);
            Assert.Equal(2.0, material.BandGap_eV, tolerance: 1e-9);
            Assert.Equal(0.1, material.EffectiveMassElectron, tolerance: 1e-9);
            Assert.Equal(9.5, material.DielectricConstant, tolerance: 1e-9);
        }
        finally
        {
            File.Delete(tempJson);
        }
    }
}