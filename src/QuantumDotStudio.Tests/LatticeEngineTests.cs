using QuantumDotStudio.Solver;

namespace QuantumDotStudio.Tests;

public class LatticeEngineTests
{
    [Fact]
    public void CdSe_3nm_Produces_Reasonable_Number_Of_Atoms()
    {
        var engine = new LatticeEngine();
        var atoms = engine.GenerateZincBlende(6.05, 3.0, "Cd", "Se");

        // CdSe: 8 Atome/Zelle, a=6.05 Å = 0.605 nm, V/Zelle = 0.221 nm^3
        // Dichte ~36 Atome/nm^3. Kugel R=3 nm => V ~113 nm^3, also ~4000 Atome.
        Assert.InRange(atoms.Count, 3500, 4500);
    }

    [Fact]
    public void All_Atoms_Are_Inside_Sphere()
    {
        var engine = new LatticeEngine();
        double radius = 2.5;
        var atoms = engine.GenerateZincBlende(5.86, radius, "In", "P");

        double radiusSquared = radius * radius;
        foreach (var atom in atoms)
        {
            Assert.True(atom.Position.LengthSquared <= radiusSquared + 1e-6,
                $"Atom at {atom.Position} is outside sphere of radius {radius}");
        }
    }

    [Fact]
    public void Larger_Radius_Produces_More_Atoms()
    {
        var engine = new LatticeEngine();
        var small = engine.GenerateZincBlende(6.05, 2.0, "Cd", "Se");
        var large = engine.GenerateZincBlende(6.05, 4.0, "Cd", "Se");

        Assert.True(large.Count > small.Count);
    }

    [Fact]
    public void Cation_And_Anion_Counts_Are_Approximately_Equal()
    {
        var engine = new LatticeEngine();
        var atoms = engine.GenerateZincBlende(6.05, 3.0, "Cd", "Se");

        int cations = atoms.Count(a => a.Element == "Cd");
        int anions = atoms.Count(a => a.Element == "Se");

        // An der Kugeloberfläche kann es kleine Ungleichgewichte geben.
        Assert.InRange(Math.Abs(cations - anions), 0, 100);
    }
}
