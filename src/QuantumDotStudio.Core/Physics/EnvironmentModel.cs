using QuantumDotStudio.Core.Models;

namespace QuantumDotStudio.Core.Physics;

/// <summary>
/// Umgebungsmodell (Roadmap 2.3) als reine Funktionen:
///  - Temperatur: Varshni-Bandlückenverschiebung, referenziert auf 300 K —
///    die Referenz (materials.json) bleibt bei Raumtemperatur exakt erhalten.
///    Varshni-Form: E_g(T) = E_g(0) − α·T²/(T+β)  (Varshni, Physica 34, 149 (1967)).
///  - Solvens: phänomenologische effektive Dielektrizitäts-Mischung für die
///    Coulomb-Abschirmung: ε_eff = (2·ε_dot + ε_sol)/3 — die Träger sitzen im
///    Dot (2/3-Gewicht), das Medium schirmt zusätzlich ab. Dokumentierte
///    Vereinfachung statt vollständiger Bildladungs-Theorie.
///  - pH: Liganden-Stabilitätsfenster (nur wässrige Liganden tragen ein Fenster;
///    organische Liganden sind pH-unabhängig — „null“ heißt „nicht anwendbar“).
/// </summary>
public static class EnvironmentModel
{
    /// <summary>Referenztemperatur, auf die die materials.json-Werte gelten.</summary>
    public const double ReferenceTemperature_K = 300.0;

    /// <summary>
    /// Varshni-Verschiebung ΔE_g(T) = α·[T²/(T+β) − T_ref²/(T_ref+β)] in eV.
    /// Positiv für T &gt; T_ref (die Lücke SCHRUMPFT um diesen Betrag).
    /// Ungültige Parameter (α/β/T &lt;= 0) ergeben 0 — kein Modell-Absturz.
    /// </summary>
    public static double VarshniShift_eV(
        double alpha_eV_per_K, double beta_K, double temperature_K,
        double referenceTemperature_K = ReferenceTemperature_K)
    {
        if (alpha_eV_per_K <= 0 || beta_K <= 0 || temperature_K <= 0)
            return 0.0;

        return alpha_eV_per_K * (
            temperature_K * temperature_K / (temperature_K + beta_K)
            - referenceTemperature_K * referenceTemperature_K / (referenceTemperature_K + beta_K));
    }

    /// <summary>
    /// Bandlücke bei Temperatur T: Referenzlücke minus Varshni-Verschiebung.
    /// Bei T = T_ref exakt die Referenzlücke (Anker für materials.json).
    /// </summary>
    public static double BandGapAtTemperature(
        double bandGapRef_eV, double alpha_eV_per_K, double beta_K, double temperature_K,
        double referenceTemperature_K = ReferenceTemperature_K)
    {
        return bandGapRef_eV
            - VarshniShift_eV(alpha_eV_per_K, beta_K, temperature_K, referenceTemperature_K);
    }

    /// <summary>
    /// Effektive Dielektrizitätskonstante für den Coulomb-Term im Solvens:
    /// ε_eff = (2·ε_dot + ε_sol)/3 (phänomenologische Mischung, dokumentierte
    /// Vereinfachung). Ohne Solvens (null oder ε_sol &lt;= 0) bleibt ε_dot.
    /// </summary>
    public static double EffectiveDielectric(double dotDielectric, double? solventDielectric)
    {
        return solventDielectric is > 0
            ? (2.0 * dotDielectric + solventDielectric.Value) / 3.0
            : dotDielectric;
    }

    /// <summary>
    /// pH-Fenster-Score eines Liganden: 1.0 im (oder ohne) Stabilitätsfenster,
    /// 0.25 außerhalb — kolloidale Stabilität bricht außerhalb des Fensters
    /// ein, ist aber durch pH-Anpassung behebbar (kein harter Kill).
    /// „Kein Fenster“ (organische Liganden) heißt „pH nicht anwendbar“ — Score 1.
    /// </summary>
    public static double PhWindowScore(Ligand? ligand, double ph)
    {
        if (ligand?.StabilityPhMin is not double lo || ligand.StabilityPhMax is not double hi)
            return 1.0; // pH-unabhängig — nicht anwendbar heißt nicht schlecht

        return ph >= lo && ph <= hi ? 1.0 : 0.25;
    }

    /// <summary>True, wenn der pH im Stabilitätsfenster des Liganden liegt.</summary>
    public static bool IsPhStable(Ligand? ligand, double ph)
        => ligand?.StabilityPhMin is double lo && ligand.StabilityPhMax is double hi
           && ph >= lo && ph <= hi;
}