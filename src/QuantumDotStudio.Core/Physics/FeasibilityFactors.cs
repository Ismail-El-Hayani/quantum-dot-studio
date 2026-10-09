using QuantumDotStudio.Core.Models;

namespace QuantumDotStudio.Core.Physics;

/// <summary>
/// Die sechs Machbarkeitsfaktoren der Roadmap (3.1) als reine Funktionen.
/// Jeder Score liegt in [0,1]; 1 = ideal, 0 = disqualifizierend.
/// Alle Konstanten tragen ihre Quelle im Kommentar (Roadmap-Regel 3).
/// </summary>
public static class FeasibilityFactors
{
    // ------------------------------------------------- 1. Emissionsfenster

    /// <summary>
    /// Faktor 1 — Emissionsfenster-Match: weiche Gauss-Bewertung der Emissions-
    /// wellenlaenge gegen das Ziel-Fenster [min, max]. Voller Score nur in der
    /// mittleren 60 %-Zone (Fensterkanten sind physikalisch schlechter: am
    /// blauen NIR-Rand dominiert z.B. Gewebe-Autofluoreszenz); ausserhalb
    /// der vollen Zone faellt der Score gaussfoermig (Breite = 30 % der
    /// Fensterbreite). Ein 100-nm-Daneben liegt nahe 0, die Kante selbst
    /// bei ~0.65. Roadmap 3.1 Faktor 1.
    /// </summary>
    public static double EmissionWindowMatch(double lambda_nm, double windowMin_nm, double windowMax_nm)
    {
        if (lambda_nm <= 0 || windowMax_nm <= windowMin_nm)
            return 0.0;

        double span = windowMax_nm - windowMin_nm;
        double fullLo = windowMin_nm + 0.2 * span;
        double fullHi = windowMax_nm - 0.2 * span;
        double width = Math.Max(0.3 * span, 1.0);

        double distance = 0.0;
        if (lambda_nm < fullLo) distance = fullLo - lambda_nm;
        else if (lambda_nm > fullHi) distance = lambda_nm - fullHi;

        return Math.Exp(-Math.Pow(distance / width, 2));
    }

    // ---------------------------------------------- 2. Verspannung/Relax

    /// <summary>
    /// Faktor 2 — Verspannung: Score aus Gitterfehlanpassung f und
    /// Schalendicke vs. kritische Dicke t_c ≈ b/(2|f|) (Matthews–Blakeslee,
    /// b = 0.30 nm). Kohaerent (t &lt;= t_c): 1 - (|f|/f_max)^2 mit f_max = 12 %
    /// (CdSe/ZnS -10.6 % gilt als kritisch, graduierte Grenzflaeche noetig).
    /// Graduierte Grenzflaeche halbiert die effektive Fehlanpassung.
    /// Relaxiert (t &gt; t_c): harter Abzug, min(0.3, ...).
    /// Roadmap 3.1 Faktor 2.
    /// </summary>
    public static double StrainScore(double mismatch_f, double shellThickness_nm, double criticalThickness_nm, bool gradedInterface)
    {
        if (double.IsInfinity(criticalThickness_nm))
            return 1.0; // perfekte Anpassung: unbegrenzt kohärent

        double f = Math.Abs(mismatch_f);
        if (gradedInterface)
            f *= 0.5; // graduierte Grenzfläche verteilt die Verspannung

        // Pseudomorphes Wachstum scheitert oberhalb ~8 % Fehlanpassung
        // praktisch immer (CdSe/ZnS −10.6 % nur mit Gradient/buffer; InP/CdS
        // −0.63 % nahezu beliebig). Roadmap 1.3-Tabelle.
        const double fMax = 0.08;
        double coherent = 1.0 - Math.Pow(f / fMax, 2);

        if (shellThickness_nm <= criticalThickness_nm)
            return Math.Clamp(coherent, 0.0, 1.0);

        // Relaxiert: Versetzungen killen die Quanteneffizienz hart.
        double relaxed = Math.Min(0.3, coherent * 0.4);
        return Math.Clamp(relaxed, 0.0, 1.0);
    }

    // ------------------------------------------------- 3. QY-Proxy

