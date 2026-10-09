using System.Text.Json;

namespace QuantumDotStudio.Core.Data;

/// <summary>
/// Laedt die vorparameterisierten Sensor-Vorlagen aus Data/sensor_templates.json
/// (Roadmap Phase 4, AC-004-Muster). Das Aufloesen der IDs gegen Material-,
/// Ligand-, Analyt- und Anwendungs-Datenbanken geschieht in
/// QuantumDotStudio.Solver.SensorTemplateResolver (Core kennt den Solver nicht).
/// </summary>
public static class SensorTemplateDatabase
{
    private static readonly string[] Candidates =
    {
        Path.Combine(AppContext.BaseDirectory, "Data", "sensor_templates.json"),
        Path.Combine(AppContext.BaseDirectory, "sensor_templates.json"),
        Path.Combine(Directory.GetCurrentDirectory(), "Data", "sensor_templates.json"),
        "sensor_templates.json"
    };

    private static readonly Lazy<List<Models.SensorTemplate>> _templates = new(LoadTemplates);

    public static string? LoadedFrom { get; private set; }

    public static List<Models.SensorTemplate> Templates => _templates.Value;

    private static List<Models.SensorTemplate> LoadTemplates()
    {
        foreach (string path in Candidates)
        {
            try
            {
                if (!File.Exists(path)) continue;
                var list = JsonSerializer.Deserialize<List<Models.SensorTemplate>>(File.ReadAllText(path), JsonOptions);
                if (list is { Count: > 0 })
                {
                    LoadedFrom = path;
                    return list;
                }
            }
            catch (Exception ex) when (ex is IOException or JsonException or UnauthorizedAccessException)
            {
                // naechsten Kandidaten versuchen
            }
        }
        return new List<Models.SensorTemplate>(); // keine Fallback-Vorlagen: Literatur-Referenzen sind Pflicht
    }

    private static JsonSerializerOptions JsonOptions => new()
    {
        PropertyNameCaseInsensitive = true,
        ReadCommentHandling = JsonCommentHandling.Skip
    };
}