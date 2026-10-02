namespace QuantumDotStudio.Core.Models;

/// <summary>
/// Core/Shell-Quantum Dot: sphärischer Core aus einem Halbleitermaterial,
/// umgeben von einer Schale aus einem zweiten Material (z. B. CdSe/CdS).
/// Die Schale passiviert Oberflächenzustände und schirmt den Core gegen die
/// Umgebung ab — die Basis fast jedes realen QD-Sensors.
/// </summary>
public class CoreShellQuantumDot : QuantumDot
{
    /// <summary>
    /// Material der Schale.
    /// </summary>
    public Material ShellMaterial { get; set; } = new();

    /// <summary>
    /// Core-Radius in nm. (Radius_nm aus der Basisklasse bezeichnet den
    /// Gesamtradius Core + Schale.)
    /// </summary>
    public double CoreRadius_nm { get; set; }

    /// <summary>
    /// Schalendicke in nm.
    /// </summary>
    public double ShellThickness_nm { get; set; }

    /// <summary>
    /// Barrierenhöhe für Elektronen an der Core/Shell-Grenzfläche in eV
    /// (aus BandAlignment; positiv = Confinement im Core).
    /// </summary>
    public double ElectronBarrier_eV { get; set; }

    /// <summary>
    /// Barrierenhöhe für Löcher an der Core/Shell-Grenzfläche in eV.
    /// </summary>
    public double HoleBarrier_eV { get; set; }

    /// <summary>
    /// Gitterfehlanpassung f = (a_shell − a_core) / a_core.
    /// </summary>
    public double LatticeMismatch_f { get; set; }

    /// <summary>
    /// Kritische Schalendicke in nm (Matthews–Blakeslee-Abschätzung).
    /// Oberhalb relaxiert die Schale über Versetzungen.
    /// </summary>
    public double CriticalThickness_nm { get; set; }

    /// <summary>
    /// True, wenn die Schale dicker als die kritische Dicke ist:
    /// Versetzungsrelaxation wahrscheinlich, Konstruktion kritisch.
    /// </summary>
    public bool IsStrainRelaxed => ShellThickness_nm > CriticalThickness_nm;
}