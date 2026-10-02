using QuantumDotStudio.Core.Models;

namespace QuantumDotStudio.Solver;

/// <summary>
/// Generiert Zinkblende-Gitterausschnitte in Kugelform für Quantum Dots:
/// homogen oder als Core/Shell-Heterostruktur.
/// </summary>
public class LatticeEngine
{
    /// <summary>
    /// Erzeugt alle Atompositionen innerhalb einer Kugel mit Radius R_nm.
    /// </summary>
    /// <param name="latticeConstant_A">Gitterkonstante in Å.</param>
    /// <param name="radius_nm">Gewünschter Kugelradius in nm.</param>
    /// <param name="cation">Kation-Symbol.</param>
    /// <param name="anion">Anion-Symbol.</param>
    public List<Atom> GenerateZincBlende(double latticeConstant_A, double radius_nm, string cation, string anion)
    {
        double a_nm = latticeConstant_A * 0.1; // Å -> nm
        double radiusSquared = radius_nm * radius_nm;

        // Bestimme Zellenbereich: Kugelradius + eine Reservezelle
        int cells = (int)Math.Ceiling(radius_nm / a_nm) + 1;

        var atoms = new List<Atom>();

        for (int ix = -cells; ix <= cells; ix++)
        {
            for (int iy = -cells; iy <= cells; iy++)
            {
                for (int iz = -cells; iz <= cells; iz++)
                {
                    var cellOrigin = new Vector3(ix * a_nm, iy * a_nm, iz * a_nm);

                    // 4 Kationen pro konventioneller Zinkblende-Zelle
                    var cationOffsets = new[]
                    {
                        new Vector3(0, 0, 0),
                        new Vector3(0, 0.5, 0.5),
                        new Vector3(0.5, 0, 0.5),
                        new Vector3(0.5, 0.5, 0)
                    };

                    // 4 Anionen pro Zelle
                    var anionOffsets = new[]
                    {
                        new Vector3(0.25, 0.25, 0.25),
                        new Vector3(0.25, 0.75, 0.75),
                        new Vector3(0.75, 0.25, 0.75),
                        new Vector3(0.75, 0.75, 0.25)
                    };

                    foreach (var offset in cationOffsets)
                    {
                        var pos = cellOrigin + offset * a_nm;
                        if (pos.LengthSquared <= radiusSquared)
                        {
                            atoms.Add(new Atom { Element = cation, Position = pos, Radius_nm = 0.12 });
                        }
                    }

                    foreach (var offset in anionOffsets)
                    {
                        var pos = cellOrigin + offset * a_nm;
                        if (pos.LengthSquared <= radiusSquared)
                        {
                            atoms.Add(new Atom { Element = anion, Position = pos, Radius_nm = 0.14 });
                        }
                    }
                }
            }
        }

        return atoms;
    }

    /// <summary>
    /// Erzeugt einen Core/Shell-Gitterausschnitt: der innere Bereich
    /// (r &lt;= coreRadius_nm) wird mit Core-Kation/-Anion gefüllt, der Ring
    /// coreRadius_nm &lt; r &lt;= coreRadius_nm + shellThickness_nm mit
    /// Shell-Kation/-Anion. Beide Bereiche nutzen die Gitterkonstante des
    /// Core-Materials (kohärente Näherung — die reale Schale wächst
    /// verspannt auf).
    /// </summary>
    /// <param name="latticeConstant_A">Gitterkonstante des Core-Materials in Å.</param>
    /// <param name="coreRadius_nm">Core-Radius in nm.</param>
    /// <param name="shellThickness_nm">Schalendicke in nm.</param>
    /// <param name="coreCation">Core-Kation-Symbol.</param>
    /// <param name="coreAnion">Core-Anion-Symbol.</param>
    /// <param name="shellCation">Shell-Kation-Symbol.</param>
    /// <param name="shellAnion">Shell-Anion-Symbol.</param>
    public List<Atom> GenerateZincBlendeCoreShell(
        double latticeConstant_A,
        double coreRadius_nm,
        double shellThickness_nm,
        string coreCation, string coreAnion,
        string shellCation, string shellAnion)
    {
        double totalRadius_nm = coreRadius_nm + shellThickness_nm;
        double coreSquared = coreRadius_nm * coreRadius_nm;
        double totalSquared = totalRadius_nm * totalRadius_nm;
        double a_nm = latticeConstant_A * 0.1;

        int cells = (int)Math.Ceiling(totalRadius_nm / a_nm) + 1;
        var atoms = new List<Atom>();

        for (int ix = -cells; ix <= cells; ix++)
        {
            for (int iy = -cells; iy <= cells; iy++)
            {
                for (int iz = -cells; iz <= cells; iz++)
                {
                    var cellOrigin = new Vector3(ix * a_nm, iy * a_nm, iz * a_nm);

                    var cationOffsets = new[]
                    {
                        new Vector3(0, 0, 0),
                        new Vector3(0, 0.5, 0.5),
                        new Vector3(0.5, 0, 0.5),
                        new Vector3(0.5, 0.5, 0)
                    };
                    var anionOffsets = new[]
                    {
                        new Vector3(0.25, 0.25, 0.25),
                        new Vector3(0.25, 0.75, 0.75),
                        new Vector3(0.75, 0.25, 0.75),
                        new Vector3(0.75, 0.75, 0.25)
                    };

                    foreach (var offset in cationOffsets)
                    {
                        var pos = cellOrigin + offset * a_nm;
                        if (pos.LengthSquared <= totalSquared)
                        {
                            bool inCore = pos.LengthSquared <= coreSquared;
                            atoms.Add(new Atom
                            {
                                Element = inCore ? coreCation : shellCation,
                                Position = pos,
                                Radius_nm = 0.12
                            });
                        }
                    }

                    foreach (var offset in anionOffsets)
                    {
                        var pos = cellOrigin + offset * a_nm;
                        if (pos.LengthSquared <= totalSquared)
                        {
                            bool inCore = pos.LengthSquared <= coreSquared;
                            atoms.Add(new Atom
                            {
                                Element = inCore ? coreAnion : shellAnion,
                                Position = pos,
                                Radius_nm = 0.14
                            });
                        }
                    }
                }
            }
        }

        return atoms;
    }
}