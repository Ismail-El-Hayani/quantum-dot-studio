using QuantumDotStudio.Core.Data;
using QuantumDotStudio.Core.Physics;
using Xunit;

namespace QuantumDotStudio.Tests;

/// <summary>
/// Tests der Sensor-Transduktionsphysik (Roadmap Phase 2).
/// FRET-Referenztabelle (R0 = 5 nm) aus docs/ROADMAP.md:
/// 3 nm -> 95.5 %, 4 nm -> 79.2 %, 5 nm -> 50 %, 6 nm -> 25.1 %,
/// 8 nm -> 5.6 %, 10 nm -> 1.5 %.
/// </summary>
public class SensorPhysicsTests
{
    // ----------------------------------------------------------------- FRET

    [Theory]
    [InlineData(3.0, 5.0, 0.955)]   // 95.5 %
    [InlineData(4.0, 5.0, 0.792)]   // 79.2 %
    [InlineData(5.0, 5.0, 0.500)]   // 50.0 %
    [InlineData(6.0, 5.0, 0.251)]   // 25.1 %
    [InlineData(8.0, 5.0, 0.056)]   //  5.6 %
    [InlineData(10.0, 5.0, 0.015)]   //  1.5 %
    public void FretEfficiency_Matches_Roadmap_Reference_Table(double r, double R0, double expected)
    {
        double e = SensorPhysics.FretEfficiency(r, R0);
        Assert.True(Math.Abs(e - expected) < 0.005,
            $"E(r={r}, R0={R0}): erwartet ~{expected}, erhalten {e:F4}");
    }

    [Fact]
    public void FretEfficiency_At_R0_Is_Half()
    {
        Assert.Equal(0.5, SensorPhysics.FretEfficiency(5.0, 5.0), 9);
    }

    [Fact]
    public void FretEfficiency_Monotonic_Decreasing_In_Distance()
    {
        double prev = 1.0;
        for (double r = 1.0; r <= 12.0; r += 0.5)
        {
            double e = SensorPhysics.FretEfficiency(r, 5.0);
            Assert.True(e < prev, $"E muss in r monoton fallen (r={r})");
            prev = e;
        }
    }

    [Fact]
    public void FretDistance_Sums_Geometry()
    {
        Assert.Equal(8.6, SensorPhysics.FretDistance(3.0, 0.6, 5.0), 9);
        Assert.Equal(3.6, SensorPhysics.FretDistance(3.0, 0.6), 9);
    }

    [Fact]
    public void FretContrast_High_When_Binding_Changes_Distance()
    {
        // Klassischer Fall: Analyt-Bindung verkuerzt den Abstand (Conformation-
        // Change) -> grosse Effizienzaenderung.
        double contrast = SensorPhysics.FretContrast(6.0, 4.5, 5.0);
        Assert.True(contrast > 0.2, $"Kontrast {contrast:F3} sollte > 20 % sein");
    }

    [Fact]
    public void FretContrast_Low_When_Distance_Unchanged()
    {
        double contrast = SensorPhysics.FretContrast(5.0, 5.05, 5.0);
        Assert.True(contrast < 0.05, $"Kontrast {contrast:F3} sollte < 5 % sein");
    }

    // ------------------------------------------------------------ Quenching

    [Fact]
    public void SternVolmer_Matches_Linear_Form()
    {
        // I0/I = 1 + K_SV [A] -> I/I0 = 1/(1 + K_SV [A])
        double kSV = 4.2e5;
        double c = 1e-6;
        double expected = 1.0 / (1.0 + kSV * c);
        Assert.Equal(expected, SensorPhysics.SternVolmerIntensity(c, kSV), 12);
    }

    [Fact]
    public void SternVolmer_Is_Linear_In_Concentration_For_Small_C()
    {
        // Fuer K_SV [A] << 1 ist I/I0 ≈ 1 − K_SV [A] (linearisierte Form).
        double kSV = 4.2e5;
        double c = 1e-7; // K_SV·c = 0.042 << 1
        double i = SensorPhysics.SternVolmerIntensity(c, kSV);
        double linear = 1.0 - kSV * c;
        Assert.True(Math.Abs(i - linear) < 0.002, $"I/I0 = {i:F5} vs. linearisiert {linear:F5}");
    }

    [Fact]
    public void SternVolmer_Half_Intensity_At_KSV_Inverse()
    {
        // I/I0 = 0.5 bei [A] = 1/K_SV
        double kSV = 1.0e6;
        double i = SensorPhysics.SternVolmerIntensity(1.0 / kSV, kSV);
        Assert.Equal(0.5, i, 9);
    }

    [Fact]
    public void QuenchingLOD_Is_Ten_Percent_Threshold()
    {
        double lod = SensorPhysics.QuenchingLOD(4.2e5);
        double iAtLod = SensorPhysics.SternVolmerIntensity(lod, 4.2e5);
        Assert.Equal(0.9, iAtLod, 9); // I/I0 = 0.9 an der LOD
        Assert.Equal(2.646e-7, lod, 3); // (1/0.9 - 1)/K_SV
    }

