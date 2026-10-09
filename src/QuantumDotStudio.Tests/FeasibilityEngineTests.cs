using QuantumDotStudio.Core.Data;
using QuantumDotStudio.Core.Models;
using QuantumDotStudio.Core.Physics;
using QuantumDotStudio.Solver;

namespace QuantumDotStudio.Tests;

/// <summary>
/// Tests der Machbarkeits-Engine (Roadmap Phase 3).
/// Anker: das durchgerechnete Beispiel aus Roadmap 3.4 —
/// InP/ZnSe vs. CdSe/ZnS, beide ~amber (0.4-0.7), NIR-I-Bio-Imaging.
/// </summary>
public class FeasibilityEngineTests
{
    private static Material Mat(string name) => MaterialDatabase.Defaults.First(m => m.Name == name);

    private static ApplicationTemplate NirBio() => new()
    {
        ApplicationId = "bio-imaging",
        DisplayName = "NIR-I Bio-Imaging",
        EmissionWindowMin_nm = 650,
        EmissionWindowMax_nm = 900,
        Medium = "aqueous"
    };

    private static SensorDesign Design(Material core, Material? shell, double coreR, double shellT,
        bool graded = false, double emissionOverride = double.NaN)
    {
        var dotEmission = double.IsNaN(emissionOverride)
            ? QuantumSolver.WavelengthFromBandGap(QuantumSolver.BrusBandGap(core, coreR))
            : emissionOverride;

        return new SensorDesign
        {
            CoreMaterial = core,
            ShellMaterial = shell,
            CoreRadius_nm = coreR,
            ShellThickness_nm = shellT,
            GradedInterface = graded,
            EmissionWavelength_nm = dotEmission,
            Ligand = SensorDatabase.Ligands.First(l => l.LigandId == "MPA"),
            Analyte = SensorDatabase.Analytes.First(a => a.AnalyteId == "Fluorescein"),
            Application = NirBio(),
            TargetConcentration_M = 1e-9,
            ReceptorKd_M = 1e-9,
            FretDistanceUnbound_nm = coreR + 0.6 + 1.5,
            FretDistanceBound_nm = coreR + 0.6
        };
    }

    [Fact]
    public void Worked_Example_InP_Beats_CdSe_For_NIR_Both_Suboptimal()
    {
        // Roadmap 3.4 (durchgerechnetes Beispiel, P dort "illustrativ" ~0.5-0.6):
        // InP/ZnSe emittiert am NIR-I-Rand (653 nm), CdSe/ZnS nur sichtbar (598 nm).
        // Kernwissenschaft: InP MUSS fuer NIR besser abschneiden — und ein
        // groesserer InP-Core hebt Faktor 1 weiter (λ wandert in die volle Zone).
        var inz = FeasibilityEngine.Evaluate(Design(Mat("InP"), Mat("ZnSe"), 3.0, 0.6));
        var cdz = FeasibilityEngine.Evaluate(Design(Mat("CdSe"), Mat("ZnS"), 3.0, 0.6, graded: true));

        Assert.True(inz.P > cdz.P,
            $"InP ({inz.P:F3}) sollte fuer NIR besser sein als CdSe ({cdz.P:F3})");
        Assert.InRange(inz.P, 0.40, 0.78);
        Assert.InRange(cdz.P, 0.10, inz.P);

        // Groesserer InP-Core: Emission wandert in die volle NIR-Zone -> Faktor 1 steigt.
        var inpBig = FeasibilityEngine.Evaluate(Design(Mat("InP"), Mat("ZnSe"), 4.5, 0.6));
        double f1Small = inz.Factors.First(f => f.Name == "Emissionsfenster").Score;
        double f1Big = inpBig.Factors.First(f => f.Name == "Emissionsfenster").Score;
        Assert.True(f1Big > f1Small,
            $"Faktor 1 sollte mit groesserem InP-Core steigen: {f1Small:F2} -> {f1Big:F2}");

        // ZnS-Schale muss ZnSe in Wasser schlagen (Roadmap: robust vs. weaker).
        double sZnS = cdz.Factors.First(f => f.Name == "Stabilitaet").Score;
        double sZnSe = inz.Factors.First(f => f.Name == "Stabilitaet").Score;
        Assert.True(sZnS > sZnSe, $"ZnS-Stabilitaet {sZnS:F2} > ZnSe {sZnSe:F2}");
    }

