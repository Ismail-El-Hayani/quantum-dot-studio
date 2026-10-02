using QuantumDotStudio.Core.Models;

namespace QuantumDotStudio.Solver;

/// <summary>
/// Berechnet quantenmechanische Größen für sphärische Quantum Dots.
/// </summary>
public static class QuantumSolver
{
    // Physikalische Konstanten (public: gemeinsame Nutzung durch FiniteWellSolver)
    public const double HBar_Js = 1.054571817e-34;           // J·s
    public const double ElectronMass_kg = 9.10938356e-31;      // kg
    public const double EvToJ = 1.602176634e-19;              // J/eV
    private const double JouleMeter = 1.98644586e-25;           // h·c in J·m
    private const double CoulombConstant_eV_nm = 1.439964548;   // e²/(4πε₀) in eV·nm

    /// <summary>
    /// Liefert die Nullstellen der sphärischen Bessel-Funktion j_l, multipliziert mit π.
    /// Für l=0: n*pi. Für l=1: Werte wie 4.493, 7.725, ...
    /// </summary>
    private static double BesselZero(int n, int l)
    {
        // Vorberechnete Werte für die ersten Niveaus im unendlichen sphärischen Topf.
        // n = 1, 2, ... ; l = 0, 1, 2, ...
        return l switch
        {
            0 => n * Math.PI,
            1 => n switch
            {
                1 => 4.493409458,
                2 => 7.725251837,
                3 => 10.90412166,
                _ => (n + 0.5) * Math.PI // asymptotisch
            },
            2 => n switch
            {
                1 => 5.763459197,
                2 => 9.095011331,
                3 => 12.32294096,
                _ => (n + 0.5) * Math.PI
            },
            _ => (n + 0.5) * Math.PI
        };
    }

    /// <summary>
    /// Berechnet die Confinement-Energie eines Teilchens der effektiven Masse mStar
    /// im unendlichen sphärischen Potentialtopf mit Radius R in nm.
    /// </summary>
    /// <param name="R_nm">Radius in nm.</param>
    /// <param name="mStar">Effektive Masse in m_e.</param>
    /// <param name="n">Radiale Quantenzahl.</param>
    /// <param name="l">Drehimpulsquantenzahl.</param>
    /// <returns>Energie in eV.</returns>
    public static double ConfinementEnergy(double R_nm, double mStar, int n, int l)
    {
        double R_m = R_nm * 1e-9;
        double alpha_nl = BesselZero(n, l);

        // E = ħ² α_{n,l}² / (2 m* R²)  (ħ in J·s), anschließend Umrechnung nach eV.
        double m_kg = mStar * ElectronMass_kg;
        double energy_J = Math.Pow(HBar_Js * alpha_nl / R_m, 2) / (2.0 * m_kg);
        return energy_J / EvToJ;
    }

    /// <summary>
    /// Berechnet den Coulomb-Anziehungsterm (führende Ordnung) des Elektron-Loch-Paares
    /// im Quantum Dot: E_C = −1.786 · e² / (4π ε₀ ε_r R).
    /// </summary>
    /// <param name="R_nm">Radius in nm.</param>
    /// <param name="dielectricConstant">Statische Dielektrizitätskonstante ε_r.</param>
    /// <returns>Coulomb-Energie in eV (immer negativ).</returns>
    public static double CoulombEnergy(double R_nm, double dielectricConstant)
    {
        if (R_nm <= 0 || dielectricConstant <= 0)
            return 0.0;

        return -1.786 * CoulombConstant_eV_nm / (dielectricConstant * R_nm);
    }

    /// <summary>
    /// Berechnet die effektive Bandlücke eines Quantum Dots nach der Brus-Formel:
    /// E_QD = E_g + ΔE_e + ΔE_h − 1.786 e² / (4π ε₀ ε_r R).
    /// </summary>
    public static double BrusBandGap(Material material, double radius_nm)
    {
        double eElectron = ConfinementEnergy(radius_nm, material.EffectiveMassElectron, 1, 0);
        double eHole = ConfinementEnergy(radius_nm, material.EffectiveMassHole, 1, 0);
        double eCoulomb = CoulombEnergy(radius_nm, material.DielectricConstant);
        return material.BandGap_eV + eElectron + eHole + eCoulomb;
    }

    /// <summary>
    /// Berechnet die Emissionswellenlänge λ = hc / E aus der effektiven Bandlücke.
    /// </summary>
    public static double WavelengthFromBandGap(double bandGap_eV)
    {
        if (bandGap_eV <= 0)
            return double.PositiveInfinity;

        double energy_J = bandGap_eV * EvToJ;
        double lambda_m = JouleMeter / energy_J;
        return lambda_m * 1e9; // nm
    }

    /// <summary>
    /// Berechnet eine Reihe von Energieniveaus bis zu einer maximalen Quantenzahl.
    /// </summary>
    public static List<EnergyLevel> CalculateEnergyLevels(double R_nm, double mStar, int maxN = 3, int maxL = 2)
    {
        var levels = new List<EnergyLevel>();
        for (int l = 0; l <= maxL; l++)
        {
            for (int n = 1; n <= maxN; n++)
            {
                levels.Add(new EnergyLevel
                {
                    PrincipalQuantumNumber_n = n,
                    AngularMomentum_l = l,
                    Energy_eV = ConfinementEnergy(R_nm, mStar, n, l),
                    Label = GetSpectroscopicLabel(n, l)
                });
            }
        }
        return levels.OrderBy(e => e.Energy_eV).ToList();
    }

    private static string GetSpectroscopicLabel(int n, int l)
    {
        string symbol = l switch
        {
            0 => "S",
            1 => "P",
            2 => "D",
            3 => "F",
            _ => $"L{l}"
        };
        return $"{n}{symbol}";
    }
}
