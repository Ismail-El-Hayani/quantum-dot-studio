using System.Text.Json;
using QuantumDotStudio.Core.Models;

namespace QuantumDotStudio.Core.Data;

/// <summary>
/// Laedt die Anwendungs-Templates (Emissionsfenster, Medium, Faktor-Gewichte)
/// aus Data/feasibility_weights.json — benutzer-tunbar ohne Codeaenderung
/// (Roadmap 3.1, AC-004-Muster). Eingebaute Fallback-Liste, falls keine
/// Datei verfuegbar ist.
/// </summary>
public static class TemplateDatabase
{
    private static readonly string[] Candidates =
    {
        Path.Combine(AppContext.BaseDirectory, "Data", "feasibility_weights.json"),
        Path.Combine(AppContext.BaseDirectory, "feasibility_weights.json"),
        Path.Combine(Directory.GetCurrentDirectory(), "Data", "feasibility_weights.json"),
        "feasibility_weights.json"
    };

    private static readonly Lazy<List<ApplicationTemplate>> _templates = new(LoadTemplates);

    public static string? LoadedFrom { get; private set; }

    public static List<ApplicationTemplate> Templates => _templates.Value;

    private static List<ApplicationTemplate> LoadTemplates()
    {
        foreach (string path in Candidates)
        {
            try
            {
                if (!File.Exists(path)) continue;
                var list = JsonSerializer.Deserialize<List<ApplicationTemplate>>(File.ReadAllText(path), JsonOptions);
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
        return FallbackTemplates;
    }

    /// <summary>Eingebaute Fallbacks — inhaltsgleich mit der JSON-Datei.</summary>
    private static List<ApplicationTemplate> FallbackTemplates => new()
    {
        new ApplicationTemplate
        {
            ApplicationId = "bio-imaging-nir", DisplayName = "Bio-Imaging (NIR-I, 650–900 nm)",
            EmissionWindowMin_nm = 650, EmissionWindowMax_nm = 900, Medium = "aqueous"
        },
        new ApplicationTemplate
        {
            ApplicationId = "visible-imaging", DisplayName = "Sichtbare Fluoreszenz (500–650 nm)",
            EmissionWindowMin_nm = 500, EmissionWindowMax_nm = 650, Medium = "aqueous"
        },
        new ApplicationTemplate
        {
            ApplicationId = "heavy-metal-sensing", DisplayName = "Schwermetall-Sensor (Pb²⁺/Hg²⁺, sichtbar)",
            EmissionWindowMin_nm = 480, EmissionWindowMax_nm = 680, Medium = "aqueous",
            WeightEmissionWindow = 0.15, WeightStrain = 0.15, WeightQuantumYield = 0.15,
            WeightTransduction = 0.35, WeightStability = 0.15, WeightBioconjugation = 0.05
        },
        new ApplicationTemplate
        {
            ApplicationId = "pesticide-sensing", DisplayName = "Pestizid-Aptasensor (sichtbar, ratiometrisch)",
            EmissionWindowMin_nm = 550, EmissionWindowMax_nm = 750, Medium = "aqueous",
            WeightEmissionWindow = 0.20, WeightStrain = 0.10, WeightQuantumYield = 0.15,
            WeightTransduction = 0.30, WeightStability = 0.15, WeightBioconjugation = 0.10
        },
        new ApplicationTemplate
        {
            ApplicationId = "metabolite-sensing", DisplayName = "Metabolit-Sensor (Glucose, klinisch)",
            EmissionWindowMin_nm = 500, EmissionWindowMax_nm = 700, Medium = "aqueous",
            WeightEmissionWindow = 0.15, WeightStrain = 0.10, WeightQuantumYield = 0.20,
            WeightTransduction = 0.30, WeightStability = 0.20, WeightBioconjugation = 0.05
        }
    };

    private static JsonSerializerOptions JsonOptions => new()
    {
        PropertyNameCaseInsensitive = true,
        ReadCommentHandling = JsonCommentHandling.Skip
    };
}