using System.Text.Json;
using QuantumDotStudio.Core.Models;

namespace QuantumDotStudio.Core.Data;

/// <summary>
/// Lädt und speichert Materialparameter aus JSON.
/// </summary>
public static class MaterialDatabase
{
    /// <summary>
    /// Vordefinierte Halbleitermaterialien für Quantum Dots.
    /// </summary>
    public static List<Material> Defaults => new()
    {
        new Material
        {
            Name = "CdSe",
            BandGap_eV = 1.74,
            EffectiveMassElectron = 0.13,
            EffectiveMassHole = 0.45,
            DielectricConstant = 10.6,
            LatticeConstant_A = 6.05,
            Cation = "Cd",
            Anion = "Se"
        },
        new Material
        {
            Name = "InP",
            BandGap_eV = 1.35,
            EffectiveMassElectron = 0.077,
            EffectiveMassHole = 0.6,
            DielectricConstant = 12.5,
            LatticeConstant_A = 5.86,
            Cation = "In",
            Anion = "P"
        },
        new Material
        {
            Name = "PbS",
            BandGap_eV = 0.41,
            EffectiveMassElectron = 0.105,
            EffectiveMassHole = 0.105,
            DielectricConstant = 17.0,
            LatticeConstant_A = 5.94,
            Cation = "Pb",
            Anion = "S"
        }
    };

    /// <summary>
    /// Lädt Materialien aus einer JSON-Datei.
    /// </summary>
    public static List<Material> LoadFromFile(string path)
    {
        if (!File.Exists(path))
            return new List<Material>();

        string json = File.ReadAllText(path);
        return JsonSerializer.Deserialize<List<Material>>(json, JsonOptions) ?? new List<Material>();
    }

    /// <summary>
    /// Speichert Materialien als JSON-Datei.
    /// </summary>
    public static void SaveToFile(IEnumerable<Material> materials, string path)
    {
        string json = JsonSerializer.Serialize(materials, JsonOptions);
        File.WriteAllText(path, json);
    }

    private static JsonSerializerOptions JsonOptions => new()
    {
        PropertyNameCaseInsensitive = true,
        WriteIndented = true
    };
}
