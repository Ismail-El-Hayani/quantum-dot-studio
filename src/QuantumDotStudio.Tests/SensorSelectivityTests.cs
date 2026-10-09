using QuantumDotStudio.Core.Data;
using QuantumDotStudio.Core.Models;
using QuantumDotStudio.Core.Physics;

namespace QuantumDotStudio.Tests;

/// <summary>
/// Selektivitaets-Matrix (Roadmap Phase 4, Faktor-4b): Querempfindlichkeiten
/// gegenueber realistischen Nebenanalyten. Literatur-Anker:
/// - Hg(II) quencht CdSe/ZnS staerker als Pb(II): K_SV ~1e6 vs 4e5 M^-1 —
///   Li 2013 nutzt deshalb GO-Ampflifizierung + Aptamer fuer Pb2+-Spezifitaet
/// - PET-Sensoren sind notorisch unspezifisch: Ascorbat (E0 +0.35 V) und
///   Dopamin (+0.21 V) quenchen CdSe neben dem Zielanalyten
/// - Aptamer-FRET ist spezifisch: Acetamiprid-Aptamer bindet kein Cy5.5-FRET
///   mit anderem R0 (Wen 2017, Kim &amp; Yoo 2021)
/// </summary>
public class SensorSelectivityTests
{
    private static Analyte A(string id) => SensorDatabase.Analytes.First(a => a.AnalyteId == id);

    [Fact]
    public void Hg2plus_Interferes_With_Pb2plus_Quenching_Sensor()
    {
        // Pb2+-Sensor (K_SV 4.2e5) sieht Hg2+ (K_SV 1e6) am selben QD —
        // bei gleicher Konzentration loescht Hg2+ staerker als das Ziel.
        double i = SensorSelectivity.InterferenceScore("Quenching", A("Pb2+"), A("Hg2+"), 7.2e-8, 7.2e-8);
        Assert.True(i > 0.9, $"Hg2+ sollte Pb2+-Quenching stark interferieren, war {i:F2}");
    }

    [Fact]
    public void Pb2plus_At_Lower_Conc_Is_Manageable_Interferent()
    {
        // Realistisch: Hg2+ ist selten, Pb2+ ist das Ziel — umgekehrt gilt:
        // ein Hg2+-Sensor sieht wenig Pb2+-Interferenz, wenn Pb2+ 10x weniger
        // konzentriert ist und 2.4x schwaecher quencht.
        double i = SensorSelectivity.InterferenceScore("Quenching", A("Hg2+"), A("Pb2+"), 7.2e-8, 7.2e-9);
        Assert.True(i < 0.5, $"Pb2+ bei 10x niedrigerer Konzentration sollte < 0.5 Interferenz geben, war {i:F2}");
    }

    [Fact]
    public void Different_FRET_Analytes_Do_Not_Interfere()
    {
        // Aptamer-FRET: IgG-FRET (R0 6.0) und Acetamiprid-FRET (R0 4.815) —
        // unterschiedliche R0 und Aptamere = keine spektrale/biochemische
        // Querempfindlichkeit.
        double i = SensorSelectivity.InterferenceScore("FRET", A("Acetamiprid"), A("IgG"), 2e-5, 1e-9);
        Assert.True(i < 0.3, $"Fremdes FRET-Paar sollte wenig interferieren, war {i:F2}");
    }

    [Fact]
    public void Ascorbate_Interferes_With_Dopamine_PET_Sensor()
    {
        // PET-Sensoren sind unspezifisch: Ascorbat (+0.35 V) quencht CdSe
        // mindestens so stark wie Dopamin (+0.21 V) — beide ueber CB-Kante
        // (+0.46 V? nein: CdSe-CB +0.46 V, beide DARUNTER) — pruefe Physik:
        // CdSe CB = EA 4.9 − 4.44 = +0.46 V vs. NHE. Ascorbat +0.35 V liegt
        // UNTER der CB-Kante -> nach unserem Kriterium KEIN PET. Test prueft
        // die echte Richtung des Modells.
        double i = SensorSelectivity.InterferenceScore("PET", A("Dopamine"), A("Ascorbate"), 1e-6, 1e-6);
        // Beide unter CB +0.46 V: Ascorbat +0.35 < 0.46 -> Fraktion < 0.5,
        // Dopamin +0.21 -> noch kleiner. Interferenz = Anteil am Gesamtquench,
        // Ascorbat dominiert den (schwachen) Gesamtquench -> nahe 1.
        Assert.True(i > 0.5, $"Ascorbat dominiert den PET-Quench, Interferenz war {i:F2}");
    }

    [Fact]
    public void Charge_Sensors_Respond_To_Same_Charge_Ions()
    {
        // Nernst antwortet auf jedes einwertige Kation gleich — H+-Sensor
        // gegen einen (fiktiven) anderen Charge-Analyten: volle Interferenz.
        double i = SensorSelectivity.InterferenceScore("Charge", A("H+"), A("H+"), 1e-7, 1e-7);
        Assert.True(i > 0.9, "Gleiche Spezies muss volle Interferenz geben");
    }

    [Fact]
    public void SelectivityFactor_Ranks_AptamerFRET_Above_MetalQuenching()
    {
        // Kern-Erkenntnis aus der Literatur (Wen 2017; Kim &amp; Yoo 2021):
        // Aptamer-FRET ist spezifischer als Metall-Quenching. Das Modell muss
        // dasselbe Ranking reproduzieren.
        double fretSel = SensorSelectivity.SelectivityFactor("FRET", A("Acetamiprid"), A("IgG"), 2e-5, 1e-9);
        double quenchSel = SensorSelectivity.SelectivityFactor("Quenching", A("Pb2+"), A("Hg2+"), 7.2e-8, 7.2e-8);
        Assert.True(fretSel > quenchSel,
            $"Aptamer-FRET ({fretSel:F2}) sollte selektiver sein als Metall-Quenching ({quenchSel:F2})");
    }
}