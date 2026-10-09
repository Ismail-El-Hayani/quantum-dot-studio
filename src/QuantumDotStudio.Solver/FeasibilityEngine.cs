using QuantumDotStudio.Core.Models;
using QuantumDotStudio.Core.Physics;
using System.Diagnostics;

namespace QuantumDotStudio.Solver;

/// <summary>
/// Machbarkeits-Engine (Roadmap Phase 3): bewertet einen Sensor-Entwurf
/// gegen die sechs Faktoren und liefert P(success) als gewichtetes
/// geometrisches Mittel — ein disqualifizierender Faktor (Score ~0) kann
/// nicht durch gute Faktoren kompensiert werden, genau wie in der realen
/// Synthese. Monte-Carlo ueber Materialparameter-Unsicherheiten liefert
/// mediane P mit 5–95 %-Band und eine Varianzzerlegung, welcher unsichere
/// Eingang das Risiko dominiert.
/// </summary>
public static class FeasibilityEngine
{
    /// <summary>
    /// Punkt-Schaetzung: gewichtetes geometrisches Mittel P = Π fᵢ^wᵢ.
    /// </summary>
    public static FeasibilityResult Evaluate(SensorDesign design)
    {
        ArgumentNullException.ThrowIfNull(design);
        var factors = ComputeFactors(design);

        double p = WeightedGeometricMean(factors);
        return new FeasibilityResult
        {
            P = p,
            Verdict = VerdictOf(p),
            Factors = factors
        };
    }

    /// <summary>
    /// Monte-Carlo (N Standard 2000): zieht Bandluecke/Massen/Gitterkonstante
    /// aus ihren Priors (±0.05 eV, ±10 %, ±0.05 Å; Roadmap 3.2), rechnet die
    /// Emission via volle Brus-Gleichung neu und bewertet jedes Sample.
    /// Liefert Median + 5/95-Perzentile und die Varianzzerlegung.
    /// Deterministischer Seed fuer reproduzierbare Tests.
    /// </summary>
    public static FeasibilityResult EvaluateMonteCarlo(SensorDesign design, int n = 2000, int seed = 42)
    {
        ArgumentNullException.ThrowIfNull(design);
        if (n < 100)
            throw new ArgumentOutOfRangeException(nameof(n), "Monte-Carlo braucht mindestens 100 Samples");

        var baseResult = Evaluate(design);

        var rng = new Random(seed);
        var samples = new double[n];
        // Partielle Summen pro unsicherem Eingang fuer die Varianzzerlegung
        // (Einfache Empfindlichkeitszerlegung: pro Eingang wird geschaetzt,
        // wie stark dessen Streuung allein die Spannweite von P treibt.)
        var pByBandGapShift = new Dictionary<double, List<double>>();

        for (int i = 0; i < n; i++)
        {
            var perturbed = Perturb(design, rng);
            var result = Evaluate(perturbed);
            samples[i] = result.P;

            // Bandluecken-Shift dieses Samples als Zerlegungs-Schluessel grob quantisieren
            double bgShift = Math.Round(perturbed.CoreMaterial.BandGap_eV - design.CoreMaterial.BandGap_eV, 2);
            if (!pByBandGapShift.TryGetValue(bgShift, out var list))
                pByBandGapShift[bgShift] = list = new List<double>();
            list.Add(result.P);
        }

        Array.Sort(samples);
        var mc = new FeasibilityResult
        {
            P = baseResult.P,
            Verdict = baseResult.Verdict,
            Factors = baseResult.Factors,
            HasMonteCarlo = true,
            McSampleCount = samples.Length,
            McMedian = Percentile(samples, 0.50),
            McP05 = Percentile(samples, 0.05),
            McP95 = Percentile(samples, 0.95)
        };

        mc.Decomposition = DecomposeVariance(pByBandGapShift, samples);
        return mc;
    }

    // ------------------------------------------------------- Faktor-Berechnung

