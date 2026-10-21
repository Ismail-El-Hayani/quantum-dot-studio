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
    /// Elektronenaffinität χ in eV (Abstand Vakuumniveau → Leitungsbandminimum).
    /// Wird für die Bandanpassung von Core/Shell-Heterostrukturen benötigt.
    /// Literaturwerte streuen ±0,1–0,2 eV — sie werden als unsicher betrachtet.
    /// </summary>
    public double ElectronAffinity_eV { get; set; }

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

    /// <summary>
    /// Varshni-Koeffizient α in eV/K: Steigung der Bandlücken-Temperatur-
    /// Verschiebung E_g(T) = E_g(0) − α·T²/(T+β) (Varshni, Physica 34, 149
    /// (1967)). Referenziert auf die 300-K-Lücke aus BandGap_eV. Quelle im
    /// materials.json-Notes; ohne Quellenfund gilt der II-VI-Standardwert.
    /// </summary>
    public double VarshniAlpha_eV_per_K { get; set; }

    /// <summary>
    /// Varshni-Temperatur β in K (nahe der Debye-Temperatur, aber anpassbar).
    /// </summary>
    public double VarshniBeta_K { get; set; }
}