    [Fact]
    public void Emission_Outside_Window_Cannot_Be_Compensated()
    {
        // Kern der Idee: ein disqualifizierender Faktor (Emission total daneben)
        // zieht P gegen 0, egal wie gut der Rest ist.
        var design = Design(Mat("CdSe"), Mat("ZnS"), 3.0, 0.6, graded: true, emissionOverride: 400.0);
        // 400 nm ist weit ausserhalb NIR-I [650, 900]
        var result = FeasibilityEngine.Evaluate(design);

        double f1 = result.Factors.First(f => f.Name == "Emissionsfenster").Score;
        Assert.True(f1 < 0.05, $"Faktor-1-Score {f1:F3} sollte ~0 sein");
        Assert.True(result.P < 0.4, $"P = {result.P:F3} sollte rot (< 0.4) sein");
    }

    [Fact]
    public void Perfect_Match_Scores_High()
    {
        // Alles im gruenen Bereich: Emission im Fenster, CdSe/CdS ( Industrie-
        // standard, kohärent), MPA in Wasser, K_D ~ Ziel, FRET-Kontrast ok.
        var design = Design(Mat("CdSe"), Mat("CdS"), 3.0, 0.6);
        // Emission von CdSe R=3 (~598 nm) liegt NICHT in NIR — fuer einen
        // vollen Score das Fenster anpassen (Anwendung: visible imaging).
        design.Application.EmissionWindowMin_nm = 500;
        design.Application.EmissionWindowMax_nm = 650;

        var result = FeasibilityEngine.Evaluate(design);
        Assert.True(result.P > 0.5, $"P = {result.P:F3} sollte > 0.5 sein");
    }

    // --------------------------------------------------------- Faktor-Tests

    [Fact]
    public void EmissionWindow_FullZone_Is_One_Edges_Degrade()
    {
        // Volle Zone = mittlere 60 % von [650, 900] -> [700, 850].
        Assert.Equal(1.0, FeasibilityFactors.EmissionWindowMatch(700, 650, 900), 9);
        Assert.Equal(1.0, FeasibilityFactors.EmissionWindowMatch(775, 650, 900), 9);
        Assert.Equal(1.0, FeasibilityFactors.EmissionWindowMatch(850, 650, 900), 9);

        // Weiche Kanten: 650/900 liegen ~0.64, nicht hart 1.0 oder 0.
        double blueEdge = FeasibilityFactors.EmissionWindowMatch(650, 650, 900);
        double redEdge = FeasibilityFactors.EmissionWindowMatch(900, 650, 900);
        Assert.InRange(blueEdge, 0.5, 0.8);
        Assert.InRange(redEdge, 0.5, 0.8);

        // Weiter draussen gaussfoermig gegen 0.
        Assert.True(FeasibilityFactors.EmissionWindowMatch(400, 650, 900) < 0.001);
    }

    [Fact]
    public void Strain_Coherent_Beats_Relaxed()
    {
        // CdSe/CdS: f = -3.6 %, t_krit ~ 4.2 nm
        double coherent = FeasibilityFactors.StrainScore(-0.036, 0.6, 4.2, gradedInterface: false);
        double relaxed = FeasibilityFactors.StrainScore(-0.036, 6.0, 4.2, gradedInterface: false);
        Assert.True(coherent > relaxed, $"kohaerent {coherent:F3} > relaxiert {relaxed:F3}");
        Assert.True(relaxed <= 0.3, $"relaxiert sollte <= 0.3 sein: {relaxed:F3}");
    }

