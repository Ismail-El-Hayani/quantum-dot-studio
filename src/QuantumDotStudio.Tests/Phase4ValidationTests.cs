using QuantumDotStudio.Core.Data;
using QuantumDotStudio.Core.Models;
using QuantumDotStudio.Solver;

namespace QuantumDotStudio.Tests;

/// <summary>
/// Phase-4-Exit-Kriterium (Roadmap): >= 3 Vorlagen, deren vorhergesagtes P die
/// literaturvalidierten Designs UEBER bekannten Fehlschlag-Designs rankt,
/// plus die ±20-%-Emissionsvalidierung gegen publizierte Konstrukte
/// (NFR-002-Stil).
///
/// Literaturquellen (Forschungspraktikum-Bibliothek, alle PDFs lokal):
/// - Xiang &amp; Tang, RSC Adv. 7, 8332 (2017): CdTe-FRET-Aptasensor Acetamiprid
///   (R0 = 4.815 nm, r = 5.134 nm, Kd = 0.58 mM, LOD 0.02 mM, Recovery 98-103 %)
/// - Li et al., Biosens. Bioelectron. 43, 69 (2013): CdSe/ZnS + GO + Aptamer,
///   Pb2+ turn-on, LOD 90 pM; EPA action level 15 µg/L ~ 72 nM
/// - Daramola et al., J. Fluoresc. 30, 557 (2020): dual-capped CdTe (MPA/TGA), pH 3.3-8
/// - Chen et al. (Si-QD-Kapitel): GOx -> H2O2 turn-off, LOD 0.35-0.97 mM
/// - Goldman et al., Anal. Chem. 74, 841 (2002): CdSe/ZnS-IgG-Fluoroimmunoassay
/// - Han et al., Nat. Biotechnol. 19, 631 (2001): 10 ZnS-capped CdSe-Farben 443-655 nm
/// - Aldana et al., JACS 123, 8844 (2001): nackte Thiol-CdSe zerfallen in Wasser
/// - Dhamo et al., Sci. Rep. 12, 22000 (2022): zu dicke ZnS-Schale -> Versetzungen
/// </summary>
public class Phase4ValidationTests
{
    // ------------------------------------------------ Hilfsmittel

    private static SensorDesign ResolveTemplate(string id)
    {
        var t = SensorTemplateDatabase.Templates.First(t => t.TemplateId == id);
        return SensorTemplateResolver.Resolve(t);
    }

    /// <summary>
    /// Fehlschlag-Variante: gleiches Template, aber mit einem bekannten
    /// Konstruktionsfehler aus der Literatur.
    /// </summary>
    private static SensorDesign BrokenVariant(SensorDesign good, Action<SensorDesign> breakIt)
    {
        var d = new SensorDesign
        {
            CoreMaterial = good.CoreMaterial,
            ShellMaterial = good.ShellMaterial,
            CoreRadius_nm = good.CoreRadius_nm,
            ShellThickness_nm = good.ShellThickness_nm,
            GradedInterface = good.GradedInterface,
            EmissionWavelength_nm = good.EmissionWavelength_nm,
            Ligand = good.Ligand,
            Analyte = good.Analyte,
            Application = good.Application,
            TargetConcentration_M = good.TargetConcentration_M,
            ReceptorKd_M = good.ReceptorKd_M,
            FretDistanceUnbound_nm = good.FretDistanceUnbound_nm,
            FretDistanceBound_nm = good.FretDistanceBound_nm
        };
        breakIt(d);
        return d;
    }

    // ------------------------------------------------ 1. Ranking-Exit-Kriterium

    [Fact]
    public void Templates_Load_From_Json_With_References()
    {
        // >= 3 Templates gefordert; jede mit Literaturquelle (Roadmap-Regel 3)
        Assert.True(SensorTemplateDatabase.Templates.Count >= 3,
            $"Erwartet >= 3 Templates, gefunden {SensorTemplateDatabase.Templates.Count}");
        Assert.All(SensorTemplateDatabase.Templates, t =>
        {
            Assert.False(string.IsNullOrWhiteSpace(t.Reference), $"Template {t.TemplateId} ohne Literaturquelle");
            Assert.False(string.IsNullOrWhiteSpace(t.CoreMaterial));
            Assert.False(string.IsNullOrWhiteSpace(t.LigandId));
            Assert.False(string.IsNullOrWhiteSpace(t.AnalyteId));
        });
        Assert.NotNull(SensorTemplateDatabase.LoadedFrom);
        Assert.True(File.Exists(SensorTemplateDatabase.LoadedFrom!));
    }

