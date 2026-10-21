namespace QuantumDotStudio.Core.Models;

/// <summary>
/// Umgebungs-Solvens: dielektrische Abschirmung des Coulomb-Terms.
/// Werte aus CRC-Handbook-Standardtabellen (20–25 °C), Quelle im Notes-Feld.
/// </summary>
public class Solvent
{
    /// <summary>Anzeigename, z. B. „Wasser“.</summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>Statische relative Permittivität ε_r des Solvens.</summary>
    public double DielectricConstant { get; set; }

    /// <summary>Frei:notiz (Quelle der Dielektrizitätskonstante).</summary>
    public string Notes { get; set; } = string.Empty;
}