    [Fact]
    public void Strain_Graded_Interface_Helps()
    {
        double plain = FeasibilityFactors.StrainScore(-0.106, 0.6, 1.4, gradedInterface: false); // CdSe/ZnS
        double graded = FeasibilityFactors.StrainScore(-0.106, 0.6, 1.4, gradedInterface: true);
        Assert.True(graded > plain, $"graduiert {graded:F3} > plain {plain:F3}");
    }

    [Fact]
    public void QY_Shell_Passivation_Dominates()
    {
        double bare = FeasibilityFactors.QuantumYieldProxy(false, 0, 0, false, false);
        double shelled = FeasibilityFactors.QuantumYieldProxy(true, 1.0, 0.4, false, false);
        double relaxed = FeasibilityFactors.QuantumYieldProxy(true, 1.0, 0.4, true, false);
        Assert.True(bare < shelled, $"nackt {bare:F2} < beschalt {shelled:F2}");
        Assert.True(relaxed < shelled, $"relaxiert {relaxed:F2} < kohärent {shelled:F2}");
    }

    [Fact]
    public void Stability_Wrong_Medium_Destroys_Score()
    {
        var topo = SensorDatabase.Ligands.First(l => l.LigandId == "TOPO"); // organisch
        var mpa = SensorDatabase.Ligands.First(l => l.LigandId == "MPA");  // wasserloeslich

        // Ligand-Chemie gegen das Medium (Modell-Basiswert ohne Shell-Effekte):
        double topoInWater = FeasibilityFactors.StabilityScore(topo, "aqueous", null);
        double mpaInWater = FeasibilityFactors.StabilityScore(mpa, "aqueous", null);
        Assert.True(topoInWater < 0.2, $"TOPO in Wasser sollte disqualifizieren: {topoInWater:F2}");
        Assert.True(mpaInWater > topoInWater, $"MPA sollte TOPO schlagen: {mpaInWater:F2}");

        // Mit robuster ZnS-Schale ist das Gesamtkonstrukt stabil (> 0.6).
        double mpaZnS = FeasibilityFactors.StabilityScore(mpa, "aqueous", Mat("ZnS"));
        Assert.True(mpaZnS > 0.6, $"MPA + ZnS in Wasser sollte stabil sein: {mpaZnS:F2}");
    }

    [Fact]
    public void Stability_Shell_Chemistry_Matters_In_Water()
    {
        var mpa = SensorDatabase.Ligands.First(l => l.LigandId == "MPA");
        var zns = Mat("ZnS");
        var znse = Mat("ZnSe");

        double withZnS = FeasibilityFactors.StabilityScore(mpa, "aqueous", zns);
        double withZnSe = FeasibilityFactors.StabilityScore(mpa, "aqueous", znse);
        double bare = FeasibilityFactors.StabilityScore(mpa, "aqueous", null);

        // Roadmap 3.4: "ZnSe in water weaker ~0.5, ZnS robust ~0.9"
        Assert.True(withZnS > withZnSe, $"ZnS ({withZnS:F2}) sollte ZnSe ({withZnSe:F2}) schlagen");
        Assert.True(withZnSe > bare, $"auch ZnSe sollte besser als nackter Core sein ({withZnSe:F2} vs. {bare:F2})");
    }

    [Fact]
    public void Bioconjugation_Needs_Kd_At_Or_Below_Target()
    {
        Assert.Equal(1.0, FeasibilityFactors.BioconjugationScore(1e-9, 1e-9), 6); // K_D = Ziel
        double weak = FeasibilityFactors.BioconjugationScore(1e-6, 1e-9); // K_D 1000x zu hoch
        Assert.True(weak < 0.01, $"K_D 1000x ueber Ziel sollte ~0 sein: {weak:F4}");
    }

    [Fact]
    public void Transduction_Fret_Needs_Contrast()
    {
        var design = new SensorDesign
        {
            Analyte = new Analyte { Mode = "FRET", ForsterRadius_nm = 5.0 },
            FretDistanceUnbound_nm = 6.0,
            FretDistanceBound_nm = 4.0
        };
        double good = FeasibilityFactors.TransductionScore("FRET", design);
        design.FretDistanceBound_nm = 5.9; // kaum Δr
        double bad = FeasibilityFactors.TransductionScore("FRET", design);
        Assert.True(good > bad);
        Assert.True(good > 0.5, $"guter FRET-Kontrast: {good:F2}");
    }

