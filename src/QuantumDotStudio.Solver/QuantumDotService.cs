using QuantumDotStudio.Core.Data;
using QuantumDotStudio.Core.Models;

namespace QuantumDotStudio.Solver;

/// <summary>
/// Verbindet Materialauswahl, Solver und Gittergenerierung zu einem berechneten QuantumDot-Objekt.
/// </summary>
public class QuantumDotService
{
    private readonly LatticeEngine _lattice;

    public QuantumDotService()
    {
        _lattice = new LatticeEngine();
    }

    /// <summary>
    /// Erzeugt ein vollständig berechnetes Quantum Dot für das gewählte Material und den Radius.
    /// </summary>
    public QuantumDot BuildQuantumDot(Material material, double radius_nm, int maxLevels = 6)
    {
        var dot = new QuantumDot
        {
            Material = material,
            Radius_nm = radius_nm
        };

        // Elektron-Confinement (n=1, l=0)
        dot.ConfinementEnergyElectron_eV = QuantumSolver.ConfinementEnergy(radius_nm, material.EffectiveMassElectron, 1, 0);

        // Loch-Confinement (n=1, l=0)
        dot.ConfinementEnergyHole_eV = QuantumSolver.ConfinementEnergy(radius_nm, material.EffectiveMassHole, 1, 0);

        // Effektive Bandlücke
        dot.TotalBandGap_eV = QuantumSolver.BrusBandGap(material, radius_nm);

        // Emissionswellenlänge
        dot.EmissionWavelength_nm = QuantumSolver.WavelengthFromBandGap(dot.TotalBandGap_eV);

        // Energieniveaus bis maxLevels (pro Teilchentyp)
        var electronLevels = QuantumSolver.CalculateEnergyLevels(radius_nm, material.EffectiveMassElectron, maxLevels, 2);
        var holeLevels = QuantumSolver.CalculateEnergyLevels(radius_nm, material.EffectiveMassHole, maxLevels, 2);

        // Markiere Teilchenart über das Label
        foreach (var level in electronLevels)
            level.Label = $"e-{level.Label}";
        foreach (var level in holeLevels)
            level.Label = $"h-{level.Label}";

        dot.EnergyLevels = electronLevels.Concat(holeLevels).OrderBy(e => e.Energy_eV).ToList();

        // Gitter (für spätere Visualisierung)
        dot.Atoms = _lattice.GenerateZincBlende(
            material.LatticeConstant_A,
            radius_nm,
            material.Cation,
            material.Anion);

        // Wahrscheinlichkeitsdichte-Wolke (1S-Elektronen-Grundzustand)
        var cloudGen = new ProbabilityCloudGenerator();
        dot.ElectronCloud = cloudGen.GenerateElectronCloud1S(radius_nm).ToList();

        return dot;
    }
}
