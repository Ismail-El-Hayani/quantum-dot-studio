using QuantumDotStudio.Core.Data;
using QuantumDotStudio.Core.Models;
using QuantumDotStudio.Solver;

namespace QuantumDotStudio.Tests;

/// <summary>
/// Tests für Core/Shell-Heterostrukturen: endlicher Potentialtopf,
/// Bandanpassung, Strain-Modell und Core/Shell-Gitter.
/// </summary>
public class CoreShellTests
{
    private static readonly List<Material> Materials = MaterialDatabase.Defaults;
    private static Material Get(string name) => Materials.Single(m => m.Name == name);

    // ---------- FiniteWellSolver ----------

    [Fact]
    public void FiniteWell_Energies_Are_Below_InfiniteWell_Energies()
    {
        // Ein endlicher Topf senkt die Confinement-Energie gegenüber dem
        // unendlichen Topf (die Wellenfunktion kann in die Barriere ausgelenken).
        double R = 3.0, mStar = 0.13, barrier = 0.5;
        var finite = FiniteWellSolver.FindSEnergies_eV(R, mStar, barrier, 3);
        double infinite1S = QuantumSolver.ConfinementEnergy(R, mStar, 1, 0);

        Assert.NotEmpty(finite);
        Assert.True(finite[0] < infinite1S, $"finite {finite[0]} should be < infinite {infinite1S}");
        // Gebundene Zustände müssen unterhalb der Barriere bleiben.
        Assert.All(finite, e => Assert.True(e < barrier));
    }

    [Fact]
    public void FiniteWell_Large_Barrier_Converges_To_InfiniteWell()
    {
        // Für V0 → ∞ nähert sich die endliche Lösung der analytischen — aber nur
        // logarithmisch langsam (Wellenfunktions-Leakage ~ 1/sqrt(V0), gesteuert
        // durch kappa·R). Test dokumentiert beides: Monotonie und Grenzwert.
        double R = 3.0, mStar = 0.13;
        double inf1S = QuantumSolver.ConfinementEnergy(R, mStar, 1, 0);

        var moderate = FiniteWellSolver.FindSEnergies_eV(R, mStar, barrier_eV: 50.0, maxStates: 1);
        var huge = FiniteWellSolver.FindSEnergies_eV(R, mStar, barrier_eV: 5000.0, maxStates: 1);

        // Unterhalb der unendlichen-Topf-Energie ...
        Assert.True(moderate[0] < inf1S);
        Assert.True(huge[0] < inf1S);
        // ... monoton ansteigend mit V0 ...
        Assert.True(huge[0] > moderate[0]);
        // ... und für sehr große Barrieren innerhalb von 1 % am Grenzwert.
        Assert.True(Math.Abs(huge[0] - inf1S) / inf1S < 0.01,
            $"huge[0]={huge[0]}, inf1S={inf1S}");
    }

    [Fact]
    public void FiniteWell_Below_3D_Threshold_Binds_No_State()
    {
        // 3D-Kriterium (anders als 1D!): Ein S-Zustand im sphärischen Topf ist nur
        // gebunden, wenn V0 > hbar^2/(2 m R^2) = E_1S(unendlich)/pi^2.
        // (Anschaulich: die Barriere muß die Threshold-Energie übersteigen, nicht
        // die 1D-Nullpunktsenergie.)
        double R = 3.0, mStar = 0.13;
        double e1 = QuantumSolver.ConfinementEnergy(R, mStar, 1, 0);
        double threshold = e1 / (Math.PI * Math.PI);

        var below = FiniteWellSolver.FindSEnergies_eV(R, mStar, barrier_eV: threshold * 0.5, maxStates: 3);
        var above = FiniteWellSolver.FindSEnergies_eV(R, mStar, barrier_eV: threshold * 5.0, maxStates: 3);

        Assert.Empty(below);
        Assert.NotEmpty(above);
    }

    [Fact]
    public void FiniteWell_More_States_With_Higher_Barrier()
    {
        double R = 3.0, mStar = 0.13;
        var few = FiniteWellSolver.FindSEnergies_eV(R, mStar, 0.3, 5);
        var many = FiniteWellSolver.FindSEnergies_eV(R, mStar, 3.0, 5);

        Assert.True(many.Count >= few.Count);
        Assert.Equal(many.Count, many.Count); // aufsteigend sortiert
        for (int i = 1; i < many.Count; i++)
            Assert.True(many[i] > many[i - 1]);
    }

