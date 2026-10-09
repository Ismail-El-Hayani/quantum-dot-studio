namespace QuantumDotStudio.Core.Models;

/// <summary>
/// Anwendungs-Template: definiert Ziel-Emissionsfenster, Medium und die
/// Gewichte der sechs Machbarkeitsfaktoren (Roadmap 3.1). Liegt als
/// Data/feasibility_weights.json vor und ist damit ohne Codeaenderung
/// tunbar (AC-004-Muster).
/// </summary>
public class ApplicationTemplate
{
    public string ApplicationId { get; set; } = "";
    public string DisplayName { get; set; } = "";
    /// <summary>Ziel-Emissionsfenster: Minimum in nm (z.B. 650 fuer NIR-I).</summary>
    public double EmissionWindowMin_nm { get; set; }
    /// <summary>Ziel-Emissionsfenster: Maximum in nm (z.B. 900 fuer NIR-I).</summary>
    public double EmissionWindowMax_nm { get; set; }
    /// <summary>Medium des Einsatzes: "aqueous" oder "organic".</summary>
    public string Medium { get; set; } = "aqueous";
    /// <summary>Relevanter Konzentrationsbereich (Anzahl Dekaden, >= 1 empfohlen).</summary>
    public double ConcentrationSpanDecades { get; set; } = 1.0;

    // Gewichte der sechs Faktoren (Roadmap 3.1, Summe ~1; Engine normiert defensiv)
    public double WeightEmissionWindow { get; set; } = 0.25;
    public double WeightStrain { get; set; } = 0.20;
    public double WeightQuantumYield { get; set; } = 0.20;
    public double WeightTransduction { get; set; } = 0.15;
    public double WeightStability { get; set; } = 0.10;
    public double WeightBioconjugation { get; set; } = 0.10;
}

/// <summary>
/// Eingabe der Machbarkeits-Engine: ein vollstaendiger Sensor-Entwurf.
/// Reine Daten — die Engine ist eine reine Funktion darauf (Roadmap-Regel 1).
/// </summary>
public class SensorDesign
{
    public Material CoreMaterial { get; set; } = new();
    /// <summary>Shell-Material oder null fuer homogenen Dot.</summary>
    public Material? ShellMaterial { get; set; }
    public double CoreRadius_nm { get; set; } = 3.0;
    public double ShellThickness_nm { get; set; }
    /// <summary>Graduierte Grenzflaeche (entlastet Verspannung, Roadmap 3.1 Faktor 2).</summary>
    public bool GradedInterface { get; set; }

    /// <summary>Punkt-Schaetzung der Emissionswellenlaenge aus der echten Simulation (nm).</summary>
    public double EmissionWavelength_nm { get; set; }

    public Ligand? Ligand { get; set; }
    public Analyte? Analyte { get; set; }

    /// <summary>
    /// Stoesrender Nebenanalyt (Roadmap 4, Faktor 4b): gesetzt, wenn ein
    /// realistischer Interferent (z.B. Hg2+ fuer einen Pb2+-Quenching-Sensor)
    /// mitbewertet werden soll. Null = Selektivitaet unbewertet.
    /// </summary>
    public Analyte? Interferent { get; set; }
    /// <summary>Realistische Konzentration des Interferenten (M).</summary>
    public double InterferentConcentration_M { get; set; }
    public ApplicationTemplate Application { get; set; } = new();

    /// <summary>Ziel-Konzentration des Analyten (M bzw. Aktivitaet).</summary>
    public double TargetConcentration_M { get; set; } = 1e-6;
    /// <summary>Rezeptor-Analyt-K_D (M) oder null fuer Direktdetektion.</summary>
    public double? ReceptorKd_M { get; set; }

    /// <summary>FRET: Donor-Akzeptor-Abstand ungebunden (nm).</summary>
    public double FretDistanceUnbound_nm { get; set; }
    /// <summary>FRET: Donor-Akzeptor-Abstand analytgebunden (nm).</summary>
    public double FretDistanceBound_nm { get; set; }
}

/// <summary>
/// Ergebnis eines einzelnen Machbarkeitsfaktors: Score in [0,1], Gewicht,
/// einzeiliger Verbesserungsvorschlag (Roadmap 3.3).
/// </summary>
public class FeasibilityFactorResult
{
    public string Name { get; set; } = "";
    public double Score { get; set; }
    public double Weight { get; set; }
    public string Suggestion { get; set; } = "";
    /// <summary>Score in Prozent (0-100) fuer ProgressBar-Bindings.</summary>
    public double ScorePercent => Math.Clamp(Score, 0, 1) * 100.0;
}

/// <summary>
/// Beitrag eines unsicheren Materialparameters zur Gesamtvarianz
/// (Monte-Carlo-Varianzzerlegung, Roadmap 3.2).
/// </summary>
public class VarianceContribution
{
    public string Input { get; set; } = "";
    /// <summary>Normierter Beitrag (0-1, alle Beitraege summieren zu 1).</summary>
    public double Fraction { get; set; }
    public double FractionPercent => Math.Clamp(Fraction, 0, 1) * 100.0;
}

/// <summary>
/// Gesamtergebnis der Machbarkeits-Engine: Punkt-Schaetzung P, Ampel-Verdikt,
/// Faktor-Tabelle und (optional) Monte-Carlo-Band mit Varianzzerlegung.
/// </summary>
public class FeasibilityResult
{
    /// <summary>Punkt-Schaetzung der Erfolgswahrscheinlichkeit (0-1, gewichtetes geometrisches Mittel).</summary>
    public double P { get; set; }
    /// <summary>Verdikt: "green" (P >= 0.7), "amber" (0.4 <= P < 0.7), "red" (P < 0.4).</summary>
    public string Verdict { get; set; } = "red";
    public List<FeasibilityFactorResult> Factors { get; set; } = new();

    // Monte-Carlo (nur gesetzt, wenn EvaluateMonteCarlo gelaufen ist)
    public bool HasMonteCarlo { get; set; }

    /// <summary>Anzahl der Monte-Carlo-Stichproben (nur informativ, 0 ohne MC).</summary>
    public int McSampleCount { get; set; }
    public double McMedian { get; set; }
    public double McP05 { get; set; }
    public double McP95 { get; set; }
    public List<VarianceContribution> Decomposition { get; set; } = new();
}

/// <summary>
/// Unsicherheits-Priors der Materialparameter fuer die Monte-Carlo-Simulation
/// (Roadmap 3.2: ±0.05 eV Bandluecke, ±10 % Massen, ±0.05 Å Gitterkonstante).
/// </summary>
public static class UncertaintyPriors
{
    public const double BandGap_eV = 0.05;
    public const double MassRelative = 0.10;
    public const double LatticeConstant_A = 0.05;
}