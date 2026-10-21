using System.Text.Json;

namespace QuantumDotStudio.Core.Data;

/// <summary>
/// Lädt Solventien aus Data/solvents.json (AC-004-Muster: neue Einträge ohne
/// Codeänderung). Fallback-Liste eingebaut, falls keine Datei verfügbar ist —
/// identisch zum MaterialDatabase/SensorDatabase-Muster; `LoadedFrom`
/// meldet, welche Quelle gewonnen hat.
/// Dielektrizitätswerte: CRC-Handbook-Standardwerte bei 20–25 °C (Notes-Feld).
/// </summary>
public static class SolventDatabase
{
    private static readonly string[] Candidates =
    {
        Path.Combine(AppContext.BaseDirectory, "Data", "solvents.json"),
        Path.Combine(AppContext.BaseDirectory, "solvents.json"),
        Path.Combine(Directory.GetCurrentDirectory(), "Data", "solvents.json"),
        "solvents.json"
    };

    private static readonly Lazy<List<Models.Solvent>> _defaults = new(LoadSolvents);

    public static string? LoadedFrom { get; private set; }

    public static List<Models.Solvent> Defaults => _defaults.Value;

    private static List<Models.Solvent> LoadSolvents()
    {
        foreach (string path in Candidates)
        {
            try
            {
                if (!File.Exists(path)) continue;
                var list = JsonSerializer.Deserialize<List<Models.Solvent>>(
                    File.ReadAllText(path), JsonOptions);
                if (list is { Count: > 0 })
                {
                    LoadedFrom = path;
                    return list;
                }
            }
            catch (Exception ex) when (ex is IOException or JsonException or UnauthorizedAccessException)
            {
                // nächsten Kandidaten versuchen
            }
        }
        return FallbackSolvents;
    }

    /// <summary>
    /// Fallback (CRC Handbook, 20–25 °C): Wasser ε_r ≈ 78,4 (25 °C),
    /// Toluol ε_r ≈ 2,38 (25 °C). Muss mit Data/solvents.json synchron
    /// gehalten werden (Tests laufen ohne deployetes Data/-Verzeichnis
    /// gegen diesen Fallback).
    /// </summary>
    private static List<Models.Solvent> FallbackSolvents => new()
    {
        new Models.Solvent { Name = "Wasser", DielectricConstant = 78.4, Notes = "CRC Handbook, 25 °C" },
        new Models.Solvent { Name = "Toluol", DielectricConstant = 2.38, Notes = "CRC Handbook, 25 °C" }
    };

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true,
        ReadCommentHandling = JsonCommentHandling.Skip,
        AllowTrailingCommas = true
    };
}