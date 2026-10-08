using QuantumDotStudio.Core.Models;

namespace QuantumDotStudio.Core.Physics;

/// <summary>
/// Reine Sensor-Transduktionsphysik. Jede Funktion ist mathematisch pur
/// (keine versteckten Zustaende) und unit-testbar (Roadmap-Regel 1).
///
/// Referenzen:
/// - Foerster 1948; Lakowicz, "Principles of Fluorescence Spectroscopy", Kap. 13
///   (E = 1 / (1 + (r/R0)^6))
/// - Stern-Volmer: Lakowicz Kap. 8 (I0/I = 1 + K_SV [A])
/// - Nernst: 59.16 mV/Dekade bei 25 C fuer einwertige Ionen
/// - PET: energetisches Kriterium (Akzeptor-Redoxpotential vs. CB-Kante)
/// </summary>
public static class SensorPhysics
{
    // ---------------------------------------------------------------- FRET

    /// <summary>
    /// Foerster-Transfer-Effizienz E = 1 / (1 + (r/R0)^6).
    /// r: Donor-Akzeptor-Abstand, R0: Foerster-Radius (beide in nm).
    /// </summary>
    public static double FretEfficiency(double distance_nm, double forsterRadius_nm)
    {
        if (distance_nm <= 0) throw new ArgumentOutOfRangeException(nameof(distance_nm));
        if (forsterRadius_nm <= 0) throw new ArgumentOutOfRangeException(nameof(forsterRadius_nm));

        double ratio = distance_nm / forsterRadius_nm;
        return 1.0 / (1.0 + Math.Pow(ratio, 6));
    }

    /// <summary>
    /// Donor-Akzeptor-Abstand: QD-Radius + Ligandlaenge + ggf. Rezeptor-
    /// Zusatzabstand. r = R_QD + L_ligand + delta_rezeptor.
    /// </summary>
    public static double FretDistance(double qdRadius_nm, double ligandLength_nm, double receptorOffset_nm = 0.0)
    {
        if (qdRadius_nm < 0 || ligandLength_nm < 0 || receptorOffset_nm < 0)
            throw new ArgumentOutOfRangeException("Alle Eingaben muessen >= 0 sein.");

        return qdRadius_nm + ligandLength_nm + receptorOffset_nm;
    }

    /// <summary>
    /// Ratiometrisches Signal des FRET-Paars: Donor/Akzeptor-Verhaeltnis,
    /// normiert auf den ausgeschalteten Zustand. R = I_A / I_D = E / (1 - E).
    /// </summary>
    public static double FretRatio(double efficiency)
    {
        if (efficiency <= 0) throw new ArgumentOutOfRangeException(nameof(efficiency));
        if (efficiency >= 1) throw new ArgumentOutOfRangeException(nameof(efficiency));

        return efficiency / (1.0 - efficiency);
    }

    /// <summary>
    /// Kontrast (relative Aenderung des ratiometrischen Signals) zwischen
    /// Analyt-gebundenem und ungebundenem Zustand. Design-Regel der Roadmap:
    /// |Δr| >= 1 nm Abstandsunterschied fuer ein aufloesbares Signal.
    /// </summary>
    public static double FretContrast(double distanceUnbound_nm, double distanceBound_nm, double forsterRadius_nm)
    {
        double eUnbound = FretEfficiency(distanceUnbound_nm, forsterRadius_nm);
        double eBound = FretEfficiency(distanceBound_nm, forsterRadius_nm);
        return Math.Abs(eBound - eUnbound) / eUnbound;
    }

    // ------------------------------------------------------------ Quenching

    /// <summary>
    /// Stern-Volmer: normierte Intensitaet I/I0 = 1 / (1 + K_SV · [A]).
    /// K_SV in M^-1, Konzentration in M.
    /// </summary>
    public static double SternVolmerIntensity(double concentration_M, double kSV_M)
    {
        if (concentration_M < 0) throw new ArgumentOutOfRangeException(nameof(concentration_M));
        if (kSV_M < 0) throw new ArgumentOutOfRangeException(nameof(kSV_M));

        return 1.0 / (1.0 + kSV_M * concentration_M);
    }

