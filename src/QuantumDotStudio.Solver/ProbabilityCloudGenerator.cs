using System.Numerics;

namespace QuantumDotStudio.Solver;

/// <summary>
/// Berechnet eine diskrete Wahrscheinlichkeitsdichte-Wolke für den Grundzustand
/// im unendlich tiefen sphärischen Potentialtopf (l=0, n=1).
/// </summary>
public class ProbabilityCloudGenerator
{
    /// <summary>
    /// Anzahl Gitterpunkte entlang jeder Achse (x, y, z). Standard 40 -> 64k Punkte.
    /// </summary>
    public int GridResolution { get; set; } = 40;

    /// <summary>
    /// Schwellwert: Punkte mit |ψ|² kleiner diesem Wert werden verworfen.
    /// </summary>
    public double ProbabilityThreshold { get; set; } = 0.05;

    /// <summary>
    /// Erzeugt eine Punktwolke mit Wahrscheinlichkeitsdichten für den 1S-Grundzustand.
    /// Gibt (Position in nm, normierte |ψ|²)-Tupel zurück.
    /// </summary>
    public IEnumerable<(Vector3 Position, float Probability)> GenerateElectronCloud1S(double radius_nm)
    {
        // Nullstelle von j_0: alpha = π
        double alpha = Math.PI;
        double R = radius_nm;
        double step = 2.0 * R / GridResolution;
        double max = 0.0;

        // Erster Durchlauf: max |ψ|² bestimmen für Normierung
        for (int ix = 0; ix < GridResolution; ix++)
        {
            double x = -R + ix * step;
            for (int iy = 0; iy < GridResolution; iy++)
            {
                double y = -R + iy * step;
                for (int iz = 0; iz < GridResolution; iz++)
                {
                    double z = -R + iz * step;
                    double r = Math.Sqrt(x * x + y * y + z * z);
                    if (r > R) continue;
                    double psiSq = PsiSquared1S(r, R, alpha);
                    if (psiSq > max) max = psiSq;
                }
            }
        }

        if (max <= 0.0)
            yield break;

        for (int ix = 0; ix < GridResolution; ix++)
        {
            double x = -R + ix * step;
            for (int iy = 0; iy < GridResolution; iy++)
            {
                double y = -R + iy * step;
                for (int iz = 0; iz < GridResolution; iz++)
                {
                    double z = -R + iz * step;
                    double r = Math.Sqrt(x * x + y * y + z * z);
                    if (r > R) continue;

                    double psiSq = PsiSquared1S(r, R, alpha) / max;
                    if (psiSq >= ProbabilityThreshold)
                    {
                        yield return (new Vector3((float)x, (float)y, (float)z), (float)psiSq);
                    }
                }
            }
        }
    }

    /// <summary>
    /// Berechnet |ψ_10(r)|² / N für den unendlichen Kugeltopf, ohne äußere Normierung.
    /// </summary>
    private static double PsiSquared1S(double r, double R, double alpha)
    {
        // ψ_n,l(r) ∝ j_l(alpha_nl * r / R) / r  (für l=0 reduziert sich dies auf sin/cos)
        // Für l=0: j_0(x) = sin(x)/x -> ψ_10(r) ∝ sin(α r/R) / r
        double x = alpha * r / R;
        if (r < 1e-6)
            return Math.Pow(alpha / R, 2); // Grenzwert sin(x)/x -> 1
        return Math.Pow(Math.Sin(x) / r, 2);
    }
}
