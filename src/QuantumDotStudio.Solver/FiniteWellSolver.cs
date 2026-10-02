using QuantumDotStudio.Core.Models;

namespace QuantumDotStudio.Solver;

/// <summary>
/// Löst die S-Zustände (l = 0) eines Teilchens der effektiven Masse m* in einem
/// endlichen sphärischen Potentialtopf: innen E = 0 für r &lt; R, außen Barriere V0.
/// Die Bindungsbedingung ist die Anschlussbedingung der radialen Wellenfunktion
/// bei r = R:
///     k·cot(k·R) = −κ   mit   k = √(2 m* E)/ħ   und   κ = √(2 m* (V0 − E))/ħ.
/// Der n-te Zustand liegt in genau einem Intervall k·R ∈ ((n−1)·π, n·π); dort ist
/// die Bedingung streng monoton fallend, daher existiert höchstens eine Wurzel,
/// die per Bisektion gefunden wird. Zustände, deren Wurzel oberhalb der Barriere
/// läge, sind nicht gebunden und werden nicht geliefert.
/// </summary>
public static class FiniteWellSolver
{
    /// <summary>
    /// Sucht die ersten maxStates gebundenen S-Zustände (l = 0) im endlichen Topf.
    /// Liefert die Energien in eV, aufsteigend sortiert. Ist die Barriere zu
    /// flach (unterhalb der 3D-Bindungsschwelle), ist die Liste leer.
    /// </summary>
    /// <param name="radius_nm">Core-Radius in nm.</param>
    /// <param name="mStar">Effektive Masse in m_e.</param>
    /// <param name="barrier_eV">Barrierenhöhe V0 in eV (muste &gt; 0 sein).</param>
    /// <param name="maxStates">Maximale Anzahl gebundener Zustände.</param>
    public static List<double> FindSEnergies_eV(double radius_nm, double mStar, double barrier_eV, int maxStates)
    {
        var energies = new List<double>();
        if (radius_nm <= 0 || mStar <= 0 || barrier_eV <= 0 || maxStates <= 0)
            return energies;

        double R_m = radius_nm * 1e-9;
        double m_kg = mStar * QuantumSolver.ElectronMass_kg;

        for (int n = 1; n <= maxStates; n++)
        {
            // Intervallgrenzen im Energieraum: E = ħ²(kR)² / (2 m R²), kR ∈ ((n−1)π, nπ).
            double eLow = n == 1
                ? 0.0
                : EnergyFromKR(QuantumSolver.HBar_Js, m_kg, R_m, (n - 1) * Math.PI);
            double eHigh = EnergyFromKR(QuantumSolver.HBar_Js, m_kg, R_m, n * Math.PI);

            if (eLow >= barrier_eV)
                break; // dieser und alle höheren Zustände sind nicht gebunden

            double lower = eLow;
            double upper = Math.Min(eHigh, barrier_eV);

            // Kleine Margins gegen die cot-Polen an den Intervallrändern.
            double margin = (upper - lower) * 1e-9 + 1e-15;
            lower += margin;
            upper -= margin;
            if (lower >= upper)
                break;

            double gLow = MatchingFunction(lower, R_m, m_kg, barrier_eV);
            double gHigh = MatchingFunction(upper, R_m, m_kg, barrier_eV);
            if (gLow <= 0 || gHigh >= 0)
                break; // Wurzel läge oberhalb der Barriere -> nicht gebunden

            // Bisektion: g ist im Intervall streng monoton fallend.
            for (int i = 0; i < 200 && (upper - lower) > 1e-13; i++)
            {
                double mid = 0.5 * (lower + upper);
                if (MatchingFunction(mid, R_m, m_kg, barrier_eV) > 0)
                    lower = mid;
                else
                    upper = mid;
            }

            energies.Add(0.5 * (lower + upper));
        }

        return energies;
    }

    /// <summary>
    /// Anschlussbedingung g(E) = k·cot(k·R) + κ. Gebundene Zustände sind die
    /// Nullstellen von g mit E &lt; V0.
    /// </summary>
    private static double MatchingFunction(double energy_eV, double R_m, double m_kg, double barrier_eV)
    {
        double e_J = energy_eV * QuantumSolver.EvToJ;
        double k = Math.Sqrt(2.0 * m_kg * e_J) / QuantumSolver.HBar_Js;                 // 1/m
        double kappa = Math.Sqrt(2.0 * m_kg * (barrier_eV - energy_eV) * QuantumSolver.EvToJ) / QuantumSolver.HBar_Js; // 1/m
        double x = k * R_m;
        return k * (Math.Cos(x) / Math.Sin(x)) + kappa;
    }

    private static double EnergyFromKR(double hbar_Js, double m_kg, double R_m, double kR)
    {
        return hbar_Js * hbar_Js * kR * kR / (2.0 * m_kg * R_m * R_m) / QuantumSolver.EvToJ;
    }
}