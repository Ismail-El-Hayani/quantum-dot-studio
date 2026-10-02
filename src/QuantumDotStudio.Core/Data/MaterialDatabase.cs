using System.Text.Json;
using QuantumDotStudio.Core.Models;

namespace QuantumDotStudio.Core.Data;

/// <summary>
/// Lädt und speichert Materialparameter aus JSON.
/// Die Standardmaterialien werden beim ersten Zugriff aus der ausgelieferten
/// Datei <c>Data/materials.json</c> geladen; neue Materialien können dort
/// ohne Code-Änderung ergänzt werden (NFR-003). Ist die Datei nicht
/// verfügbar oder ungültig, greift die eingebaute Fallback-Liste.
/// </summary>
public static class MaterialDatabase
{
    /// <summary>
    /// Kandidatenpfade für die Standard-Konfigurationsdatei, in Reihenfolge der Prüfung.
    /// </summary>
    private static readonly string[] JsonCandidates =
    {
        Path.Combine(AppContext.BaseDirectory, "Data", "materials.json"),
        Path.Combine(AppContext.BaseDirectory, "materials.json"),
        Path.Combine(Directory.GetCurrentDirectory(), "Data", "materials.json"),
        "materials.json"
    };

    private static readonly Lazy<List<Material>> _defaults = new(LoadDefaults);

    /// <summary>
    /// Pfad der tatsächlich geladenen JSON-Datei, oder null, wenn der
    /// eingebaute Fallback aktiv ist. Diagnosehilfe und Testanker.
    /// </summary>
    public static string? LoadedFrom { get; private set; }

    /// <summary>
    /// Verfügbare Halbleitermaterialien. Wird beim ersten Zugriff einmalig
    /// aus <c>Data/materials.json</c> geladen (thread-sicher via Lazy).
    /// </summary>
    public static List<Material> Defaults => _defaults.Value;

    private static List<Material> LoadDefaults()
    {
        foreach (string path in JsonCandidates)
        {
            try
            {
                if (!File.Exists(path))
                    continue;

                List<Material> materials = LoadFromFile(path);
                if (materials.Count > 0)
                {
                    LoadedFrom = path;
                    return materials;
                }
            }
            catch (Exception ex) when (ex is IOException or JsonException or UnauthorizedAccessException)
            {
                // Ungültige oder gesperrte Datei: nächsten Kandidaten versuchen,
                // am Ende den eingebauten Fallback verwenden.
            }
        }

        return FallbackDefaults;
    }

    /// <summary>
    /// Eingebaute Fallback-Materialien für den Fall, dass keine gültige
    /// JSON-Datei gefunden wird. Muss mit <c>Data/materials.json</c>
    /// inhaltlich übereinstimmen.
    /// </summary>
    private static List<Material> FallbackDefaults => new()
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
    /// Existiert die Datei nicht, wird eine leere Liste zurückgegeben;
    /// ungültiges JSON löst eine Ausnahme aus.
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