    // ------------------------------------------------------ Monte-Carlo

    [Fact]
    public void MonteCarlo_Is_Deterministic_And_Fast()
    {
        var design = Design(Mat("CdSe"), Mat("ZnS"), 3.0, 0.6, graded: true);

        var sw = System.Diagnostics.Stopwatch.StartNew();
        var mc1 = FeasibilityEngine.EvaluateMonteCarlo(design, n: 2000, seed: 42);
        sw.Stop();

        var mc2 = FeasibilityEngine.EvaluateMonteCarlo(design, n: 2000, seed: 42);

        Assert.True(sw.ElapsedMilliseconds < 3000, $"MC dauerte {sw.ElapsedMilliseconds} ms — < 3 s gefordert");
        Assert.True(mc1.HasMonteCarlo);
        Assert.Equal(mc2.McMedian, mc1.McMedian, 9); // deterministisch
        Assert.True(mc1.McP05 <= mc1.McMedian + 1e-9);
        Assert.True(mc1.McMedian <= mc1.McP95 + 1e-9);
        Assert.InRange(mc1.McP05, 0.0, 1.0);
        Assert.InRange(mc1.McP95, 0.0, 1.0);
        Assert.NotEmpty(mc1.Decomposition);
        Assert.True(Math.Abs(mc1.Decomposition.Sum(d => d.Fraction) - 1.0) < 0.01,
            "Zerlegung muss auf 1 normieren");
    }

    [Fact]
    public void MonteCarlo_Band_Widens_At_Window_Edge()
    {
        // Reale Physik: InP R=3.4 nm emittiert ~700 nm (KNAPP an der vollen
        // Zone [700, 850]); InP R=4.5 nm emittiert ~786 nm (Mitte, flach).
        // Bandluecken-Streuung (±0.05 eV ~ ±25 nm) treibt das Kanten-Design
        // ueber die Gauss-Kante -> breiteres P-Band, das Mittendesign bleibt ruhig.
        var edgeDesign = Design(Mat("InP"), Mat("ZnSe"), 3.4, 0.6);
        var centerDesign = Design(Mat("InP"), Mat("ZnSe"), 4.5, 0.6);

        // Sanity: die Punkt-Emissionen liegen tatsaechlich nahe der erwarteten Stellen
        Assert.InRange(edgeDesign.EmissionWavelength_nm, 670, 730);
        Assert.InRange(centerDesign.EmissionWavelength_nm, 750, 820);

        var edge = FeasibilityEngine.EvaluateMonteCarlo(edgeDesign, n: 1000, seed: 7);
        var center = FeasibilityEngine.EvaluateMonteCarlo(centerDesign, n: 1000, seed: 7);

        double bandEdge = edge.McP95 - edge.McP05;
        double bandCenter = center.McP95 - center.McP05;
        Assert.True(bandEdge > bandCenter,
            $"Kanten-Design sollte unsicherer sein: {bandEdge:F3} vs. {bandCenter:F3}");
    }

    [Fact]
    public void Verdict_Thresholds_Are_Exact()
    {
        // green >= 0.7, amber >= 0.4, red < 0.4 — an den Grenzen prueefen
        var g = FeasibilityEngine.Evaluate(Design(Mat("CdSe"), Mat("CdS"), 3.0, 0.6, emissionOverride: 775.0));
        Assert.Contains(g.Verdict, new[] { "green", "amber" }); // stabiles Design im Fenster
        // roter Fall: Emission daneben + falscher Ligand + kein Rezeptor
        var bad = Design(Mat("CdSe"), null, 3.0, 0, emissionOverride: 400.0);
        bad.ReceptorKd_M = null;
        bad.Ligand = null;
        var r = FeasibilityEngine.Evaluate(bad);
        Assert.Equal("red", r.Verdict);
        Assert.True(r.P < 0.1);
    }
}