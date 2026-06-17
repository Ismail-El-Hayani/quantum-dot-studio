using QuantumDotStudio.Core.Models;

namespace QuantumDotStudio.Core.Models;

/// <summary>
/// Aggregiert Material, Radius und berechnete Eigenschaften eines Quantum Dots.
/// </summary>
public class QuantumDot
{
    /// <summary>
    /// Gewähltes Halbleitermaterial.
    /// </summary>
    public Material Material { get; set; } = new();

    /// <summary>
    /// Nanokristall-Radius in nm.
    /// </summary>
    public double Radius_nm { get; set; }

    /// <summary>
    /// Confinement-Energie des Elektrons in eV.
    /// </summary>
    public double ConfinementEnergyElectron_eV { get; set; }

    /// <summary>
    /// Confinement-Energie des Lochs in eV.
    /// </summary>
    public double ConfinementEnergyHole_eV { get; set; }

    /// <summary>
    /// Effektive Gesamtbandlücke des Quantum Dots in eV.
    /// </summary>
    public double TotalBandGap_eV { get; set; }

    /// <summary>
    /// Emissionswellenlänge in nm.
    /// </summary>
    public double EmissionWavelength_nm { get; set; }

    /// <summary>
    /// Berechnete Energieniveaus.
    /// </summary>
    public List<EnergyLevel> EnergyLevels { get; set; } = new();

    /// <summary>
    /// Elektronen-Wahrscheinlichkeitsdichte-Wolke (Position in nm, normierte |ψ|²).
    /// </summary>
    public List<(System.Numerics.Vector3 Position, float Probability)> ElectronCloud { get; set; } = new();

    /// <summary>
    /// Atome des Zinkblende-Gitterausschnitts.
    /// </summary>
    public List<Atom> Atoms { get; set; } = new();
}
