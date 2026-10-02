using QuantumDotStudio.Core.Data;
using QuantumDotStudio.Core.Models;

namespace QuantumDotStudio.Solver;

/// <summary>
/// Verbindet Materialauswahl, Solver und Gittergenerierung zu einem berechneten QuantumDot-Objekt.
/// Zwischenergebnisse für identisches (Material, Radius) werden gecacht.
/// </summary>
public class QuantumDotService
{
    private readonly LatticeEngine _lattice;
    private readonly Dictionary<(string MaterialName, double Radius_nm), QuantumDot> _cache;

    public QuantumDotService()
    {
        _lattice = new LatticeEngine();
        _cache = new Dictionary<(string MaterialName, double Radius_nm), QuantumDot>();
    }

    /// <summary>
    /// Erzeugt ein vollständig berechnetes Quantum Dot für das gewählte Material und den Radius.
    /// Nutzt einen Cache, um wiederholte Berechnungen mit gleichen Parametern zu vermeiden.
    /// </summary>
    public QuantumDot BuildQuantumDot(Material material, double radius_nm, int maxLevels = 6)
    {
        if (material == null)
            throw new ArgumentNullException(nameof(material));

        var key = (material.Name, radius_nm);
        if (_cache.TryGetValue(key, out var cached))
            return cached;

        var dot = new QuantumDot
        {
            Material = material,
            Radius_nm = radius_nm
        };

        // Elektron-Confinement (n=1, l=0)
        dot.ConfinementEnergyElectron_eV = QuantumSolver.ConfinementEnergy(radius_nm, material.EffectiveMassElectron, 1, 0);

        // Loch-Confinement (n=1, l=0)
        dot.ConfinementEnergyHole_eV = QuantumSolver.ConfinementEnergy(radius_nm, material.EffectiveMassHole, 1, 0);

        // Effektive Bandlücke (volle Brus-Gleichung inkl. Coulomb-Term)
        dot.TotalBandGap_eV = QuantumSolver.BrusBandGap(material, radius_nm);
        dot.CoulombEnergy_eV = QuantumSolver.CoulombEnergy(radius_nm, material.DielectricConstant);

        // Emissionswellenlänge
        dot.EmissionWavelength_nm = QuantumSolver.WavelengthFromBandGap(dot.TotalBandGap_eV);

        // Energieniveaus bis maxLevels (pro Teilchentyp)
        var electronLevels = QuantumSolver.CalculateEnergyLevels(radius_nm, material.EffectiveMassElectron, maxLevels, 2);
        var holeLevels = QuantumSolver.CalculateEnergyLevels(radius_nm, material.EffectiveMassHole, maxLevels, 2);

        // Teilchenart am Niveau markieren
        foreach (var level in electronLevels)
            level.Particle = Particle.Electron;
        foreach (var level in holeLevels)
            level.Particle = Particle.Hole;

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

        _cache[key] = dot;
        return dot;
    }

    /// <summary>
    /// Löscht den Cache, z. B. wenn sich Materialdaten zur Laufzeit ändern.
    /// </summary>
    public void ClearCache()
    {
        _cache.Clear();
    }
}
