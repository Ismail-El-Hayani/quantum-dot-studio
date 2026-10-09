using QuantumDotStudio.Core.Models;

namespace QuantumDotStudio.Core.Models;

/// <summary>
/// Vorparameterisierter Sensor-Entwurf (Roadmap Phase 4): komplettes Design
/// aus Data/sensor_templates.json, aufgeloest gegen Material-/Ligand-/
/// Analyt-/Anwendungs-Datenbanken. Jede Vorlage traegt ihre Literaturquelle.
/// </summary>
public class SensorTemplate
{
    public string TemplateId { get; set; } = "";
    public string DisplayName { get; set; } = "";
    public string CoreMaterial { get; set; } = "";
    public double CoreRadius_nm { get; set; }
    public string? ShellMaterial { get; set; }
    public double ShellThickness_nm { get; set; }
    /// <summary>Graduierte Grenzflaeche (kommerzielle CdSe/ZnS nutzen legierte Zwischenschichten).</summary>
    public bool GradedInterface { get; set; }
    public string LigandId { get; set; } = "";
    public string AnalyteId { get; set; } = "";
    public string ApplicationId { get; set; } = "";
    public double TargetConcentration_M { get; set; }
    public double? ReceptorKd_M { get; set; }
    public double FretDistanceUnbound_nm { get; set; }
    public double FretDistanceBound_nm { get; set; }
    /// <summary>Literaturquelle der Vorlagen-Parameter (Roadmap-Regel 3).</summary>
    public string Reference { get; set; } = "";
}