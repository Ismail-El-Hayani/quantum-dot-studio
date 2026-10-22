using QuantumDotStudio.Core.Physics;

namespace QuantumDotStudio.Tests;

/// <summary>
/// Tests für die Antwortzeit-Abschätzung (Roadmap 4, optional): das Sensorsignal
/// ist durch die langsamere der beiden seriellen Schritte begrenzt — die
/// Diffusion des Analyten zur QD-Oberfläche und die Transduktion selbst.
/// </summary>
public class SensorResponseTimeTests
{
    [Fact]
    public void Diffusion_Time_Follows_L_Squared_Over_D()
    {
        double t = SensorPhysics.DiffusionTime_s();

        // Standard: L = 10 µm, D = 5·10⁻¹⁰ m²/s → 0,2 s
        Assert.Equal(0.2, t, tolerance: 1e-9);
        Assert.Equal(
            SensorPhysics.DiffusionLength_m * SensorPhysics.DiffusionLength_m / SensorPhysics.DiffusionCoefficientWater_m2_per_s,
            t, tolerance: 1e-12);
    }

    [Fact]
    public void Diffusion_Time_Is_Independent_Of_Concentration()
    {
        double a = SensorPhysics.DiffusionTime_s();
        double b = SensorPhysics.DiffusionTime_s();

        Assert.Equal(a, b, tolerance: 0.0);
    }

    [Fact]
    public void Fret_Response_Is_Binding_Limited_At_Low_Concentration()
    {
        // 1 µM FRET: t_rxn = 1/(1e5·1e-6) = 10 s > Diffusionszeit (0,2 s)
        double t = SensorPhysics.ResponseTime_s("FRET", 1e-6);

        Assert.Equal(10.0, t, tolerance: 1e-6);
        Assert.Equal("Reaktion/Bindung", SensorPhysics.ResponseTimeLimit("FRET", 1e-6));
    }

    [Fact]
    public void Fret_Response_Becomes_Diffusion_Limited_At_High_Concentration()
    {
        // 1 mM: t_rxn = 1/(1e5·1e-3) = 0,01 s < Diffusionszeit → Diffusionslimit
        double t = SensorPhysics.ResponseTime_s("FRET", 1e-3);

        Assert.Equal(SensorPhysics.DiffusionTime_s(), t, tolerance: 1e-12);
        Assert.Equal("Diffusion", SensorPhysics.ResponseTimeLimit("FRET", 1e-3));
    }

    [Fact]
    public void Quenching_Responds_Faster_Than_Fret_At_The_Same_Concentration()
    {
        // Quenching/Charge/PET sind diffusionskontrolliert (10¹⁰ M⁻¹s⁻¹) —
        // dort begrenzt die Diffusion, nicht die Bindung.
        double fret = SensorPhysics.ResponseTime_s("FRET", 1e-6);
        double quenching = SensorPhysics.ResponseTime_s("Quenching", 1e-6);

        Assert.True(quenching < fret, $"Quenching {quenching:F4} s sollte schneller sein als FRET {fret:F4} s");
        Assert.Equal("Diffusion", SensorPhysics.ResponseTimeLimit("Quenching", 1e-6));
    }

    [Fact]
    public void Response_Time_Does_Not_Grow_With_Concentration()
    {
        double low = SensorPhysics.ResponseTime_s("FRET", 1e-6);
        double high = SensorPhysics.ResponseTime_s("FRET", 1e-3);

        Assert.True(high < low, $"höhere Konzentration muss schneller sein ({high:F4} s < {low:F4} s)");
    }

    [Fact]
    public void Zero_Concentration_Never_Responds()
    {
        // Bei [A] = 0 gibt es kein Signal: die Antwortzeit ist unendlich —
        // NICHT das Diffusionslimit (es findet gar keine Umschaltung statt).
        double t = SensorPhysics.ResponseTime_s("FRET", 0.0);

        Assert.True(double.IsInfinity(t));
        Assert.False(double.IsNaN(t));
    }
}