using QuantumDotStudio.Core.Data;
using QuantumDotStudio.Core.Models;

namespace QuantumDotStudio.Solver;

/// <summary>
/// Löst eine Sensor-Vorlage (Data/sensor_templates.json) zu einem vollständigen
/// SensorDesign auf: Materialien, Ligand, Analyt und Anwendungs-Template aus
/// ihren Datenbanken, Emission via voller Brus-Gleichung berechnet (kein
/// gespeicherter Wert — die Engine sieht dasselbe Design wie ein
/// interaktiv erstelltes). Roadmap Phase 4.
/// </summary>
public static class SensorTemplateResolver
{
    /// <summary>Vorlage → SensorDesign; wirft bei unbekannten IDs (Fehler sichtbar machen).</summary>
    public static SensorDesign Resolve(SensorTemplate template)
    {
        ArgumentNullException.ThrowIfNull(template);

        var core = MaterialDatabase.Defaults.FirstOrDefault(m => m.Name == template.CoreMaterial)
            ?? throw new InvalidOperationException($"Material '{template.CoreMaterial}' der Vorlage '{template.TemplateId}' nicht in der Materialdatenbank.");
        var shell = template.ShellMaterial is null ? null
            : MaterialDatabase.Defaults.FirstOrDefault(m => m.Name == template.ShellMaterial)
              ?? throw new InvalidOperationException($"Shell-Material '{template.ShellMaterial}' der Vorlage '{template.TemplateId}' nicht in der Materialdatenbank.");
        var ligand = SensorDatabase.Ligands.FirstOrDefault(l => l.LigandId == template.LigandId)
            ?? throw new InvalidOperationException($"Ligand '{template.LigandId}' der Vorlage '{template.TemplateId}' nicht in der Liganden-Datenbank.");
        var analyte = SensorDatabase.Analytes.FirstOrDefault(a => a.AnalyteId == template.AnalyteId)
            ?? throw new InvalidOperationException($"Analyt '{template.AnalyteId}' der Vorlage '{template.TemplateId}' nicht in der Analyt-Datenbank.");
        var application = TemplateDatabase.Templates.FirstOrDefault(t => t.ApplicationId == template.ApplicationId)
            ?? throw new InvalidOperationException($"Anwendung '{template.ApplicationId}' der Vorlage '{template.TemplateId}' nicht in feasibility_weights.json.");

        return new SensorDesign
        {
            CoreMaterial = core,
            ShellMaterial = shell,
            CoreRadius_nm = template.CoreRadius_nm,
            ShellThickness_nm = template.ShellThickness_nm,
            GradedInterface = template.GradedInterface,
            EmissionWavelength_nm = QuantumSolver.WavelengthFromBandGap(
                QuantumSolver.BrusBandGap(core, template.CoreRadius_nm)),
            Ligand = ligand,
            Analyte = analyte,
            Application = application,
            TargetConcentration_M = template.TargetConcentration_M,
            ReceptorKd_M = template.ReceptorKd_M,
            FretDistanceUnbound_nm = template.FretDistanceUnbound_nm,
            FretDistanceBound_nm = template.FretDistanceBound_nm
        };
    }
}