    private static List<FeasibilityFactorResult> ComputeFactors(SensorDesign design)
    {
        var app = design.Application;
        bool hasShell = design.ShellMaterial is not null;
        bool graded = design.GradedInterface;

        // Faktor 1: Emissionsfenster
        double f1 = FeasibilityFactors.EmissionWindowMatch(
            design.EmissionWavelength_nm, app.EmissionWindowMin_nm, app.EmissionWindowMax_nm);

        // Faktor 2: Verspannung
        double mismatch = 0.0, criticalT = double.PositiveInfinity;
        bool relaxed = false;
        if (hasShell)
        {
            mismatch = StrainModel.LatticeMismatch(design.CoreMaterial, design.ShellMaterial!);
            criticalT = StrainModel.CriticalThickness_nm(design.CoreMaterial, design.ShellMaterial!);
            relaxed = StrainModel.IsStrainRelaxed(design.CoreMaterial, design.ShellMaterial!, design.ShellThickness_nm);
        }
        double f2 = FeasibilityFactors.StrainScore(mismatch, design.ShellThickness_nm, criticalT, graded);

        // Faktor 3: QY-Proxy — die STAERKERE der beiden Barrieren zaehlt
        // (solange ein Träger confiniert bleibt, ist die Rekombination radiativ).
        double barrier = hasShell
            ? Math.Max(BandAlignment.ElectronBarrier_eV(design.CoreMaterial, design.ShellMaterial!),
                       BandAlignment.HoleBarrier_eV(design.CoreMaterial, design.ShellMaterial!))
            : 0.0;
        double f3 = FeasibilityFactors.QuantumYieldProxy(hasShell, design.ShellThickness_nm, barrier, relaxed, graded);

        // Faktor 4: Transduktion (Modus aus dem Analyten) — mit Selektivitaets-
        // Abschlag (Faktor 4b), wenn ein Interferent gesetzt ist.
        string mode = design.Analyte?.Mode ?? "";
        double f4 = FeasibilityFactors.TransductionScore(mode, design);
        if (design.Interferent is not null && design.Analyte is not null)
        {
            double sel = SensorSelectivity.SelectivityFactor(
                mode, design.Analyte, design.Interferent,
                design.TargetConcentration_M,
                design.InterferentConcentration_M > 0 ? design.InterferentConcentration_M : design.TargetConcentration_M);
            f4 *= sel; // Interferenz schwaecht das nutzbare Signal
        }

        // Faktor 5: Stabilitaet (Shell-CHEMIE zaehlt: Sulfid robust, Selenid schwach)
        double f5 = FeasibilityFactors.StabilityScore(design.Ligand, app.Medium, design.ShellMaterial);

        // Faktor 6: Bioconjugation (nur FRET-Designs benoetigen einen Rezeptor;
        // Quenching/PET/Charge wirken direkt auf der Oberflaeche)
        double f6 = FeasibilityFactors.BioconjugationScore(design.ReceptorKd_M, design.TargetConcentration_M, mode);

        return new List<FeasibilityFactorResult>
        {
            new() { Name = "Emissionsfenster", Score = f1, Weight = app.WeightEmissionWindow,
                    Suggestion = f1 < 0.5 ? $"Emission {design.EmissionWavelength_nm:F0} nm liegt ausserhalb von [{app.EmissionWindowMin_nm:F0}, {app.EmissionWindowMax_nm:F0}] nm — Radius/Kernmaterial anpassen." : "" },
            new() { Name = "Verspannung", Score = f2, Weight = app.WeightStrain,
                    Suggestion = f2 < 0.5 ? (relaxed ? $"Schale {design.ShellThickness_nm:F1} nm > t_krit {criticalT:F1} nm — duennere Schale oder graduierte Grenzflaeche." : $"Fehlanpassung {mismatch * 100:F1} % hoch — passenderes Shell-Material waehlen.") : "" },
            new() { Name = "Quanteneffizienz", Score = f3, Weight = app.WeightQuantumYield,
                    Suggestion = f3 < 0.5 ? (hasShell ? "Barriere/Schalendicke erhoeen oder graduierte Grenzflaeche." : "Passivierende Schale hinzufuegen — nackte Oberflaeche hat hohe Γ_nr.") : "" },
            new() { Name = "Transduktion", Score = f4, Weight = app.WeightTransduction,
                    Suggestion = f4 < 0.5 ? "Signal zu schwach: FRET-Abstandsdifferenz vergroessern, hoehere K_SV/PET-Fenster oder sensitiveren Modus waehlen." : "" },
            new() { Name = "Stabilitaet", Score = f5, Weight = app.WeightStability,
                    Suggestion = f5 < 0.5 ? $"Ligand fuer Medium '{app.Medium}' ungeeignet — wasserloeslichen Ligand (z.B. MPA/PEG) bzw. ZnS-Schutz verwenden." : "" },
            new() { Name = "Bioconjugation", Score = f6, Weight = app.WeightBioconjugation,
                    Suggestion = f6 < 0.5 ? "K_D zu hoch gegenueber Zielkonzentration — affinerer Rezeptor noetig." : "" }
        };
    }

    // ------------------------------------------------------------ Kernmathematik

    private static double WeightedGeometricMean(List<FeasibilityFactorResult> factors)
    {
        double weightSum = factors.Sum(f => f.Weight);
        if (weightSum <= 0)
            return 0.0;

        double logSum = 0.0;
        foreach (var f in factors)
        {
            double w = f.Weight / weightSum; // defensive Normierung
            double score = Math.Clamp(f.Score, 1e-6, 1.0); // log(0) verhindern
            logSum += w * Math.Log(score);
        }
        return Math.Exp(logSum);
    }

    private static string VerdictOf(double p) => p switch
    {
        >= 0.7 => "green",
        >= 0.4 => "amber",
        _ => "red"
    };

