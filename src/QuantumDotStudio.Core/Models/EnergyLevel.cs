namespace QuantumDotStudio.Core.Models;

/// <summary>
/// Ein einzelnes berechnetes Energieniveau im sphärischen Potentialtopf.
/// </summary>
public class EnergyLevel
{
    /// <summary>
    /// Radiale Quantenzahl n (n = 1, 2, ...).
    /// </summary>
    public int PrincipalQuantumNumber_n { get; set; }

    /// <summary>
    /// Drehimpulsquantenzahl l (l = 0, 1, 2, ...).
    /// </summary>
    public int AngularMomentum_l { get; set; }

    /// <summary>
    /// Energie des Niveaus in eV.
    /// </summary>
    public double Energy_eV { get; set; }

    /// <summary>
    /// Bezeichnung wie 1S, 1P, 2S, ... (ohne Teilchen-Präfix).
    /// </summary>
    public string Label { get; set; } = string.Empty;

    /// <summary>
    /// Teilchentyp (Elektron oder Loch), zu dem dieses Niveau gehört.
    /// </summary>
    public Particle Particle { get; set; } = Particle.Electron;
}
