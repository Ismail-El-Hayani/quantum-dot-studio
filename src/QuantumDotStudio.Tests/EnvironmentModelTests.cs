using QuantumDotStudio.Core.Data;
using QuantumDotStudio.Core.Models;
using QuantumDotStudio.Core.Physics;
using QuantumDotStudio.Solver;

namespace QuantumDotStudio.Tests;

/// <summary>
/// Tests für das Umgebungsmodell (Roadmap 2.3): Varshni-Temperaturverschiebung,
/// Solvens-Abschirmung des Coulomb-Terms, Liganden-pH-Fenster und deren
/// Verdrahtung in Service, Spektrum-Anker und Machbarkeits-Faktor 5.
/// </summary>
public class EnvironmentModelTests
{
    private static readonly List<Material> Materials = MaterialDatabase.Defaults;
    private static Material Get(string name) => Materials.Single(m => m.Name == name);

    private static readonly QuantumDotService Service = new();

    private static Solvent Solvent(string name) =>
        SolventDatabase.Defaults.Single(s => s.Name == name);

    private static Ligand Ligand(string id) =>
        SensorDatabase.Ligands.Single(l => l.LigandId == id);

    // ---------- Varshni ----------

    [Fact]
    public void Varshni_Shift_Is_Exactly_Zero_At_Reference_Temperature()
    {
        // Anker: bei 300 K bleibt die materials.json-Lücke exakt erhalten —
        // Standard-Umgebung ist damit bit-identisch zur bisherigen Rechnung.
        double shift = EnvironmentModel.VarshniShift_eV(4.4e-4, 181, 300.0);

        Assert.Equal(0.0, shift, tolerance: 0.0);
    }

    [Fact]
    public void Varshni_Shift_Grows_With_Temperature_And_Shrinks_The_Gap()
    {
        // Varshni 1967 (CdSe: α = 4.4e-4 eV/K, β = 181 K): T↑ → E_g↓.
        double shift373 = EnvironmentModel.VarshniShift_eV(4.4e-4, 181, 373.0);
        double shift450 = EnvironmentModel.VarshniShift_eV(4.4e-4, 181, 450.0);

        Assert.True(shift373 > 0.0);
        Assert.True(shift450 > shift373);

        double gap373 = EnvironmentModel.BandGapAtTemperature(1.74, 4.4e-4, 181, 373.0);
        Assert.True(gap373 < 1.74);
    }

    [Fact]
    public void Varshni_CdSe_373K_Shift_Matches_Measured_QD_Redshift_Order()
    {
        // Gemessener Anker: CdSe/ZnS-QDs zeigen zwischen 300 und 373 K eine
        // PL-Rotverschiebung von ~4–6 nm (ΔE_g ≈ 0.0224 eV, QD-α ≈ 2.07e-4 —
        // Superlattices & Microstructures 46 (2009) 4-6). Das Bulk-Parameter-
        // Modell (Varshni 1967) sagt ~0.028 eV voraus — gleiche Größenordnung.
        double shift = EnvironmentModel.VarshniShift_eV(4.4e-4, 181, 373.0);

        Assert.InRange(shift, 0.020, 0.035);
    }

    // ---------- Solvens ----------

    [Fact]
    public void EffectiveDielectric_Blends_Dot_And_Solvent()
    {
        // Phänomenologische Mischung: ε_eff = (2·ε_dot + ε_sol)/3.
        Assert.Equal((2.0 * 10.6 + 78.4) / 3.0,
            EnvironmentModel.EffectiveDielectric(10.6, 78.4), tolerance: 1e-12);

        // Ohne Solvens (null oder Sentinel ε = 0): unveränderte Dot-ε_r.
        Assert.Equal(10.6, EnvironmentModel.EffectiveDielectric(10.6, null), tolerance: 1e-12);
        Assert.Equal(10.6, EnvironmentModel.EffectiveDielectric(10.6, 0.0), tolerance: 1e-12);
    }

