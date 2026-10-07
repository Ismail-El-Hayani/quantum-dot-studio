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

    // Cache für Bessel-Nullstellen: die Interlacing-Rekursion verzweigt sich
    // zweiarmig (alpha_{n,l} braucht alpha_{n,l-1} und alpha_{n+1,l-1}), ohne
    // Memoization würde der Aufrufbaum mit 2^l explodieren.
    private static readonly System.Collections.Concurrent.ConcurrentDictionary<(int n, int l), double> _besselZeroCache = new();

    private const int MaxL = 10; // Physik: höhere Drehimpulse sind im QD-Modell irrelevant

    /// <summary>
    /// Nullstelle alpha_{n,l} der sphärischen Bessel-Funktion j_l (n-te positive Nullstelle),
    /// numerisch exakt statt asymptotischer Näherung: die Nullstellen von j_l schachteln
    /// sich mit denen von j_{l-1}, daher ist (alpha_{n,l-1}, alpha_{n+1,l-1}) ein
    /// Klammer-Intervall — rekursiv bis auf den exakten l=0-Fall n·π.
    /// </summary>
    private static double BesselZero(int n, int l)
    {
        if (n < 1) throw new ArgumentOutOfRangeException(nameof(n));
        if (l < 0 || l > MaxL) throw new ArgumentOutOfRangeException(nameof(l));

        return _besselZeroCache.GetOrAdd((n, l), key =>
        {
            if (key.l == 0) return key.n * Math.PI;

            // Interlacing-Klammer: alpha_{n,l} liegt zwischen den l-1-Nullstellen n und n+1.
            double lower = BesselZero(key.n, key.l - 1);
            double upper = BesselZero(key.n + 1, key.l - 1);

            return BisectionRoot(
                x => SphericalBesselJ(key.l, x),
                lower, upper,
                tolerance: 1e-12);
        });
    }

    /// <summary>
    /// Sphärische Bessel-Funktion j_l(x) über die Aufwärts-Rekurrenz
    /// j_{k+1}(x) = (2k+1)/x · j_k(x) − j_{k−1}(x), gestartet mit den exakten
    /// Startwerten j_0 = sin x / x und j_1 = sin x / x² − cos x / x.
    /// Für die hier benötigten Bereiche (l klein, x zwischen aufeinanderfolgenden
    /// Nullstellen) ist die Rekurrenz stabil; Ergebnisse sind gegen
    /// literaturbekannte Nullstellen getestet (BesselZeroTests).
    /// </summary>
    private static double SphericalBesselJ(int l, double x)
    {
        if (l < 0) throw new ArgumentOutOfRangeException(nameof(l));
        if (x <= 0) return l == 0 ? 1.0 : 0.0;

        if (l == 0) return Math.Sin(x) / x;
        if (l == 1) return Math.Sin(x) / (x * x) - Math.Cos(x) / x;

        double jPrev = Math.Sin(x) / x;                          // j_0
        double jCurr = Math.Sin(x) / (x * x) - Math.Cos(x) / x;  // j_1
        for (int k = 1; k < l; k++)
        {
            double jNext = (2.0 * k + 1.0) / x * jCurr - jPrev;
            jPrev = jCurr;
            jCurr = jNext;
        }
        return jCurr;
    }

    /// <summary>
    /// Nullstellensuche per Bisektion auf einem garantierten Klammer-Intervall.
    /// </summary>
    private static double BisectionRoot(Func<double, double> f, double lower, double upper, double tolerance)
    {
        double fLower = f(lower);
        double fUpper = f(upper);

        // An den Intervallrändern kann numerisch exakt 0 auftreten.
        if (Math.Abs(fLower) < 1e-30) return lower;
        if (Math.Abs(fUpper) < 1e-30) return upper;

        if (double.IsNaN(fLower) || double.IsNaN(fUpper))
            throw new InvalidOperationException("Bessel-Nullstellensuche: NaN am Intervallrand");

        bool signLower = fLower > 0;
        if (signLower == (fUpper > 0))
            throw new InvalidOperationException("Bessel-Nullstellensuche: kein Vorzeichenwechsel im Klammer-Intervall");

        for (int i = 0; i < 200; i++)
        {
            double mid = 0.5 * (lower + upper);
            double fMid = f(mid);

            if (Math.Abs(fMid) < 1e-30 || upper - lower < tolerance)
                return mid;

            if ((fMid > 0) == signLower)
            {
                lower = mid;
                signLower = fMid > 0;
            }
            else
            {
                upper = mid;
            }
        }

        return 0.5 * (lower + upper);
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