    /// <summary>
    /// Faktor 3 — Quanteneffizienz-Proxy: QY ≈ Γ_rad/(Γ_rad + Γ_nr).
    /// Γ_nr waechst mit Relaxation (Versetzungen) und Oberflaechen-Fallen;
    /// eine Schale passiviert und senkt Γ_nr. Passivierungsstaerke bemisst
    /// sich an der STAERKEREN der beiden Barrieren (Elektron/Loch): solange
    /// ein Träger stark confiniert ist, bleibt die Rekombination radiativ —
    /// InP/ZnSe hat z.B. nur 0.1 eV Elektronen-, aber 1.25 eV Lochbarriere
    /// und erreicht real QY ~60-80 %. Modell:
    /// - Basis-QY ohne Schale: 0.25 (nackte CdSe-artige Oberflaeche)
    /// - Passivierung: +0.45 × Barrieren-Faktor × Dicken-Faktor
    /// - Relaxiert: × 0.4 (Versetzungen dominieren)
    /// - Graduierte Grenzflaeche: +0.05 zusaetzlich
    /// Roadmap 3.1 Faktor 3.
    /// </summary>
    public static double QuantumYieldProxy(bool hasShell, double shellThickness_nm, double barrier_eV, bool isRelaxed, bool gradedInterface)
    {
        double qy = 0.25; // nackte Oberfläche, Literatur-typisch für unpassivierte QDs

        if (hasShell)
        {
            // Passivierungsstaerke: Barriere begrenzt die Wirkung (0.4 eV = volle Wirkung).
            double barrierFactor = Math.Clamp(barrier_eV / 0.4, 0.0, 1.0);
            double passivation = 0.45 * barrierFactor * Math.Clamp(shellThickness_nm / 1.0, 0.0, 1.0);
            qy += passivation;
            if (gradedInterface)
                qy += 0.05;
        }

        if (isRelaxed)
            qy *= 0.4;

        return Math.Clamp(qy, 0.0, 1.0);
    }

    // ------------------------------------------------- 4. Transduktion

    /// <summary>
    /// Faktor 4 — Signal-Transduktion: Modus-abhaengig.
    /// FRET: Kontrast |ΔE|/E zwischen gebunden/ungebunden (>= 20 % = 1.0,
    /// linear darunter). Quenching: K_SV vs. Zielkonzentration — detektierbar
    /// heisst I/I0-Verlust >= 10 % am Ziel (LOD-Kriterium). Charge/PET:
    /// Signal (|ψ| bzw. Quench-Fraktion) gegen 10-%-Schwelle.
    /// Roadmap 3.1 Faktor 4.
    /// </summary>
    public static double TransductionScore(string mode, SensorDesign design)
    {
        ArgumentNullException.ThrowIfNull(design);
        var analyte = design.Analyte ?? new Analyte();

        switch (mode)
        {
            case "FRET":
            {
                double r0 = analyte.ForsterRadius_nm;
                if (r0 <= 0 || design.FretDistanceUnbound_nm <= 0)
                    return 0.0;
                double contrast = SensorPhysics.FretContrast(
                    design.FretDistanceUnbound_nm, design.FretDistanceBound_nm, r0);
                return Math.Clamp(contrast / 0.20, 0.0, 1.0); // 20 % Kontrast = voller Score
            }
            case "Quenching":
            {
                double ksv = analyte.SternVolmerConstant_M;
                if (ksv <= 0 || design.TargetConcentration_M <= 0)
                    return 0.0;
                double iRatio = SensorPhysics.SternVolmerIntensity(design.TargetConcentration_M, ksv);
                double loss = 1.0 - iRatio; // Signalverlust am Ziel
                return Math.Clamp(loss / 0.10, 0.0, 1.0); // 10 % Verlust = detektierbar
            }
            case "Charge":
            {
                // pH-Fall: Zielkonzentration als Aktivitaet; Signal = |ψ|.
                double psi = SensorPhysics.NernstPotential_mV(design.TargetConcentration_M);
                // Eine Dekade Abstand vom Neutralpunkt (~59 mV) gilt als klar detektierbar.
                return Math.Clamp(Math.Abs(psi) / 59.16, 0.0, 1.0);
            }
            case "PET":
            {
                if (analyte.RedoxPotential_V == 0)
                    return 0.0;
                double frac = SensorPhysics.PetQuenchFraction(
                    analyte.RedoxPotential_V, design.CoreMaterial.ElectronAffinity_eV);
                return Math.Clamp(frac / 0.5, 0.0, 1.0); // >= 50 % Quench-Fraktion = voller Score
            }
            default:
                return 0.0;
        }
    }