    [Fact]
    public void High_Epsilon_Solvent_Screens_Coulomb_And_Blueshifts_Emission()
    {
        var cdSe = Get("CdSe");
        var inWater = Service.BuildQuantumDot(cdSe, 3.0,
            environment: new EnvironmentSettings(Solvent: Solvent("Wasser")));
        var inToluene = Service.BuildQuantumDot(cdSe, 3.0,
            environment: new EnvironmentSettings(Solvent: Solvent("Toluol")));

        // Wasser (ε_eff ≈ 33.2) schirmt stärker ab als Toluol (ε_eff ≈ 7.9):
        // |Coulomb| kleiner → Lücke größer → Emission blauer.
        Assert.True(Math.Abs(inWater.CoulombEnergy_eV) < Math.Abs(inToluene.CoulombEnergy_eV));
        Assert.True(inWater.TotalBandGap_eV > inToluene.TotalBandGap_eV);
        Assert.True(inWater.EmissionWavelength_nm < inToluene.EmissionWavelength_nm);
    }

    [Fact]
    public void Default_Environment_Is_Bit_Identical_To_Legacy_Path()
    {
        // Regressionsschutz: ohne Umgebung (null oder Standard) bleibt die
        // Brus-Rechnung exakt die bisherige — Sensor- und Machbarkeits-Engine
        // sehen bei Standardbedingungen keine einzige Bit-Änderung.
        var cdSe = Get("CdSe");

        var legacy = Service.BuildQuantumDot(cdSe, 3.0);
        var defaulted = Service.BuildQuantumDot(cdSe, 3.0, environment: new EnvironmentSettings());

        Assert.Equal(QuantumSolver.BrusBandGap(cdSe, 3.0), legacy.TotalBandGap_eV, tolerance: 0.0);
        Assert.Equal(legacy.TotalBandGap_eV, defaulted.TotalBandGap_eV, tolerance: 0.0);
        Assert.Equal(legacy.EmissionWavelength_nm, defaulted.EmissionWavelength_nm, tolerance: 0.0);
    }

    [Fact]
    public void Environment_Is_Part_Of_The_Cache_Key()
    {
        var cdSe = Get("CdSe");

        var dot300 = Service.BuildQuantumDot(cdSe, 3.0, environment: new EnvironmentSettings());
        var dot450 = Service.BuildQuantumDot(cdSe, 3.0, environment: new EnvironmentSettings(Temperature_K: 450.0));
        var dot300Again = Service.BuildQuantumDot(cdSe, 3.0, environment: new EnvironmentSettings());

        // Heißes Dot emittiert roter (Varshni) ...
        Assert.True(dot300.EmissionWavelength_nm < dot450.EmissionWavelength_nm,
            $"300 K: {dot300.EmissionWavelength_nm:F1} nm sollte blauer sein als 450 K: {dot450.EmissionWavelength_nm:F1} nm");
        // ... und gleiche Umgebung trifft denselben Cache-Eintrag.
        Assert.Same(dot300, dot300Again);
    }

    [Fact]
    public void Environment_Flows_Through_The_Whole_Pipeline()
    {
        // End-to-End: bei 373 K verschiebt sich die Emission, bleibt aber an
        // TotalBandGap verankert — auch der 1S-Grundzustands-Übergang
        // des Spektrums folgt der Umgebung (Roadmap 2.3 + optische Übergänge).
        var cdSe = Get("CdSe");
        var dot = Service.BuildQuantumDot(cdSe, 3.0,
            environment: new EnvironmentSettings(Temperature_K: 373.0));

        var ground = Assert.Single(dot.OpticalTransitions, t => t.IsGroundState);

        Assert.Equal(dot.TotalBandGap_eV, ground.Energy_eV, tolerance: 1e-12);
        Assert.Equal(dot.EmissionWavelength_nm, ground.Wavelength_nm, tolerance: 1e-6);
        Assert.True(dot.EmissionWavelength_nm > 597.0,
            $"373 K ({dot.EmissionWavelength_nm:F1} nm) sollte roter sein als 300 K (~598 nm)");
    }

