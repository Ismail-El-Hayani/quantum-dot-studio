namespace QuantumDotStudio.Core.Models;

/// <summary>
/// Physikalische Parameter eines Halbleitermaterials für Quantum-Dot-Berechnungen.
/// </summary>
public class Material
{
    /// <summary>
    /// Name des Materials, z. B. CdSe.
    /// </summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// Bandlücke des Bulk-Materials in eV.
    /// </summary>
    public double BandGap_eV { get; set; }

    /// <summary>
    /// Effektive Elektronenmasse in Einheiten der Elektronenmasse m_e.
    /// </summary>
    public double EffectiveMassElectron { get; set; }

    /// <summary>
    /// Effektive Löchermasse in Einheiten der Elektronenmasse m_e.
    /// </summary>
    public double EffectiveMassHole { get; set; }

    /// <summary>
    /// Statische Dielektrizitätskonstante ε_r.
    /// </summary>
    public double DielectricConstant { get; set; }

    /// <summary>
    /// Gitterkonstante in Ångström.
    /// </summary>
    public double LatticeConstant_A { get; set; }

    /// <summary>
    /// Kation-Symbol für die Zinkblende-Struktur (z. B. Cd).
    /// </summary>
    public string Cation { get; set; } = string.Empty;

    /// <summary>
    /// Anion-Symbol für die Zinkblende-Struktur (z. B. Se).
    /// </summary>
    public string Anion { get; set; } = string.Empty;
}
