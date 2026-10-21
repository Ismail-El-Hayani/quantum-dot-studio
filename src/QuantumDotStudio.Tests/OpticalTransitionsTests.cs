using QuantumDotStudio.Core.Data;
using QuantumDotStudio.Core.Models;
using QuantumDotStudio.Core.Physics;
using QuantumDotStudio.Solver;
using QuantumDotStudio.WPF.ViewModels;

namespace QuantumDotStudio.Tests;

/// <summary>
/// Tests für das optische Übergangsmodell (Absorptions-/Emissionsspektrum,
/// FR-007): Auswahlregel Δn = Δl = 0, Entartungs-Oszillatorstärken,
/// Verankerung des Grundzustands an der Brus-Bandlücke und die
/// Core/Shell-S-Zustände des endlichen Topfs.
/// </summary>
public class OpticalTransitionsTests
{
    private static readonly List<Material> Materials = MaterialDatabase.Defaults;
    private static Material Get(string name) => Materials.Single(m => m.Name == name);

    private static readonly QuantumDotService Service = new();

    // ---------- Compute: unendlicher Topf (Service-Integration) ----------

    [Fact]
    public void Ground_Transition_Is_Anchored_To_Effective_Band_Gap()
    {
        // Der 1S-Grundzustandsübergang MUSS exakt die effektive Bandlücke
        // treffen — Emissionslinie und Brus-Formel bleiben deckungsgleich
        // (keine Regression für Sensor- und Machbarkeits-Engine).
        var dot = Service.BuildQuantumDot(Get("CdSe"), 3.0);

        var ground = Assert.Single(dot.OpticalTransitions, t => t.IsGroundState);

        Assert.Equal(dot.TotalBandGap_eV, ground.Energy_eV, tolerance: 1e-12);
        Assert.Equal(dot.EmissionWavelength_nm, ground.Wavelength_nm, tolerance: 1e-6);
    }

    [Fact]
    public void Infinite_Well_Transitions_Follow_Selection_Rule_And_Degeneracy()
    {
        var dot = Service.BuildQuantumDot(Get("CdSe"), 3.0);
        var transitions = dot.OpticalTransitions;

        // Reihenfolge nach Übergangsenergie: sortiert nach α_{n,l}² ist
        // 1S < 1P < 1D < 2S < 2P (9,87 < 20,19 < 33,22 < 39,48 < 59,68).
        Assert.Equal(new[]
        {
            "1S→1S", "1P→1P", "1D→1D", "2S→2S", "2P→2P"
        }, transitions.Select(t => t.Label).ToArray());

        // Relative Oszillatorstärke = (2l+1), 1S normiert auf 1.
        Assert.Equal(new[] { 1.0, 3.0, 5.0, 1.0, 3.0 },
            transitions.Select(t => t.RelativeStrength).ToArray());

        // Auswahlregel Δn = Δl = 0: Label-Hälften identisch.
        Assert.All(transitions, t =>
            Assert.Equal(t.Label.Split('→')[0], t.Label.Split('→')[1]));
    }

    [Fact]
    public void Infinite_Well_Excited_Energy_Matches_Confinement_Sum()
    {
        // Kreuzcheck gegen den Solver: E(1P) = E_g + E_e(1,1) + E_h(1,1).
        var cdSe = Get("CdSe");
        var dot = Service.BuildQuantumDot(cdSe, 3.0);
        var oneP = Assert.Single(dot.OpticalTransitions, t => t.Label == "1P→1P");

        double expected = cdSe.BandGap_eV
            + QuantumSolver.ConfinementEnergy(3.0, cdSe.EffectiveMassElectron, 1, 1)
            + QuantumSolver.ConfinementEnergy(3.0, cdSe.EffectiveMassHole, 1, 1);

        Assert.Equal(expected, oneP.Energy_eV, tolerance: 1e-12);
    }

    [Fact]
    public void Transitions_Are_Energy_Sorted_With_Decreasing_Wavelength()
    {
        var dot = Service.BuildQuantumDot(Get("CdSe"), 3.0);
        var transitions = dot.OpticalTransitions;

        for (int i = 1; i < transitions.Count; i++)
        {
            Assert.True(transitions[i].Energy_eV > transitions[i - 1].Energy_eV,
                $"E({transitions[i].Label}) sollte größer sein als E({transitions[i - 1].Label})");
            Assert.True(transitions[i].Wavelength_nm < transitions[i - 1].Wavelength_nm);
        }
    }

    // ---------- Compute: endlicher Topf (Core/Shell, Service) ----------

    [Fact]
    public void Shell_Mode_Uses_Finite_Well_S_States()
    {
        // Endlicher Topf liefert nur S-Zustände → reine S-Übergänge,
        // Grundzustand wieder exakt an der effektiven Bandlücke verankert.
        var dot = Service.BuildCoreShellQuantumDot(Get("CdSe"), Get("CdS"), 3.0, 0.6);

        Assert.NotEmpty(dot.OpticalTransitions);
        Assert.All(dot.OpticalTransitions, t =>
            Assert.Matches(@"^\d+S→\d+S$", t.Label));
        Assert.All(dot.OpticalTransitions, t => Assert.Equal(1.0, t.RelativeStrength));

        var ground = Assert.Single(dot.OpticalTransitions, t => t.IsGroundState);
        Assert.Equal(dot.TotalBandGap_eV, ground.Energy_eV, tolerance: 1e-12);
    }

    // ---------- Compute: reine Funktion ----------