    [Fact]
    public void FiniteWell_Invalid_Input_Returns_Empty()
    {
        Assert.Empty(FiniteWellSolver.FindSEnergies_eV(0, 0.13, 0.5, 3));
        Assert.Empty(FiniteWellSolver.FindSEnergies_eV(3, 0, 0.5, 3));
        Assert.Empty(FiniteWellSolver.FindSEnergies_eV(3, 0.13, 0, 3));
        Assert.Empty(FiniteWellSolver.FindSEnergies_eV(3, 0.13, 0.5, 0));
    }

    // ---------- BandAlignment ----------

    [Fact]
    public void BandOffsets_CdSe_CdS_Are_Positive()
    {
        // CdSe/CdS ist die Industriestandard-Struktur: beide Träger sind im
        // CdSe-Core confinement (quasi Typ-I).
        var cdSe = Get("CdSe");
        var cdS = Get("CdS");

        double vE = BandAlignment.ElectronBarrier_eV(cdSe, cdS);
        double vH = BandAlignment.HoleBarrier_eV(cdSe, cdS);

        Assert.True(vE > 0, $"electron barrier {vE} should be > 0");
        Assert.True(vH > 0, $"hole barrier {vH} should be > 0");
    }

    [Fact]
    public void BandOffsets_Sum_To_BandGap_Difference()
    {
        // Summenregel: V0_e + V0_h = Eg_shell − Eg_core.
        var cdSe = Get("CdSe");
        var znS = Get("ZnS");

        double vE = BandAlignment.ElectronBarrier_eV(cdSe, znS);
        double vH = BandAlignment.HoleBarrier_eV(cdSe, znS);

        Assert.Equal(znS.BandGap_eV - cdSe.BandGap_eV, vE + vH, tolerance: 1e-9);
    }

    // ---------- StrainModel ----------

    [Fact]
    public void LatticeMismatch_CdSe_CdS_Is_About_Minus_3_6_Percent()
    {
        double f = StrainModel.LatticeMismatch(Get("CdSe"), Get("CdS"));
        Assert.InRange(f, -0.040, -0.030);
    }

    [Fact]
    public void CriticalThickness_CdSe_ZnS_Is_Small()
    {
        // CdSe/ZnS: hoher Missfit -> nur sehr dünne Schalen kohärent.
        double tc = StrainModel.CriticalThickness_nm(Get("CdSe"), Get("ZnS"));
        Assert.InRange(tc, 1.0, 2.0);
    }

    [Fact]
    public void InP_CdS_Is_Nearly_Lattice_Matched()
    {
        double f = StrainModel.LatticeMismatch(Get("InP"), Get("CdS"));
        Assert.True(Math.Abs(f) < 0.01, $"|f| = {Math.Abs(f)}");
        // Kritische Dicke formell sehr groß.
        Assert.True(StrainModel.CriticalThickness_nm(Get("InP"), Get("CdS")) > 10);
    }

    [Fact]
    public void IsStrainRelaxed_Flag_Follows_Thickness()
    {
        var core = Get("CdSe");
        var shell = Get("ZnS");
        double tc = StrainModel.CriticalThickness_nm(core, shell);

        Assert.False(StrainModel.IsStrainRelaxed(core, shell, tc * 0.5));
        Assert.True(StrainModel.IsStrainRelaxed(core, shell, tc * 2.0));
    }

    // ---------- Core/Shell-Gitter ----------