    /// <summary>
    /// Detektionsgrenze (LOD): Konzentration fuer 10 % Signalverlust
    /// (I/I0 = 0.9): [A]_LOD = (1/0.9 − 1)/K_SV ≈ 0.111 / K_SV.
    /// </summary>
    public static double QuenchingLOD(double kSV_M)
    {
        if (kSV_M <= 0) throw new ArgumentOutOfRangeException(nameof(kSV_M));
        return (1.0 / 0.9 - 1.0) / kSV_M;
    }

    // --------------------------------------------------------------- Charge

    /// <summary>
    /// Nernst-Potential in mV: E = E0 + slope · log10(a), slope = 59.16 mV/Dekade
    /// (einwertig, 25 C). Aktivitaet a = 10^-pH fuer den pH-Fall.
    /// </summary>
    public static double NernstPotential_mV(double activity, double slope_mV_per_decade = 59.16)
    {
        if (activity <= 0) throw new ArgumentOutOfRangeException(nameof(activity));

        return slope_mV_per_decade * Math.Log10(activity);
    }

    /// <summary>
    /// Spektraler Shift der QD-Emission durch ein Oberflaechenpotential:
    /// lineares Modell delta_lambda = k_shift · psi. k_shift ~ 0.5-2 nm/mV
    /// (QCSE-artige Empfindlichkeit von QDs); Standard 1 nm/mV konservativ.
    /// </summary>
    public static double SurfacePotentialSpectralShift(double surfacePotential_mV, double sensitivity_nm_per_mV = 1.0)
    {
        return sensitivity_nm_per_mV * surfacePotential_mV;
    }

    // ----------------------------------------------------------------- PET

    /// <summary>
    /// PET-Kriterium: ist Elektrontransfer vom angeregten QD zum Analyten
    /// energetisch abwaerts moeglich (Quench-ON) oder nicht (Quench-OFF)?
    /// CB-Kante vs. NHE: U_CB = EA − 4.44 (4.44 V = 0 V NHE vs. Vakuum).
    /// Ein Elektron faellt vom CB (z.B. +0.46 V fuer CdSe) zum Akzeptor mit
    /// REDOXpotential E0 — abwaerts bedeutet E0 &gt; U_CB (positiveres
    /// Potential = niedrigere Elektronenenergie). PET ist also moeglich,
    /// wenn das Redoxpotential des Analyten OBERHALB der CB-Kante liegt.
    /// Konsistent mit <see cref="PetQuenchFraction"/> (dort &gt; 0.5).
    /// </summary>
    public static bool PetIsEnergeticallyFavorable(double analyteRedox_V, double electronAffinity_eV)
    {
        double cbEdge_V_vsNHE = electronAffinity_eV - 4.44; // CB-Kante in V vs. NHE
        return analyteRedox_V > cbEdge_V_vsNHE;
    }

    /// <summary>
    /// PET-Quenching-Faktor: 1 = vollstaendig geloescht, 0 = keine Loeschung.
    /// Boltzmann-artige Uebergangszone um die Energieluecke (kT ~ 25.7 meV bei
    /// 25 C); naechstgelegene Luecke = cbEdge − redox.
    /// </summary>
    public static double PetQuenchFraction(double analyteRedox_V, double electronAffinity_eV, double kT_eV = 0.0257)
    {
        if (kT_eV <= 0) throw new ArgumentOutOfRangeException(nameof(kT_eV));

        double gap_eV = (electronAffinity_eV - 4.44) - analyteRedox_V;
        // Fermi-artige Kante: bei gap = 0 exakt 50 %, weit negativ -> 1 (ON),
        // weit positiv -> 0 (OFF).
        return 1.0 / (1.0 + Math.Exp(gap_eV / kT_eV));
    }
}