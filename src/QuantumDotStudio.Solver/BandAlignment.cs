using QuantumDotStudio.Core.Models;

namespace QuantumDotStudio.Solver;

/// <summary>
/// Bandanpassung (Band-Offsets) zwischen Core- und Shell-Material aus den
/// Elektronaffinitäten χ (Abstand Vakuumniveau → Leitungsbandminimum):
///     V0_e = χ_core − χ_shell             (Barriere für Elektronen im Core)
///     V0_h = (Eg_shell − Eg_core) − V0_e  (Barriere für Löchern im Core)
/// Positive Werte bedeuten Confinement des jeweiligen Ladungsträgers im Core
/// (Typ-I-artig). Ein negativer Wert bedeutet: dieser Träger ist im Core
/// nicht gebunden (Typ-II-artig) — der endliche Potentialtopf hat dann keine
/// gebundenen Zustände für ihn.
/// </summary>
public static class BandAlignment
{
    /// <summary>
    /// Barrierenhöhe für Elektronen an der Core/Shell-Grenzfläche in eV.
    /// </summary>
    public static double ElectronBarrier_eV(Material core, Material shell)
    {
        ArgumentNullException.ThrowIfNull(core);
        ArgumentNullException.ThrowIfNull(shell);
        return core.ElectronAffinity_eV - shell.ElectronAffinity_eV;
    }

    /// <summary>
    /// Barrierenhöhe für Löcher an der Core/Shell-Grenzfläche in eV.
    /// </summary>
    public static double HoleBarrier_eV(Material core, Material shell)
    {
        ArgumentNullException.ThrowIfNull(core);
        ArgumentNullException.ThrowIfNull(shell);
        return (shell.BandGap_eV - core.BandGap_eV) - ElectronBarrier_eV(core, shell);
    }
}