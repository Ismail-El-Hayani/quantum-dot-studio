using QuantumDotStudio.Solver;

namespace QuantumDotStudio.Tests;

/// <summary>
/// Tests für die numerische Berechnung der sphärischen Bessel-Nullstellen.
/// Literaturwerte: Standard-Tabellen (Abramowitz &amp; Stegun u.a.), z.B.
/// j_1-Nullstellen 4.493409, 7.725252, 10.904122, 14.066194, 17.220755;
/// j_2: 5.763459, 9.095011, 12.322941; j_3: 6.987932, 10.417119; j_4: 8.182561.
/// </summary>
public class BesselZeroTests
{
    [Theory]
    [InlineData(1, 0, 3.14159265358979)]
    [InlineData(2, 0, 6.28318530717959)]
    [InlineData(1, 1, 4.49340945790906)]
    [InlineData(2, 1, 7.72525183693771)]
    [InlineData(3, 1, 10.90412165962773)]
    [InlineData(4, 1, 14.066193912831)]
    [InlineData(5, 1, 17.22075527193077)]
    [InlineData(1, 2, 5.76345919686431)]
    [InlineData(2, 2, 9.09501133052874)]
    [InlineData(3, 2, 12.32294097009953)]
    [InlineData(1, 3, 6.98793200010502)]
    [InlineData(2, 3, 10.41711854737912)]
    [InlineData(1, 4, 8.18256145257179)]
    [InlineData(1, 5, 9.35581211104289)]
    public void BesselZero_Matches_Literature_Values(int n, int l, double expected)
    {
        double actual = Alpha(n, l);
        Assert.True(Math.Abs(actual - expected) < 1e-9,
            $"alpha({n},{l}): erwartet {expected}, erhalten {actual}");
    }

    [Theory]
    [InlineData(1, 1)]
    [InlineData(3, 2)]
    [InlineData(2, 3)]
    [InlineData(5, 1)]
    [InlineData(4, 4)]
    public void BesselZero_Interlacing_Holds(int n, int l)
    {
        // alpha_{n,l} muss strikt zwischen den Nachbarn unterer Ordnung liegen:
        // alpha_{n,l-1} < alpha_{n,l} < alpha_{n+1,l-1}.
        double aLower = Alpha(n, l - 1);
        double alpha  = Alpha(n, l);
        double aUpper = Alpha(n + 1, l - 1);

        Assert.True(aLower < alpha && alpha < aUpper,
            $"Interlacing verletzt: {aLower} < {alpha} < {aUpper}");
    }

    [Fact]
    public void BesselZero_Monotonic_In_n()
    {
        double prev = Alpha(1, 2);
        for (int n = 2; n <= 8; n++)
        {
            double curr = Alpha(n, 2);
            Assert.True(prev < curr, $"n-Monotonie verletzt bei n={n} (l=2)");
            prev = curr;
        }
    }

    [Fact]
    public void BesselZero_Fixed_Point_Regression()
    {
        // Die vorher fest kodierten Tabellenwerte (auf 8–9 Stellen gerundete
        // Literaturwerte) müssen reproduziert werden — sonst hätte die
        // Umstellung das Energiespektrum verschoben. Toleranz 1e-7 deckt
        // die Rundung der alten Tabelle ab.
        Assert.Equal(4.493409458, Alpha(1, 1), 7);
        Assert.Equal(7.725251837, Alpha(2, 1), 7);
        Assert.Equal(10.90412166, Alpha(3, 1), 7);
        Assert.Equal(5.763459197, Alpha(1, 2), 7);
        Assert.Equal(9.095011331, Alpha(2, 2), 7);
        Assert.Equal(12.32294096, Alpha(3, 2), 7);
    }

    [Fact]
    public void BesselZero_L_Too_Low_Throws()
    {
        Assert.Throws<System.Reflection.TargetInvocationException>(() => Alpha(1, -1));
    }

    [Fact]
    public void BesselZero_L_Too_High_Throws()
    {
        Assert.Throws<System.Reflection.TargetInvocationException>(() => Alpha(1, 11));
    }

    [Fact]
    public void BesselZero_Memoization_Makes_HighL_Fast()
    {
        // Ohne Cache explodierte der Aufrufbaum mit 2^l (l=10 brachte den
        // Testlauf zum Stillstand). Mit Memoization muss l=10 sofort liefern.
        var sw = System.Diagnostics.Stopwatch.StartNew();
        double a = Alpha(1, 10);
        sw.Stop();
        Assert.True(a > 0);
        Assert.True(sw.ElapsedMilliseconds < 5000,
            $"alpha(1,10) dauerte {sw.ElapsedMilliseconds} ms — Cache defekt?");
    }

    private static double Alpha(int n, int l)
    {
        return _cache.GetOrAdd((n, l), key =>
        {
            var method = typeof(QuantumSolver).GetMethod("BesselZero",
                System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Static);
            Assert.NotNull(method);
            return (double)method!.Invoke(null, new object[] { key.Item1, key.Item2 })!;
        });
    }

    private static readonly System.Collections.Concurrent.ConcurrentDictionary<(int n, int l), double> _cache = new();
}