    [Fact]
    public void Rank_Acetamiprid_Literature_Above_BrokenDistance()
    {
        // Literatur: r_unbound = 5.134 nm (Xiang &amp; Tang messen r = 5.134 nm am
        // hybridisierten Paar). Fehlschlag: Abstandsdifferenz kollabiert
        // (gebunden ~ ungebunden) -> kein messbarer FRET-Kontrast.
        var good = ResolveTemplate("fret-acetamiprid");
        Assert.True(FeasibilityEngine.Evaluate(good).P > 0.4,
            "Literaturdesign (LOD 0.02 mM, Recovery 98-103 %) sollte mindestens amber sein");

        var bad = BrokenVariant(good, d => d.FretDistanceBound_nm = d.FretDistanceUnbound_nm + 0.1);
        Assert.True(FeasibilityEngine.Evaluate(good).P > FeasibilityEngine.Evaluate(bad).P,
            "FRET-Kontrast-Design muss ueber dem kollabierten Abstand rangieren");
    }

    [Fact]
    public void Interferent_Lowers_Pb2plus_Feasibility()
    {
        // Faktor 4b: Hg2+ ist der klassische Stoeranalyt fuer Pb2+-Quenching
        // (K_SV 1e6 vs 4.2e5). Der Engine-Abschlag muss P senken.
        var design = ResolveTemplate("quenching-pb2plus");
        double pClean = FeasibilityEngine.Evaluate(design).P;

        design.Interferent = SensorDatabase.Analytes.First(a => a.AnalyteId == "Hg2+");
        design.InterferentConcentration_M = 7.2e-8; // gleiche Konzentration wie das Ziel
        double pInterfered = FeasibilityEngine.Evaluate(design).P;

        Assert.True(pInterfered < pClean,
            $"Hg2+-Interferenz muss P senken ({pInterfered:F2} vs {pClean:F2})");
        // Und der Faktor-4-Score faellt sichtbar:
        var f4Clean = FeasibilityEngine.Evaluate(ResolveTemplate("quenching-pb2plus"))
            .Factors.First(f => f.Name == "Transduktion").Score;
        Assert.True(f4Clean > 0.2, "Der Interferenz-Abschlag sollte von einer Basis > 0.2 ausgehen");
    }

    [Fact]
    public void Rank_Pb2plus_Literature_Above_BareCore()
    {
        // Literatur: CdSe/ZnS-MPA (Li 2013). Fehlschlag: nackter CdSe-Core mit
        // Thiol-Liganden in Wasser — Aldana 2001: photochemischer Zerfall.
        var good = ResolveTemplate("quenching-pb2plus");
        Assert.True(FeasibilityEngine.Evaluate(good).P > 0.4,
            "CdSe/ZnS-MPA (Li 2013, LOD 90 pM) sollte mindestens amber sein");

        var bad = BrokenVariant(good, d =>
        {
            d.ShellMaterial = null;
            d.ShellThickness_nm = 0.0;
        });
        Assert.True(FeasibilityEngine.Evaluate(good).P > FeasibilityEngine.Evaluate(bad).P,
            "ZnS-Schale muss ueber nacktem CdSe in Wasser rangieren (Aldana 2001)");
    }

    [Fact]
    public void Rank_Glucose_Literature_Above_ThickRelaxedShell()
    {
        // Literatur: CdSe/ZnS-GOx turn-off (Chen, LOD 0.35-0.97 mM). Fehlschlag:
        // 3.5-nm-ZnS-Schale >> t_krit ~ 1.4 nm -> relaxiert, Versetzungen
        // killen QY (Dhamo 2022).
        var good = ResolveTemplate("pet-glucose");
        Assert.True(FeasibilityEngine.Evaluate(good).P > 0.4,
            "Literaturdesign sollte mindestens amber sein");

        var bad = BrokenVariant(good, d => d.ShellThickness_nm = 3.5);
        Assert.True(FeasibilityEngine.Evaluate(good).P > FeasibilityEngine.Evaluate(bad).P,
            "Kohärente Duennschale muss ueber relaxierter Dickenschale rangieren (Dhamo 2022)");
    }

    // ------------------------------------------------ 2. Emission ±20 % (NFR-002)

    [Fact]
    public void Brus_Emission_Matches_Han2001_CdSe_Color_Series_Within_20Percent()
    {
        // Han et al. 2001 (zitiert in Wen 2017, Fig. 1b): 10 ZnS-capped CdSe-
        // Emissionsmaxima 443, 473, 481, 500, 518, 543, 565, 587, 610, 655 nm.
        // Brus-Gleichung invertieren: R(lambda) — vergleiche gegen die 3 nm-
        // Referenz (598 nm) mit ±20 % Toleranz im sichtbaren Kernbereich.
        var cdse = MaterialDatabase.Defaults.First(m => m.Name == "CdSe");

        // Unser Modell: 3 nm -> 598 nm (Yu et al. 2003-Anker, bereits getestet).
        double r3 = QuantumSolver.WavelengthFromBandGap(QuantumSolver.BrusBandGap(cdse, 3.0));
        Assert.InRange(r3, 598 * 0.8, 598 * 1.2);

        // Monotonie ueber die Serie: groesserer Radius -> roetere Emission.
        // 655-nm-Punkt: R ~ 4.5-5 nm in Han 2001 — groesseres R muss roeter sein.
        double r45 = QuantumSolver.WavelengthFromBandGap(QuantumSolver.BrusBandGap(cdse, 4.5));
        Assert.True(r45 > r3, "4.5-nm-CdSe muss roeter emittieren als 3-nm-CdSe");
        // 655 nm ~ Han-2001-Serie-Endpunkt; ±20 % -> [524, 786]
        Assert.InRange(r45, 655 * 0.8, 655 * 1.2);
    }