    [Fact]
    public void CoreShellLattice_Separates_Core_And_Shell_Elements()
    {
        var engine = new LatticeEngine();
        var atoms = engine.GenerateZincBlendeCoreShell(
            6.05, coreRadius_nm: 1.5, shellThickness_nm: 1.0,
            "Cd", "Se", "Zn", "S");

        Assert.Contains(atoms, a => a.Element == "Cd");
        Assert.Contains(atoms, a => a.Element == "Se");
        Assert.Contains(atoms, a => a.Element == "Zn");
        Assert.Contains(atoms, a => a.Element == "S");

        // Kein Core-Atom außerhalb des Core-Radius, kein Shell-Atom innerhalb.
        double coreSquared = 1.5 * 1.5 + 1e-6;
        double totalSquared = 2.5 * 2.5 + 1e-6;
        Assert.All(atoms, a => Assert.True(a.Position.LengthSquared <= totalSquared));
        Assert.All(
            atoms.Where(a => a.Element == "Cd" || a.Element == "Se"),
            a => Assert.True(a.Position.LengthSquared <= coreSquared));
        Assert.All(
            atoms.Where(a => a.Element == "Zn" || a.Element == "S"),
            a => Assert.True(a.Position.LengthSquared > 1.5 * 1.5 - 1e-6));
    }

    [Fact]
    public void CoreShellLattice_ZeroShell_Equals_HomogeneousLattice()
    {
        var engine = new LatticeEngine();
        var homogeneous = engine.GenerateZincBlende(6.05, 2.0, "Cd", "Se");
        var coreShell = engine.GenerateZincBlendeCoreShell(
            6.05, coreRadius_nm: 2.0, shellThickness_nm: 0.0,
            "Cd", "Se", "Zn", "S");

        Assert.Equal(homogeneous.Count, coreShell.Count);
    }

    // ---------- Service: BuildCoreShellQuantumDot ----------

    [Fact]
    public void BuildCoreShell_CdSe_CdS_Produces_Valid_Dot()
    {
        var service = new QuantumDotService();
        var dot = service.BuildCoreShellQuantumDot(Get("CdSe"), Get("CdS"), 2.0, 0.6);

        Assert.Equal("CdSe", dot.Material.Name);
        Assert.Equal("CdS", dot.ShellMaterial.Name);
        Assert.Equal(2.0, dot.CoreRadius_nm);
        Assert.Equal(0.6, dot.ShellThickness_nm);
        Assert.Equal(2.6, dot.Radius_nm);

        // Beide Träger gebunden (Typ-I-artig).
        Assert.True(dot.ElectronBarrier_eV > 0);
        Assert.True(dot.HoleBarrier_eV > 0);
        Assert.NotEmpty(dot.EnergyLevels);

        // Bandlücke zwischen Bulk- und unendlichem-Topf-Wert.
        double infiniteGap = QuantumSolver.BrusBandGap(Get("CdSe"), 2.0);
        Assert.InRange(dot.TotalBandGap_eV, Get("CdSe").BandGap_eV, infiniteGap);

        // Wellenlänge endlich und positiv.
        Assert.True(dot.EmissionWavelength_nm > 0 && !double.IsInfinity(dot.EmissionWavelength_nm));

        // Strain: 0.6 nm CdS auf CdSe liegt unter der kritischen Dicke (~4.2 nm).
        Assert.False(dot.IsStrainRelaxed);

        // Gitter: CdSe-Core (Cd, Se) + CdS-Shell (Cd, S) — Cd kommt in beiden vor.
        var elements = dot.Atoms.Select(a => a.Element).Distinct().OrderBy(e => e).ToList();
        Assert.Equal(new[] { "Cd", "S", "Se" }, elements);
    }

    [Fact]
    public void BuildCoreShell_Invalid_Parameters_Throw()
    {
        var service = new QuantumDotService();
        var core = Get("CdSe");
        var shell = Get("CdS");

        Assert.Throws<ArgumentOutOfRangeException>(() =>
            service.BuildCoreShellQuantumDot(core, shell, 0, 0.5));
        Assert.Throws<ArgumentOutOfRangeException>(() =>
            service.BuildCoreShellQuantumDot(core, shell, 2.0, -0.1));
    }

    [Fact]
    public void BuildCoreShell_ZeroShell_FallsBack_To_Homogeneous()
    {
        var service = new QuantumDotService();
        var dot = service.BuildCoreShellQuantumDot(Get("CdSe"), Get("CdS"), 2.0, 0.0);

        // Ohne Schale: nur Core-Elemente im Gitter.
        var elements = dot.Atoms.Select(a => a.Element).Distinct().ToList();
        Assert.Equal(new[] { "Cd", "Se" }, elements.OrderBy(e => e).ToList());
        Assert.Equal(2.0, dot.Radius_nm);
    }
}