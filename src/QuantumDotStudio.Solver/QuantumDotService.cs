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

    /// <summary>
    /// Baut ein Core/Shell-Quantum Dot: berechnet Band-Offsets (aus
    /// Elektronenaffinitäten), Gitterfehlanpassung und kritische Schalendicke
    /// (Matthews–Blakeslee), die gebundenen Elektronen-/Loch-Zustände im Core
    /// (endlicher Potentialtopf mit den Band-Offsets als Barriere) und die
    /// effektive Bandlücke aus dem tiefsten gebundenen Elektron-Loch-Übergang.
    /// Atome: Core/Shell-Gitter mit korrekten Elementen je Region.
    /// </summary>
    /// <param name="core">Core-Material.</param>
    /// <param name="shell">Shell-Material.</param>
    /// <param name="coreRadius_nm">Core-Radius in nm (1..10).</param>
    /// <param name="shellThickness_nm">Schalendicke in nm (0..5).</param>
    /// <param name="maxStates">Maximale Anzahl gebundener Zustände pro Teilchen.</param>
    public CoreShellQuantumDot BuildCoreShellQuantumDot(
        Material core, Material shell,
        double coreRadius_nm, double shellThickness_nm,
        int maxStates = 3)
    {
        ArgumentNullException.ThrowIfNull(core);
        ArgumentNullException.ThrowIfNull(shell);

        if (coreRadius_nm <= 0)
            throw new ArgumentOutOfRangeException(nameof(coreRadius_nm), "Core-Radius muss positiv sein.");
        if (shellThickness_nm < 0)
            throw new ArgumentOutOfRangeException(nameof(shellThickness_nm), "Schalendicke darf nicht negativ sein.");

        double electronBarrier = BandAlignment.ElectronBarrier_eV(core, shell);
        double holeBarrier = BandAlignment.HoleBarrier_eV(core, shell);

        var dot = new CoreShellQuantumDot
        {
            Material = core,
            ShellMaterial = shell,
            CoreRadius_nm = coreRadius_nm,
            ShellThickness_nm = shellThickness_nm,
            Radius_nm = coreRadius_nm + shellThickness_nm,
            ElectronBarrier_eV = electronBarrier,
            HoleBarrier_eV = holeBarrier,
            LatticeMismatch_f = StrainModel.LatticeMismatch(core, shell),
            CriticalThickness_nm = StrainModel.CriticalThickness_nm(core, shell)
        };

        // Gebundene Zustände im endlichen Topf: Barriere = Band-Offset.
        // Positive Barriere = Confinement im Core; ohne gebundene Zustände
        // (zu flache Barriere oder Typ-II) bleibt die Energie-Liste leer
        // und die Bandlücke fällt auf den Bulk-Wert zurück.
        var electronEnergies = electronBarrier > 0
            ? FiniteWellSolver.FindSEnergies_eV(coreRadius_nm, core.EffectiveMassElectron, electronBarrier, maxStates)
            : new List<double>();
        var holeEnergies = holeBarrier > 0
            ? FiniteWellSolver.FindSEnergies_eV(coreRadius_nm, core.EffectiveMassHole, holeBarrier, maxStates)
            : new List<double>();

        dot.EnergyLevels = electronEnergies.Select((e, i) => new EnergyLevel
        {
            PrincipalQuantumNumber_n = i + 1,
            AngularMomentum_l = 0,
            Energy_eV = e,
            Label = $"{i + 1}S",
            Particle = Particle.Electron
        })
        .Concat(holeEnergies.Select((e, i) => new EnergyLevel
        {
            PrincipalQuantumNumber_n = i + 1,
            AngularMomentum_l = 0,
            Energy_eV = e,
            Label = $"{i + 1}S",
            Particle = Particle.Hole
        }))
        .OrderBy(e => e.Energy_eV)
        .ToList();

        // Tiefster Übergang: Grundzustands-Energien von Elektron und Loch.
        double eE = electronEnergies.FirstOrDefault();
        double hE = holeEnergies.FirstOrDefault();
        double groundTransition = (electronEnergies.Count > 0 && holeEnergies.Count > 0)
            ? eE + hE
            : double.NaN;

        dot.ConfinementEnergyElectron_eV = electronEnergies.Count > 0 ? eE : 0.0;
        dot.ConfinementEnergyHole_eV = holeEnergies.Count > 0 ? hE : 0.0;
        dot.TotalBandGap_eV = double.IsNaN(groundTransition)
            ? core.BandGap_eV
            : core.BandGap_eV + groundTransition;
        dot.EmissionWavelength_nm = QuantumSolver.WavelengthFromBandGap(dot.TotalBandGap_eV);

        // Coulomb: Core-Dielektrikum, Radius = Core-Radius (Emissionsvolumen).
        dot.CoulombEnergy_eV = QuantumSolver.CoulombEnergy(coreRadius_nm, core.DielectricConstant);

        // Gitter: Core/Shell-Ausschnitt mit korrekten Elementen je Region.
        dot.Atoms = shellThickness_nm > 0
            ? _lattice.GenerateZincBlendeCoreShell(
                core.LatticeConstant_A, coreRadius_nm, shellThickness_nm,
                core.Cation, core.Anion, shell.Cation, shell.Anion)
            : _lattice.GenerateZincBlende(
                core.LatticeConstant_A, coreRadius_nm, core.Cation, core.Anion);

        // Wahrscheinlichkeitswolke auf den Core beschränkt (Emissionsvolumen).
        dot.ElectronCloud = new ProbabilityCloudGenerator().GenerateElectronCloud1S(coreRadius_nm).ToList();

        return dot;
    }
}