    [Fact]
    public void Compute_With_Explicit_Levels_Sorts_And_Anchors()
    {
        var levels = new List<EnergyLevel>
        {
            new() { PrincipalQuantumNumber_n = 1, AngularMomentum_l = 0, Energy_eV = 0.10, Label = "1S", Particle = Particle.Electron },
            new() { PrincipalQuantumNumber_n = 2, AngularMomentum_l = 0, Energy_eV = 0.40, Label = "2S", Particle = Particle.Electron },
            new() { PrincipalQuantumNumber_n = 3, AngularMomentum_l = 0, Energy_eV = 0.90, Label = "3S", Particle = Particle.Electron },
            new() { PrincipalQuantumNumber_n = 1, AngularMomentum_l = 0, Energy_eV = 0.05, Label = "1S", Particle = Particle.Hole },
            new() { PrincipalQuantumNumber_n = 2, AngularMomentum_l = 0, Energy_eV = 0.20, Label = "2S", Particle = Particle.Hole },
            new() { PrincipalQuantumNumber_n = 3, AngularMomentum_l = 0, Energy_eV = 0.50, Label = "3S", Particle = Particle.Hole },
        };

        var transitions = OpticalTransitions.Compute(1.74, 2.07, levels);

        Assert.Equal(new[] { "1S→1S", "2S→2S", "3S→3S" }, transitions.Select(t => t.Label).ToArray());

        // Grundzustand: Anker auf die effektive Bandlücke (nicht die Rohsumme 1,89).
        Assert.Equal(2.07, transitions[0].Energy_eV, tolerance: 1e-12);
        Assert.True(transitions[0].IsGroundState);

        // Angeregte Zustände: reine Summen ohne Bindung.
        Assert.Equal(1.74 + 0.40 + 0.20, transitions[1].Energy_eV, tolerance: 1e-12);
        Assert.Equal(1.74 + 0.90 + 0.50, transitions[2].Energy_eV, tolerance: 1e-12);
        Assert.All(transitions, t => Assert.Equal(1.0, t.RelativeStrength));
    }

    [Fact]
    public void Compute_Skips_Electron_States_Without_Hole_Partner()
    {
        // Auswahlregel: Elektron 1S ohne Loch-1S-Pendant (hier Δl = 1) ist verboten.
        var levels = new List<EnergyLevel>
        {
            new() { PrincipalQuantumNumber_n = 1, AngularMomentum_l = 0, Energy_eV = 0.1, Particle = Particle.Electron },
            new() { PrincipalQuantumNumber_n = 1, AngularMomentum_l = 1, Energy_eV = 0.05, Particle = Particle.Hole },
        };

        Assert.Empty(OpticalTransitions.Compute(1.74, 2.07, levels));
    }

    [Fact]
    public void Compute_Empty_Levels_Returns_Empty_List()
    {
        Assert.Empty(OpticalTransitions.Compute(1.74, 2.07, Enumerable.Empty<EnergyLevel>()));
    }

    [Fact]
    public void Compute_Respects_Max_Transitions()
    {
        var levels = new List<EnergyLevel>
        {
            new() { PrincipalQuantumNumber_n = 1, AngularMomentum_l = 0, Energy_eV = 0.10, Particle = Particle.Electron },
            new() { PrincipalQuantumNumber_n = 2, AngularMomentum_l = 0, Energy_eV = 0.40, Particle = Particle.Electron },
            new() { PrincipalQuantumNumber_n = 3, AngularMomentum_l = 0, Energy_eV = 0.90, Particle = Particle.Electron },
            new() { PrincipalQuantumNumber_n = 1, AngularMomentum_l = 0, Energy_eV = 0.05, Particle = Particle.Hole },
            new() { PrincipalQuantumNumber_n = 2, AngularMomentum_l = 0, Energy_eV = 0.20, Particle = Particle.Hole },
            new() { PrincipalQuantumNumber_n = 3, AngularMomentum_l = 0, Energy_eV = 0.50, Particle = Particle.Hole },
        };

        var transitions = OpticalTransitions.Compute(1.74, 2.07, levels, maxTransitions: 2);

        Assert.Equal(new[] { "1S→1S", "2S→2S" }, transitions.Select(t => t.Label).ToArray());
    }

    // ---------- PlotFactory: Spektrum-Plot ----------

    [Fact]
    public void Spectrum_Plot_Shows_Absorption_And_Emission()
    {
        var dot = Service.BuildQuantumDot(Get("CdSe"), 3.0);
        var model = PlotFactory.CreateSpectrumPlot(dot);

        Assert.Equal(2, model.Series.Count);
        Assert.Contains(model.Series, s => s.Title.StartsWith("Absorption"));
        Assert.Contains(model.Series, s => s.Title.StartsWith("Emission"));

        var absorption = model.Series.OfType<OxyPlot.Series.LineSeries>()
            .Single(s => s.Title.StartsWith("Absorption"));
        Assert.True(absorption.Points.Count >= 300,
            $"Absorptionsreihe hat nur {absorption.Points.Count} Punkte");
    }

    [Fact]
    public void Spectrum_Plot_Without_Transitions_Shows_Emission_Only()
    {
        var dot = new QuantumDot { EmissionWavelength_nm = 600.0 };
        var model = PlotFactory.CreateSpectrumPlot(dot);

        Assert.Single(model.Series);
        Assert.StartsWith("Emission", model.Series[0].Title);
        Assert.Single(model.Annotations.OfType<OxyPlot.Annotations.LineAnnotation>());
    }
}