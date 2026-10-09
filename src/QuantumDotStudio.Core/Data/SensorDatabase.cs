using System.Text.Json;
using QuantumDotStudio.Core.Models;

namespace QuantumDotStudio.Core.Data;

/// <summary>
/// Laedt Liganden und Analyten aus JSON (AC-004-Muster: neue Eintraege ohne
/// Codeaenderung, nur Data/ligands.json bzw. Data/analytes.json erweitern).
/// Fallback-Listen sind eingebaut, falls keine Datei verfuegbar ist.
/// </summary>
public static class SensorDatabase
{
    private static readonly string[] LigandCandidates =
    {
        Path.Combine(AppContext.BaseDirectory, "Data", "ligands.json"),
        Path.Combine(AppContext.BaseDirectory, "ligands.json"),
        Path.Combine(Directory.GetCurrentDirectory(), "Data", "ligands.json"),
        "ligands.json"
    };

    private static readonly string[] AnalyteCandidates =
    {
        Path.Combine(AppContext.BaseDirectory, "Data", "analytes.json"),
        Path.Combine(AppContext.BaseDirectory, "analytes.json"),
        Path.Combine(Directory.GetCurrentDirectory(), "Data", "analytes.json"),
        "analytes.json"
    };

    private static readonly Lazy<List<Ligand>> _ligands = new(LoadLigands);
    private static readonly Lazy<List<Analyte>> _analytes = new(LoadAnalytes);

    public static string? LigandsLoadedFrom { get; private set; }
    public static string? AnalytesLoadedFrom { get; private set; }

    public static List<Ligand> Ligands => _ligands.Value;
    public static List<Analyte> Analytes => _analytes.Value;

    private static List<Ligand> LoadLigands()
    {
        foreach (string path in LigandCandidates)
        {
            try
            {
                if (!File.Exists(path)) continue;
                var list = JsonSerializer.Deserialize<List<Ligand>>(File.ReadAllText(path), JsonOptions);
                if (list is { Count: > 0 })
                {
                    LigandsLoadedFrom = path;
                    return list;
                }
            }
            catch (Exception ex) when (ex is IOException or JsonException or UnauthorizedAccessException)
            {
                // naechsten Kandidaten versuchen
            }
        }
        return FallbackLigands;
    }

    private static List<Analyte> LoadAnalytes()
    {
        foreach (string path in AnalyteCandidates)
        {
            try
            {
                if (!File.Exists(path)) continue;
                var list = JsonSerializer.Deserialize<List<Analyte>>(File.ReadAllText(path), JsonOptions);
                if (list is { Count: > 0 })
                {
                    AnalytesLoadedFrom = path;
                    return list;
                }
            }
            catch (Exception ex) when (ex is IOException or JsonException or UnauthorizedAccessException)
            {
                // naechsten Kandidaten versuchen
            }
        }
        return FallbackAnalytes;
    }

    /// <summary>Eingebaute Fallbacks — inhaltsgleich mit den JSON-Dateien.</summary>
    private static List<Ligand> FallbackLigands => new()
    {
        new Ligand { LigandId = "MPA", DisplayName = "Mercaptopropionic acid", AnchorAtom = "S", Length_nm = 0.6, Charge_e = -1, Targets = new() { "Cd", "Zn", "Pb" }, Solubility = "aqueous" },
        new Ligand { LigandId = "TGA", DisplayName = "Thioglycolic acid", AnchorAtom = "S", Length_nm = 0.5, Charge_e = -1, Targets = new() { "Cd", "Zn", "Pb" }, Solubility = "aqueous" },
        new Ligand { LigandId = "TOPO", DisplayName = "Trioctylphosphine oxide", AnchorAtom = "P", Length_nm = 1.1, Charge_e = 0, Targets = new() { "Cd", "In", "Pb" }, Solubility = "organic" },
        new Ligand { LigandId = "PEG-5000", DisplayName = "Polyethylene glycol 5000", AnchorAtom = "O", Length_nm = 5.0, Charge_e = 0, Targets = new() { "Zn", "Cd" }, Solubility = "aqueous" }
    };

    private static List<Analyte> FallbackAnalytes => new()
    {
        new Analyte { AnalyteId = "Fluorescein", DisplayName = "Fluorescein (FRET-Akzeptor)", Mode = "FRET", ForsterRadius_nm = 5.0, Notes = "R0 ~ 5 nm (Lakowicz)" },
        new Analyte { AnalyteId = "Rhodamine6G", DisplayName = "Rhodamine 6G (FRET-Akzeptor)", Mode = "FRET", ForsterRadius_nm = 5.5, Notes = "typischer Paar-Abstand" },
        new Analyte { AnalyteId = "Pb2+", DisplayName = "Pb(II)-Ion", Mode = "Quenching", SternVolmerConstant_M = 4.2e5, Notes = "K_SV ~ 4e5 M^-1 (CdSe/ZnS-MPA)" },
        new Analyte { AnalyteId = "Hg2+", DisplayName = "Hg(II)-Ion", Mode = "Quenching", SternVolmerConstant_M = 1.0e6, Notes = "Hg quencht staerker als Pb" },
        new Analyte { AnalyteId = "H+", DisplayName = "Proton (pH)", Mode = "Charge", NernstSlope_mV_per_decade = 59.16, Notes = "59.16 mV/Dekade bei 25 C" },
        new Analyte { AnalyteId = "Dopamine", DisplayName = "Dopamin (PET-Quencher)", Mode = "PET", RedoxPotential_V = 0.21, Notes = "E0 ~ +0.21 V vs. NHE (pH 7)" },
        new Analyte { AnalyteId = "Ascorbate", DisplayName = "Ascorbat / Vitamin C (PET-Quencher)", Mode = "PET", RedoxPotential_V = 0.35, Notes = "E0 ~ +0.35 V vs. NHE (pH 7)" }
    };

    private static JsonSerializerOptions JsonOptions => new()
    {
        PropertyNameCaseInsensitive = true,
        ReadCommentHandling = JsonCommentHandling.Skip
    };
}