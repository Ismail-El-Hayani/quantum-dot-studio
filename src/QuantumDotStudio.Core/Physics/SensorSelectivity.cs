using QuantumDotStudio.Core.Models;

namespace QuantumDotStudio.Core.Physics;

/// <summary>
/// Selektivitaet (Roadmap Phase 4, Faktor-4b): Antwort des gewaehlten Sensors
/// auf Nebenanalyten (Interferenz). Jede Funktion ist rein und unit-testbar
/// (Roadmap-Regel 1).
///
/// Physikalische Modelle pro Modus:
/// - Quenching: Nebenanalyt loescht nach demselben Stern-Volmer-Gesetz, aber
///   mit eigener K_SV. Selektivitaet = Ziel-Verlust / (Ziel-Verlust + Inter-
///   ferent-Verlust am jeweiligen Level). Hg(II) quencht staerker als Pb(II)
///   (K_SV ~1e6 vs 4e5 M^-1) — klassische Querempfindlichkeit von Metall-
///   Quenching-Sensoren (Wen 2017, Kap. 2).
/// - FRET: Interferenz nur bei spektraler Ueberlappung + Bindung — modelliert
///   als relativer FRET-Kontrast des Interferenten am selben Donor.
/// - Charge: Nernst ist ionenspezifisch nur ueber die Ladung z — gleiche
///   Ladung = gleiche Antwort (pH-Sensoren sprechen auf alle protonierten
///   Spezies an).
/// - PET: Redoxpotential des Interferenten entscheidet — jeder Quencher mit
///   E0 ueber der CB-Kante loescht ebenfalls (Ascorbat, Dopamin, H2O2 alle
///   PET-Quencher fuer CdSe, deshalb sind PET-Sensoren notorisch unspezifisch).
/// </summary>
public static class SensorSelectivity
{
    /// <summary>
    /// Interferenz-Score eines Nebenanalyten am Design: 1 = antwortet genausoviel
    /// wie das Ziel (voellig unspezifisch), 0 = keine Antwort. Gewichtet mit
    /// dem realistischen Konzentrationsverhaeltnis Interferent/Ziel.
    /// </summary>
    public static double InterferenceScore(string mode, Analyte target, Analyte interferent,
        double targetConcentration_M, double interferentConcentration_M)
    {
        ArgumentNullException.ThrowIfNull(target);
        ArgumentNullException.ThrowIfNull(interferent);

        if (interferentConcentration_M <= 0)
            return 0.0;

        return mode switch
        {
            "Quenching" => QuenchingInterference(target, interferent, targetConcentration_M, interferentConcentration_M),
            "PET" => PetInterference(target, interferent, targetConcentration_M, interferentConcentration_M),
            "Charge" => ChargeInterference(target, interferent),
            "FRET" => FretInterference(target, interferent),
            _ => 0.0
        };
    }

    /// <summary>
    /// Selektivitaets-Faktor in [0,1]: 1 = perfekt selektiv (kein Interferent
    /// antwortet), 0 = jeder Interferent ueberdeckt das Signal. Geometrisches
    /// Mittel ueber (1 - Interferenz) — ein starker Einzelinterferent dominiert.
    /// </summary>
    public static double SelectivityFactor(string mode, Analyte target, Analyte interferent,
        double targetConcentration_M, double interferentConcentration_M)
    {
        double interference = InterferenceScore(mode, target, interferent, targetConcentration_M, interferentConcentration_M);
        return Math.Clamp(1.0 - interference, 0.0, 1.0);
    }

    // ------------------------------------------------------------ pro Modus

    private static double QuenchingInterference(Analyte target, Analyte interferent,
        double cTarget, double cInterferent)
    {
        // Beide loeschen am selben QD nach Stern-Volmer mit ihren K_SV.
        if (target.SternVolmerConstant_M <= 0)
            return 0.0;

        double lossTarget = 1.0 - SensorPhysics.SternVolmerIntensity(cTarget, target.SternVolmerConstant_M);
        double lossInterferent = interferent.SternVolmerConstant_M <= 0
            ? 0.0
            : 1.0 - SensorPhysics.SternVolmerIntensity(cInterferent, interferent.SternVolmerConstant_M);

        if (lossTarget + lossInterferent <= 0)
            return 0.0;

        double fraction = lossInterferent / (lossTarget + lossInterferent);
        return Math.Clamp(fraction * 2.0, 0.0, 1.0);
    }

    private static double PetInterference(Analyte target, Analyte interferent,
        double cTarget, double cInterferent)
    {
        // PET: jeder Quencher ueber der CB-Kante loescht — die Staerke ist
        // die Quench-Fraktion; Interferenz = Anteil am Gesamtquench.
        if (target.RedoxPotential_V == 0)
            return 0.0;

        // Gleiche CB-Kante angenommen (gleicher QD): nur Redoxpotential zaehlt.
        double qTarget = SensorPhysics.PetQuenchFraction(target.RedoxPotential_V, 4.9); // CdSe-Beispiel-EA
        double qInterferent = interferent.RedoxPotential_V == 0
            ? 0.0
            : SensorPhysics.PetQuenchFraction(interferent.RedoxPotential_V, 4.9);

        if (qTarget + qInterferent <= 0)
            return 0.0;

        double fraction = qInterferent / (qTarget + qInterferent);
        return Math.Clamp(fraction * 2.0, 0.0, 1.0);
    }

    private static double ChargeInterference(Analyte target, Analyte interferent)
    {
        // Nernst: alle Ionen gleicher Ladung z erzeugen dasselbe Potential —
        // ein pH-Sensor (H+) sieht nur Protonen, aber ein Cd2+-Sensor sieht
        // auch Mg2+/Ca2+. Modell: volle Interferenz nur bei identischem Modus.
        return interferent.Mode == "Charge" ? 1.0 : 0.0;
    }

    private static double FretInterference(Analyte target, Analyte interferent)
    {
        // FRET: Interferenz benoetigt spektrale Ueberlappung UND Bindung —
        // Aptamer-Sensoren sind hier spezifisch. Modell: Interferenz nur,
        // wenn der Interferent ebenfalls ein FRET-Akzeptor ist (gleiches R0).
        if (interferent.Mode != "FRET")
            return 0.0;

        // Aehnliche Foerster-Radiusen (±20 %) bedeuten spektrale Ueberlappung.
        double r0Target = target.ForsterRadius_nm;
        double r0Interferent = interferent.ForsterRadius_nm;
        if (r0Target <= 0 || r0Interferent <= 0)
            return 0.0;

        double relDiff = Math.Abs(r0Interferent - r0Target) / r0Target;
        return relDiff < 0.2 ? 0.5 : relDiff < 0.5 ? 0.2 : 0.0;
    }
}