    // --------------------------------------------------------------- Charge

    [Fact]
    public void Nernst_One_Decade_Is_59mV()
    {
        // log10(10) = +1 -> +59.16 mV; log10(0.1) = -1 -> -59.16 mV.
        Assert.Equal(59.16, SensorPhysics.NernstPotential_mV(10.0), 6);
        Assert.Equal(-59.16, SensorPhysics.NernstPotential_mV(0.1), 6);
    }

    [Fact]
    public void Nernst_At_pH7_Is_Minus414mV()
    {
        // pH 7: a = 10^-7 -> E = 59.16 · (-7) = −414.1 mV
        double e = SensorPhysics.NernstPotential_mV(Math.Pow(10, -7));
        Assert.Equal(-414.12, e, 1);
    }

    [Fact]
    public void SurfacePotentialShift_Scales_Linearly()
    {
        Assert.Equal(5.0, SensorPhysics.SurfacePotentialSpectralShift(5.0), 9);
        Assert.Equal(10.0, SensorPhysics.SurfacePotentialSpectralShift(5.0, 2.0), 9);
    }

    // ----------------------------------------------------------------- PET

    [Fact]
    public void PetIsFavorable_When_RedoxAbove_CBand()
    {
        // CdSe: EA = 4.9 eV -> CB = 4.9 - 4.44 = +0.46 V vs. NHE.
        // Ein Elektron faellt vom CB zum Akzeptor, wenn dessen Redoxpotential
        // OBERHALB der CB-Kante liegt: E0 = +1.0 V -> PET moeglich (Quench-ON).
        Assert.True(SensorPhysics.PetIsEnergeticallyFavorable(1.0, 4.9));
        // E0 = 0.0 V liegt UNTER der CB-Kante -> bergauf, kein PET.
        Assert.False(SensorPhysics.PetIsEnergeticallyFavorable(0.0, 4.9));
    }

    [Fact]
    public void PetQuenchFraction_Transitions_At_Energy_Match()
    {
        // gap = U_CB − E0: ON wenn E0 weit ueber der CB-Kante (gap << 0),
        // 50 % bei Energiegleichstand (gap = 0), OFF wenn E0 darunter.
        double onFrac  = SensorPhysics.PetQuenchFraction(1.00, 4.9);  // gap = -0.54
        double midFrac = SensorPhysics.PetQuenchFraction(0.46, 4.9);  // gap = 0
        double offFrac = SensorPhysics.PetQuenchFraction(-0.10, 4.9); // gap = +0.56
        Assert.True(onFrac > 0.98, $"ON-Fraktion {onFrac:F4}");
        Assert.Equal(0.5, midFrac, 6);
        Assert.True(offFrac < 0.02, $"OFF-Fraktion {offFrac:F4}");
    }
}

/// <summary>
/// Tests fuer die JSON-gestuetzte Liganden-/Analyten-Datenbank.
/// </summary>
public class SensorDatabaseTests
{
    [Fact]
    public void Ligands_Are_Loaded_From_Deployed_Json()
    {
        Assert.NotEmpty(SensorDatabase.Ligands);
        Assert.NotNull(SensorDatabase.LigandsLoadedFrom);
        Assert.True(File.Exists(SensorDatabase.LigandsLoadedFrom!), "JSON muss physisch existieren");

        var mpa = SensorDatabase.Ligands.First(l => l.LigandId == "MPA");
        Assert.Equal("S", mpa.AnchorAtom);
        Assert.Equal(0.6, mpa.Length_nm, 6);
        Assert.Contains("Cd", mpa.Targets);
    }

    [Fact]
    public void Analytes_Are_Loaded_From_Deployed_Json()
    {
        Assert.NotEmpty(SensorDatabase.Analytes);
        Assert.NotNull(SensorDatabase.AnalytesLoadedFrom);

        var pb = SensorDatabase.Analytes.First(a => a.AnalyteId == "Pb2+");
        Assert.Equal("Quenching", pb.Mode);
        Assert.Equal(4.2e5, pb.SternVolmerConstant_M, 2);

        var fluo = SensorDatabase.Analytes.First(a => a.AnalyteId == "Fluorescein");
        Assert.Equal(5.0, fluo.ForsterRadius_nm, 6);
    }

    [Fact]
    public void Every_Analyte_Has_A_Known_Mode()
    {
        var known = new[] { "FRET", "Quenching", "Charge", "PET" };
        Assert.All(SensorDatabase.Analytes, a => Assert.Contains(a.Mode, known));
    }

    [Fact]
    public void Ligand_Anchor_Must_Target_QD_Cation()
    {
        // MPA bindet an Cd — ein CdSe-QD muss also funktionalisierbar sein.
        var mpa = SensorDatabase.Ligands.First(l => l.LigandId == "MPA");
        var cdse = MaterialDatabase.Defaults.First(m => m.Name == "CdSe");
        Assert.Contains(cdse.Cation, mpa.Targets);
    }
}