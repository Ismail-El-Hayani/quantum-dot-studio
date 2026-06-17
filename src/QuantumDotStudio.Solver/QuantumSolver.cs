using QuantumDotStudio.Core.Models;

namespace QuantumDotStudio.Solver;

/// <summary>
/// Berechnet quantenmechanische Größen für sphärische Quantum Dots.
/// </summary>
public static class QuantumSolver
{
    // Physikalische Konstanten
    private const double HBar_eV_s = 6.582119569e-16; // eV·s
    private const double ElectronMass_kg = 9.10938356e-31;
    private const double EvToJ = 1.602176634e-19;
    private const double SpeedOfLight_m_s = 2.99792458e8;
    private const double JouleMeter = 1.98644586e-25; // h*c in J·m

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

        // E = (hbar^2 * alpha_nl^2) / (2 * m* * R^2)
        // m* in kg = mStar * m_e
        double m_kg = mStar * ElectronMass_kg;
        double energy_J = Math.Pow(HBar_eV_s * alpha_nl / R_m, 2) / (2.0 * m_kg);

        // HBar_eV_s wurde in eV*s verwendet, aber die Division durch R_m liefert (eV*s / m)^2 = eV^2*s^2/m^2
        // Dann / kg ergibt eV^2; sqrt? Nein — wir haben hbar^2/(2mR^2) mit hbar in J*s wäre konsistent.
        // Korrektur: hbar in J*s verwenden, dann in eV umrechnen.
        double hbar_Js = 1.054571817e-34;
        energy_J = Math.Pow(hbar_Js * alpha_nl / R_m, 2) / (2.0 * m_kg);
        return energy_J / EvToJ;
    }

    /// <summary>
    /// Berechnet die effektive Bandlücke eines Quantum Dots mit der vereinfachten Brus-Formel.
    /// E_QD = E_g + ΔE_e + ΔE_h (ohne Coulomb-Korrektur).
    /// </summary>
    public static double BrusBandGap(Material material, double radius_nm)
    {
        double eElectron = ConfinementEnergy(radius_nm, material.EffectiveMassElectron, 1, 0);
        double eHole = ConfinementEnergy(radius_nm, material.EffectiveMassHole, 1, 0);
        return material.BandGap_eV + eElectron + eHole;
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