    [Fact]
    public void Brus_Emission_CdTe_Matches_XiangTang650nm_Within_20Percent()
    {
        // Xiang &amp; Tang 2017: wasserloesliche CdTe QDs, Emissionsmaximum 650 nm.
        // Das Template nutzt R = 2.5 nm — Brus muss 650 ± 20 % treffen.
        var cdte = MaterialDatabase.Defaults.First(m => m.Name == "CdTe");
        double lambda = QuantumSolver.WavelengthFromBandGap(QuantumSolver.BrusBandGap(cdte, 2.5));
        Assert.InRange(lambda, 650 * 0.8, 650 * 1.2);
    }

    // ------------------------------------------------ 3. Sensor-Physik-Anker

    [Fact]
    public void Fret_R0_XiangTang_Is_Detectable()
    {
        // R0 = 4.815 nm, r_unbound = 5.134 nm (Paper): E = 1/(1+(5.134/4.815)^6) ~ 0.41.
        // Der Sensor funktioniert nachweislich (LOD 0.02 mM, Recovery 98-103 %) —
        // E ~ 0.4 am Arbeitspunkt ist der Literaturzustand, nicht 0.5+.
        var design = ResolveTemplate("fret-acetamiprid");
        double e = Core.Physics.SensorPhysics.FretEfficiency(design.FretDistanceUnbound_nm, 4.815);
        Assert.InRange(e, 0.30, 0.50); // Paper-Arbeitspunkt
        // Und das Paper: Target-Bindung verringert E -> detektierbarer Kontrast
        double eBound = Core.Physics.SensorPhysics.FretEfficiency(design.FretDistanceBound_nm, 4.815);
        Assert.True(eBound < e, "Bindung verringert FRET (Turn-off), Kontrast detektierbar");
    }

    [Fact]
    public void Pb2plus_At_EPA_Level_Is_Detectable_Via_SternVolmer()
    {
        // EPA action level 72 nM; K_SV(Pb2+) = 4.2e5 M^-1 (CdSe/ZnS-MPA, typisch).
        // I/I0 = 1/(1 + K_SV · c) — Verlust bei 72 nM muss >= 1 % sein (klar messbar).
        double loss = 1.0 - Core.Physics.SensorPhysics.SternVolmerIntensity(7.2e-8, 4.2e5);
        Assert.True(loss > 0.01, $"Signalverlust bei EPA-Level sollte > 1 % sein, war {loss * 100:F2} %");
    }

    [Fact]
    public void Glucose_At_BloodLevel_Is_Strongly_PetQuenched()
    {
        // Blutglukose 5 mM; H2O2 entsteht stoechiometrisch (GOx).
        // E0(H2O2/H2O) = +1.776 V >> CdSe-CB (+0.46 V): PET immer an.
        double frac = Core.Physics.SensorPhysics.PetQuenchFraction(1.776, 4.9);
        Assert.True(frac > 0.99, $"PET-Fraktion fuer H2O2 an CdSe sollte ~1 sein, war {frac:F3}");
    }

    [Fact]
    public void pH_Sensor_Nernst_Response_Is_Physiologically_Resolved()
    {
        // pH 6.0 -> 7.4 (Daramola 2020: physiologischer Bereich):
        // Delta-psi = 59.16 mV * (7.4 - 6.0) ~ 83 mV — gut aufloesbar.
        double psi6 = Core.Physics.SensorPhysics.NernstPotential_mV(1e-6);
        double psi74 = SensorPhysicsNernstAtPh(7.4);
        double delta = Math.Abs(psi74 - psi6);
        Assert.True(delta > 40, $"pH-Spanne 6.0-7.4 sollte > 40 mV Differenz geben, war {delta:F0} mV");
    }

    /// <summary>Nernst-Potential bei gegebenem pH (Aktivitaet = 10^-pH).</summary>
    private static double SensorPhysicsNernstAtPh(double pH)
        => Core.Physics.SensorPhysics.NernstPotential_mV(Math.Pow(10, -pH));
}