    // ------------------------------------------- 5. Kolloidale Stabilitaet

    /// <summary>
    /// Faktor 5 — Kolloidale &amp; chemische Stabilitaet: Ligand-Solvens gegen
    /// Medium plus Shell-CHEMIE. In Wasser sind Sulfid-Schalen (ZnS, CdS)
    /// chemisch robust (+), Selenid-Schalen (ZnSe) photo-oxidieren quicker
    /// (neutral) — deshalb ist ZnS die Industrie-Standard-Aussenschale — und
    /// ein nackter Core in Wasser degradieren (−). Roadmap 3.1 Faktor 5
    /// ("ZnSe in water weaker ~0.5, ZnS robust ~0.9").
    /// </summary>
    public static double StabilityScore(Ligand? ligand, string medium, Material? shellMaterial)
    {
        if (ligand is null)
            return 0.0; // keine Funktionalisierung: instabil in jedem Medium

        bool aqueousMedium = medium.Equals("aqueous", StringComparison.OrdinalIgnoreCase);
        bool aqueousLigand = ligand.Solubility.Equals("aqueous", StringComparison.OrdinalIgnoreCase);

        double score;
        if (aqueousMedium)
        {
            score = aqueousLigand ? 0.7 : 0.05;
            if (ligand.Charge_e != 0)
                score += 0.15; // elektrostatische Stabilisierung

            // Shell-Chemie gegen das wässrige Medium:
            if (shellMaterial is null)
                score -= 0.35; // nackter Core photo-oxidiert in Wasser in Minuten — schlechter als jede Schale
            else if (shellMaterial.Anion.Equals("S", StringComparison.OrdinalIgnoreCase))
                score += 0.05; // Sulfid-Schale: chemisch robust (ZnS-Standard, ~0.9)
            else if (shellMaterial.Anion.Equals("Se", StringComparison.OrdinalIgnoreCase))
                score -= 0.25; // Selenid-Schale: photo-oxidativer Abbau schneller als Sulfid (~0.6)
        }
        else
        {
            score = ligand.Solubility.Equals("organic", StringComparison.OrdinalIgnoreCase) ? 0.7 : 0.2;
            if (shellMaterial is not null)
                score += 0.05; // Schale schirmt auch in organischen Medien ab
        }

        return Math.Clamp(score, 0.0, 1.0);
    }

    // -------------------------------------------- 6. Bioconjugation

    /// <summary>
    /// Faktor 6 — Bioconjugation-Affinitaet: Rezeptor-Analyt-K_D gegen die
    /// Zielkonzentration. Regel der Roadmap: der relevante Bereich muss
    /// >= 10x Spanne um das Ziel abdecken. K_D ~ Ziel: 1.0;
    /// K_D &gt;&gt; Ziel: faellt logarithmisch (bindet nicht).
    /// Roadmap 3.1 Faktor 6.
    /// </summary>
    public static double BioconjugationScore(double? receptorKd_M, double targetConcentration_M)
    {
        if (receptorKd_M is null || receptorKd_M <= 0 || targetConcentration_M <= 0)
            return 0.0;

        double ratio = targetConcentration_M / receptorKd_M.Value;
        if (ratio >= 1.0)
            return 1.0; // Zielkonzentration dominiert die Bindung

        // Unterhalb von K_D: occupancy ~ [A]/(K_D+[A]) — Score folgt der Occupancy.
        double occupancy = targetConcentration_M / (receptorKd_M.Value + targetConcentration_M);
        // 10x-Spannen-Kriterium: K_D sollte nicht mehr als ~100x ueber dem Ziel liegen.
        return Math.Clamp(occupancy * 2.0, 0.0, 1.0);
    }
}