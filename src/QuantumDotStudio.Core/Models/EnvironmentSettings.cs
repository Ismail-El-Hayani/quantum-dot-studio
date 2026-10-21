namespace QuantumDotStudio.Core.Models;

/// <summary>
/// Umgebungsparameter der Simulation (Roadmap 2.3): Solvens (dielektrische
/// Abschirmung), Temperatur (Varshni-Bandlückenverschiebung) und pH-Wert
/// (Liganden-Stabilitätsfenster). Als record mit Wertgleichheit, damit die
/// Cache-Schlüssel des Services korrekt unterscheiden.
/// Standard (null/300 K/kein Solvens) liefert bit-identisch die bisherigen
/// Zahlen: die Varshni-Verschiebung ist bei Referenztemperatur exakt null.
/// </summary>
public sealed record EnvironmentSettings(
    Solvent? Solvent = null,
    double Temperature_K = 300.0, /* EnvironmentModel.ReferenceTemperature_K */
    double? Ph = null);