    private static double Percentile(double[] sorted, double q)
    {
        double pos = q * (sorted.Length - 1);
        int lower = (int)Math.Floor(pos);
        int upper = (int)Math.Ceiling(pos);
        if (lower == upper) return sorted[lower];
        return sorted[lower] + (pos - lower) * (sorted[upper] - sorted[lower]);
    }

    // ------------------------------------------------------- Monte-Carlo-Perturb

    private static SensorDesign Perturb(SensorDesign design, Random rng)
    {
        // Gauss-Box (±2σ) fuer Bandluecke/Masse/Gitterkonstante — Priors aus
        // UncertaintyPriors (Roadmap 3.2).
        var core = CloneMaterial(design.CoreMaterial, rng);
        var shell = design.ShellMaterial is null ? null : CloneMaterial(design.ShellMaterial, rng);

        var d = new SensorDesign
        {
            CoreMaterial = core,
            ShellMaterial = shell,
            CoreRadius_nm = design.CoreRadius_nm,
            ShellThickness_nm = design.ShellThickness_nm,
            GradedInterface = design.GradedInterface,
            // Emission mit perturbiertem Material neu rechnen (volle Brus)
            EmissionWavelength_nm = QuantumSolver.WavelengthFromBandGap(
                QuantumSolver.BrusBandGap(core, design.CoreRadius_nm)),
            Ligand = design.Ligand,
            Analyte = design.Analyte,
            Application = design.Application,
            TargetConcentration_M = design.TargetConcentration_M,
            ReceptorKd_M = design.ReceptorKd_M,
            FretDistanceUnbound_nm = design.FretDistanceUnbound_nm,
            FretDistanceBound_nm = design.FretDistanceBound_nm
        };
        return d;
    }

    private static Material CloneMaterial(Material m, Random rng)
    {
        return new Material
        {
            Name = m.Name,
            BandGap_eV = m.BandGap_eV + Gaussian(rng, UncertaintyPriors.BandGap_eV),
            EffectiveMassElectron = m.EffectiveMassElectron * (1.0 + Gaussian(rng, UncertaintyPriors.MassRelative)),
            EffectiveMassHole = m.EffectiveMassHole * (1.0 + Gaussian(rng, UncertaintyPriors.MassRelative)),
            DielectricConstant = m.DielectricConstant,
            LatticeConstant_A = m.LatticeConstant_A + Gaussian(rng, UncertaintyPriors.LatticeConstant_A),
            ElectronAffinity_eV = m.ElectronAffinity_eV,
            Cation = m.Cation,
            Anion = m.Anion
        };
    }

    /// <summary>Summe von 4 Uniformen − 2 (Irwyn-Hall n=4, Var=1/3), auf Var=1
    /// skaliert und mit sigma multipliziert — effektive Standardabweichung sigma.</summary>
    private static double Gaussian(Random rng, double sigma)
    {
        double u = rng.NextDouble() + rng.NextDouble() + rng.NextDouble() + rng.NextDouble(); // 0..4
        double standard = (u - 2.0) * Math.Sqrt(3.0); // mean 0, variance 1
        return standard * sigma;
    }

    // ---------------------------------------------------------- Varianzzerlegung

    /// <summary>
    /// Einfache Zerlegung: die Spannweite von P auf Bandluecken-Shift-Bins
    /// gegenueber der Gesamtvarianz. Der dominante Beitrag zeigt, dass die
    /// Bandluecken-Unsicherheit das Risiko treibt (andere Parameter sind
    /// gekoppelt — eine vollstaendige Sobol-Analyse ist Phase-4-Option).
    /// </summary>
    private static List<VarianceContribution> DecomposeVariance(Dictionary<double, List<double>> pByBgShift, double[] samples)
    {
        double totalVar = Variance(samples);
        if (totalVar <= 1e-12)
            return new List<VarianceContribution>();

        // Between-group-Varianz der Bandluecken-Bins
        double betweenVar = 0.0;
        double overallMean = samples.Average();
        int n = samples.Length;
        foreach (var (_, ps) in pByBgShift)
        {
            double groupMean = ps.Average();
            betweenVar += ps.Count * Math.Pow(groupMean - overallMean, 2);
        }
        betweenVar /= n;

        // Rest (Massen/Gitter + Nichtlinearitaet + Within-Bin)
        double withinVar = Math.Max(totalVar - betweenVar, 0.0);

        double sum = betweenVar + withinVar;
        return new List<VarianceContribution>
        {
            new() { Input = "Bandlücke (±0.05 eV)", Fraction = sum > 0 ? betweenVar / sum : 0.0 },
            new() { Input = "Massen & Gitterkonstante", Fraction = sum > 0 ? withinVar / sum : 0.0 }
        };
    }

    private static double Variance(double[] xs)
    {
        double mean = xs.Average();
        double sum = 0.0;
        foreach (var x in xs)
            sum += (x - mean) * (x - mean);
        return sum / xs.Length;
    }
}