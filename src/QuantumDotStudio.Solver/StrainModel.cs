using QuantumDotStudio.Core.Models;

namespace QuantumDotStudio.Solver;

/// <summary>
/// Elastische Größen für Core/Shell-Heterostrukturen (Näherung).
/// Gitterfehlanpassung f = (a_shell − a_core) / a_core und kritische
/// Schalendicke t_c ≈ b / (2·|f|) als Matthews–Blakeslee-artige Abschätzung
/// (Versetzungsburgersvektor b ≈ 0,3 nm, typisch für II-VI/III-V-Halbleiter).
/// Oberhalb von t_c relaxiert die Schale über Versetzungen und die
/// Strahlenqualität (Quanteneffizienz) sinkt deutlich.
/// </summary>
public static class StrainModel
{
    /// <summary>
    /// Burgersvektor-Länge in nm (typisch a/√2 der dichtestgepackten Ebenen).
    /// </summary>
    public const double BurgersVector_nm = 0.30;

    /// <summary>
    /// Gitterfehlanpassung f = (a_shell − a_core) / a_core. Negativ = Schale
    /// ist kompressiv verspannt (kleinere Gitterkonstante).
    /// </summary>
    public static double LatticeMismatch(Material core, Material shell)
    {
        ArgumentNullException.ThrowIfNull(core);
        ArgumentNullException.ThrowIfNull(shell);
        return (shell.LatticeConstant_A - core.LatticeConstant_A) / core.LatticeConstant_A;
    }

    /// <summary>
    /// Kritische Schalendicke in nm: Oberhalb relaxiert die Schale über
    /// Versetzungen. Bei (nahezu) fehlender Fehlanpassung ist sie formell
    /// unendlich (kohärentes Wachstum in beliebiger Dicke).
    /// </summary>
    public static double CriticalThickness_nm(Material core, Material shell)
    {
        double f = Math.Abs(LatticeMismatch(core, shell));
        if (f < 1e-6)
            return double.PositiveInfinity;

        return BurgersVector_nm / (2.0 * f);
    }

    /// <summary>
    /// True, wenn die Schale dicker als die kritische Dicke ist — dann ist
    /// Versetzungsrelaxation wahrscheinlich und die Konstruktion kritisch.
    /// </summary>
    public static bool IsStrainRelaxed(Material core, Material shell, double shellThickness_nm)
    {
        return shellThickness_nm > CriticalThickness_nm(core, shell);
    }
}