    // ---------- pH-Fenster ----------

    [Fact]
    public void PhWindowScore_Inside_Outside_And_Not_Applicable()
    {
        var mpa = Ligand("MPA"); // Fenster 4–11 (Carboxylat/Thiolat-Chemie)

        Assert.Equal(1.0, EnvironmentModel.PhWindowScore(mpa, 7.0), tolerance: 1e-12);
        Assert.Equal(1.0, EnvironmentModel.PhWindowScore(mpa, 4.0), tolerance: 1e-12); // Ränder inklusive
        Assert.Equal(0.25, EnvironmentModel.PhWindowScore(mpa, 3.0), tolerance: 1e-12);

        var topo = Ligand("TOPO"); // organisch: pH-unabhängig, „null heißt nicht schlecht“
        Assert.Equal(1.0, EnvironmentModel.PhWindowScore(topo, 2.0), tolerance: 1e-12);

        Assert.Equal(1.0, EnvironmentModel.PhWindowScore(null, 1.0), tolerance: 1e-12);
    }

    [Fact]
    public void StabilityScore_With_Ph_Inside_Window_Unchanged_Outside_Degraded()
    {
        var mpa = Ligand("MPA");

        double withoutPh = FeasibilityFactors.StabilityScore(mpa, "aqueous", null);
        double inside = FeasibilityFactors.StabilityScore(mpa, "aqueous", null, ph: 7.0);
        double outside = FeasibilityFactors.StabilityScore(mpa, "aqueous", null, ph: 3.0);

        // Im Fenster: kein Effekt (Abwärtskompatibilität der 3-Argument-Rufe).
        Assert.Equal(withoutPh, inside, tolerance: 1e-12);
        // Außerhalb: kolloidale Stabilität bricht ein (Faktor 0.25).
        Assert.Equal(withoutPh * 0.25, outside, tolerance: 1e-12);
    }

    [Fact]
    public void Feasibility_Engine_Picks_Up_Environment_Ph()
    {
        // Roadmap 2.3 × 3: design.EnvironmentPh senkt die Machbarkeit, wenn der
        // pH außerhalb des Liganden-Fensters liegt (Faktor 5 der Engine).
        SensorDesign Design(double ph) => new SensorDesign
        {
            CoreMaterial = Get("CdSe"),
            ShellMaterial = Get("CdS"),
            CoreRadius_nm = 3.0,
            ShellThickness_nm = 0.6,
            EmissionWavelength_nm = 600.0,
            Ligand = Ligand("MPA"),
            Analyte = SensorDatabase.Analytes.Single(a => a.AnalyteId == "Pb2+"),
            TargetConcentration_M = 1e-6,
            Application = new ApplicationTemplate
            {
                ApplicationId = "test",
                Medium = "aqueous",
                EmissionWindowMin_nm = 550.0,
                EmissionWindowMax_nm = 650.0
            },
            Environment = new EnvironmentSettings(Ph: ph)
        };

        double pIn = FeasibilityEngine.Evaluate(Design(7.0)).P;
        double pOut = FeasibilityEngine.Evaluate(Design(3.0)).P;

        Assert.True(pIn > 0.0);
        Assert.True(pIn > pOut, $"P(pH 7) = {pIn:F3} sollte > P(pH 3) = {pOut:F3} sein");
    }

    // ---------- Datenbank ----------

    [Fact]
    public void SolventDatabase_Loads_From_Deployed_Json()
    {
        // AC-004-Muster: solvents.json muss ins Ausgabeverzeichnis deployt
        // werden und tatsächlich als Quelle dienen.
        Assert.NotNull(SolventDatabase.LoadedFrom);
        Assert.True(File.Exists(SolventDatabase.LoadedFrom),
            $"solvents.json wurde nicht ins Ausgabeverzeichnis kopiert: {SolventDatabase.LoadedFrom}");
        Assert.Contains(SolventDatabase.Defaults, s => s.Name == "Wasser" && s.DielectricConstant > 50.0);
    }
}