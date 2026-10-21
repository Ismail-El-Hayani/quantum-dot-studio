using QuantumDotStudio.Core.Models;

namespace QuantumDotStudio.Core.Physics;

/// <summary>
/// Ein optischer Übergang zwischen einem gebundenen Elektron- und einem
/// gebundenen Loch-Zustand gleicher Quantenzahlen (Auswahlregel Δn = Δl = 0).
/// </summary>
public sealed record OpticalTransition
{
    /// <summary>
    /// Bezeichnung wie „1S→1S“ (Elektron→Loch, gleiche Quantenzahlen).
    /// </summary>
    public string Label { get; init; } = string.Empty;

    /// <summary>
    /// Übergangsenergie in eV.
    /// </summary>
    public double Energy_eV { get; init; }

    /// <summary>
    /// Wellenlänge des Übergangs in nm.
    /// </summary>
    public double Wavelength_nm { get; init; }

    /// <summary>
    /// Relative Oszillatorstärke, normiert auf den 1S-Übergang = 1:
    /// f ∝ (2l+1) · |⟨ψ_e|ψ_h⟩|² — Entartung über die magnetische
    /// Quantenzahl mal Überlappungsquadrat (1 für gleiche Quantenzahlen
    /// im selben Potentialtopf, 0 nach Orthogonalität sonst).
    /// </summary>
    public double RelativeStrength { get; init; }

    /// <summary>
    /// True für den tiefsten Übergang 1S→1S: exakt auf die effektive
    /// Bandlücke des Modells verankert (inkl. Coulomb-Term der Brus-Formel).
    /// </summary>
    public bool IsGroundState { get; init; }
}

/// <summary>
/// Berechnet die optischen Übergänge eines Quantum Dots aus den gebundenen
/// Elektron- und Loch-Niveaus (Modell der starken Konfinierung):
///
///  - Auswahlregel Δn = Δl = 0: Elektron- und Loch-Hüllfunktionen im selben
///    sphärischen Topf sind orthogonal — nur Zustände gleicher Quantenzahlen
///    (n, l) haben ein von null verschiedenes Überlappungsquadrat.
///  - Übergangsenergie E(n,l) = E_g + E_e(n,l) + E_h(n,l). Der
///    Grundzustandsübergang 1S→1S wird exakt auf die effektive Bandlücke des
///    Modells verankert, damit die Emissionslinie (Photolumineszenz nach
///    Relaxation in den Grundzustand) unverändert mit der Brus-Bandlücke
///    übereinstimmt. Für angeregte Zustände wird die (schwachere) Coulomb-
///    Bindung vernachlässigt — dokumentierte Vereinfachung.
///  - Relative Stärke f ∝ (2l+1): Entartung über die magnetische Quantenzahl.
///
/// Für Core/Shell-Dots (endlicher Potentialtopf, nur S-Zustände) ergeben
/// sich reine S-Übergänge 1S→1S, 2S→2S, 3S→3S aus den endlichen-Topf-Energien.
/// </summary>
public static class OpticalTransitions
{
    /// <summary>
    /// hc in eV·nm, gebildet aus denselben Naturkonstanten wie
    /// QuantumSolver.WavelengthFromBandGap (h·c in J·m / Elementarladung),
    /// damit Übergangs- und Emissionswellenlängen konsistent sind.
    /// (h·c = 1,98644586·10⁻²⁵ J·m, e = 1,602176634·10⁻¹⁹ C.)
    /// </summary>
    private const double Hc_eV_nm = 1.98644586e-25 / 1.602176634e-19 * 1e9;

    /// <summary>
    /// Berechnet die optischen Übergänge, aufsteigend nach Energie.
    /// </summary>
    /// <param name="bulkBandGap_eV">Bulk-Bandlücke des Core-Materials in eV.</param>
    /// <param name="effectiveBandGap_eV">
    /// Effektive Bandlücke des Modells (QuantumDot.TotalBandGap_eV) — Anker
    /// für den 1S-Grundzustandsübergang.
    /// </param>
    /// <param name="levels">Gebundene Niveaus (Elektron und Loch, dot.EnergyLevels).</param>
    /// <param name="maxTransitions">Maximale Anzahl zurückgegebener Übergänge.</param>
    public static List<OpticalTransition> Compute(
        double bulkBandGap_eV,
        double effectiveBandGap_eV,
        IEnumerable<EnergyLevel> levels,
        int maxTransitions = 5)
    {
        if (levels == null || maxTransitions <= 0)
            return new List<OpticalTransition>();

        // Löcher nach (n, l) indizieren; nur Elektron-Niveaus mit Loch-Pendant
        // in denselben Quantenzahlen sind optisch erlaubt (Auswahlregel).
        var holesByNl = levels
            .Where(l => l.Particle == Particle.Hole)
            .GroupBy(h => (h.PrincipalQuantumNumber_n, h.AngularMomentum_l))
            .ToDictionary(g => g.Key, g => g.First());

        var raw = new List<(int n, int l, double energy_eV, int degeneracy)>();
        foreach (var e in levels.Where(l => l.Particle == Particle.Electron))
        {
            if (!holesByNl.TryGetValue((e.PrincipalQuantumNumber_n, e.AngularMomentum_l), out var h))
                continue;

            double energy = bulkBandGap_eV + e.Energy_eV + h.Energy_eV;
            if (double.IsNaN(energy) || energy <= 0)
                continue;

            raw.Add((e.PrincipalQuantumNumber_n, e.AngularMomentum_l, energy, 2 * e.AngularMomentum_l + 1));
        }

        var transitions = new List<OpticalTransition>();
        foreach (var t in raw
                     .OrderBy(r => r.energy_eV)
                     .ThenBy(r => r.l)
                     .ThenBy(r => r.n)
                     .Take(maxTransitions))
        {
            bool isGround = t.n == 1 && t.l == 0;
            double energy = isGround ? effectiveBandGap_eV : t.energy_eV;
            string state = GetSpectroscopicLabel(t.n, t.l);

            transitions.Add(new OpticalTransition
            {
                Label = $"{state}→{state}",
                Energy_eV = energy,
                Wavelength_nm = energy > 0 ? Hc_eV_nm / energy : double.PositiveInfinity,
                RelativeStrength = t.degeneracy,
                IsGroundState = isGround
            });
        }

        return transitions;
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