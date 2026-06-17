using QuantumDotStudio.Core.Data;
using QuantumDotStudio.Core.Models;
using QuantumDotStudio.Solver;

namespace QuantumDotStudio.Tests;

public class QuantumSolverTests
{
    private static readonly Material CdSe = MaterialDatabase.Defaults.Single(m => m.Name == "CdSe");

    [Fact]
    public void CdSe_3nm_BandGap_Is_Around_2_3_eV()
    {
        double bandGap = QuantumSolver.BrusBandGap(CdSe, 3.0);

        Assert.InRange(bandGap, 2.0, 2.6);
    }

    [Theory]
    [InlineData(1.0, 5.47, 0.2)]
    [InlineData(2.0, 2.67, 0.15)]
    [InlineData(3.0, 2.16, 0.15)]
    [InlineData(5.0, 1.90, 0.15)]
    [InlineData(10.0, 1.80, 0.15)]
    public void CdSe_BandGap_Follows_Infinite_Well_Model(double radius_nm, double expectedBandGap_eV, double tolerance)
    {
        double bandGap = QuantumSolver.BrusBandGap(CdSe, radius_nm);

        Assert.Equal(expectedBandGap_eV, bandGap, tolerance: tolerance);
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
