using QuantumDotStudio.Core.Data;
using QuantumDotStudio.Core.Models;
using QuantumDotStudio.Solver;

namespace QuantumDotStudio.Tests;

public class QuantumSolverTests
{
    private static readonly Material CdSe = MaterialDatabase.Defaults.Single(m => m.Name == "CdSe");

    [Fact]
    public void CdSe_3nm_BandGap_Is_Around_2_1_eV()
    {
        // Volle Brus-Gleichung inkl. Coulomb-Term. Literaturvergleich (Yu et al.,
        // Chem. Mater. 15 (2003), Sizing-Curve für CdSe): Partikel mit 6 nm
        // Durchmesser (R = 3 nm) emittieren bei ca. 2,0–2,1 eV.
        double bandGap = QuantumSolver.BrusBandGap(CdSe, 3.0);

        Assert.InRange(bandGap, 2.0, 2.2);
    }

    [Theory]
    [InlineData(1.0, 5.23, 0.05)]
    [InlineData(2.0, 2.55, 0.05)]
    [InlineData(3.0, 2.07, 0.05)]
    [InlineData(5.0, 1.84, 0.05)]
    [InlineData(10.0, 1.75, 0.05)]
    public void CdSe_BandGap_Follows_Infinite_Well_Model(double radius_nm, double expectedBandGap_eV, double tolerance)
    {
        double bandGap = QuantumSolver.BrusBandGap(CdSe, radius_nm);

        Assert.Equal(expectedBandGap_eV, bandGap, tolerance: tolerance);
    }

    [Fact]
    public void CoulombEnergy_Is_Negative_And_Decreases_With_Radius()
    {
        double coulombSmall = QuantumSolver.CoulombEnergy(1.0, CdSe.DielectricConstant);
        double coulombLarge = QuantumSolver.CoulombEnergy(5.0, CdSe.DielectricConstant);

        Assert.True(coulombSmall < 0);
        Assert.True(coulombLarge < 0);
        // |E_C| ~ 1/R: kleinerer Radius bedeutet stärkere Anziehung.
        Assert.True(Math.Abs(coulombSmall) > Math.Abs(coulombLarge));
    }

    [Fact]
    public void CoulombEnergy_Invalid_Input_Returns_Zero()
    {
        Assert.Equal(0.0, QuantumSolver.CoulombEnergy(0.0, CdSe.DielectricConstant));
        Assert.Equal(0.0, QuantumSolver.CoulombEnergy(-1.0, CdSe.DielectricConstant));
        Assert.Equal(0.0, QuantumSolver.CoulombEnergy(3.0, 0.0));
    }

    [Fact]
    public void BrusBandGap_Is_Lower_Than_Confinement_Only()
    {
        double confinementOnly = CdSe.BandGap_eV
            + QuantumSolver.ConfinementEnergy(3.0, CdSe.EffectiveMassElectron, 1, 0)
            + QuantumSolver.ConfinementEnergy(3.0, CdSe.EffectiveMassHole, 1, 0);
        double withCoulomb = QuantumSolver.BrusBandGap(CdSe, 3.0);

        Assert.True(withCoulomb < confinementOnly);
        // Differenz entspricht genau dem Coulomb-Term.
        Assert.Equal(QuantumSolver.CoulombEnergy(3.0, CdSe.DielectricConstant),
            withCoulomb - confinementOnly, tolerance: 1e-12);
    }

    [Fact]
    public void WavelengthFromBandGap_CdSe_3nm_Is_In_Visible_Range()
    {
        double bandGap = QuantumSolver.BrusBandGap(CdSe, 3.0);
        double lambda = QuantumSolver.WavelengthFromBandGap(bandGap);

        Assert.InRange(lambda, 380.0, 750.0);
    }

    [Fact]
    public void ConfinementEnergy_Increases_With_Smaller_Radius()
    {
        double small = QuantumSolver.ConfinementEnergy(1.0, CdSe.EffectiveMassElectron, 1, 0);
        double large = QuantumSolver.ConfinementEnergy(5.0, CdSe.EffectiveMassElectron, 1, 0);

        Assert.True(small > large);
    }

    [Fact]
    public void Ground_State_Label_Is_1S()
    {
        var levels = QuantumSolver.CalculateEnergyLevels(3.0, CdSe.EffectiveMassElectron, maxN: 1, maxL: 0);

        Assert.Single(levels);
        Assert.Equal("1S", levels[0].Label);
    }
}