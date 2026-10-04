using System.Diagnostics;
using System.Windows;
using System.Windows.Media;
using System.Windows.Media.Media3D;
using HelixToolkit.Geometry;
using HelixToolkit.Wpf;
using QuantumDotStudio.Core.Data;
using QuantumDotStudio.Core.Models;
using QuantumDotStudio.Renderer;
using QuantumDotStudio.Solver;

namespace QuantumDotStudio.Bench;

/// <summary>
/// Render-Benchmark: misst BuildModel-Zeiten für verschiedene Radien.
/// Läuft headless (kein Fenster) — die Mesh-Erzeugung ist der teure Teil
/// und identisch zur In-App-Render-Pipeline.
/// Aufruf: dotnet run --project src/QuantumDotStudio.Bench
/// </summary>
public static class Program
{
    public static void Main()
    {
        Console.WriteLine("QuantumDotStudio Render-Benchmark (BuildModel, mesh-merged)");
        Console.WriteLine("------------------------------------------------------------");

        var service = new QuantumDotService();
        var renderer = new QuantumDotRenderer3D();
        var cdSe = MaterialDatabase.Defaults.Single(m => m.Name == "CdSe");

        // Wolken-Checkboxen aus: isoliert das Atomgitter-Rendering.
        foreach (double radius in new[] { 3.0, 5.0, 8.0, 10.0 })
        {
            QuantumDot dot = service.BuildQuantumDot(cdSe, radius);

            // Warmup (JIT + erste Mesh-Allokationen)
            renderer.BuildModel(dot, showLattice: true, showCloud: false, showCloud1P: false);

            const int runs = 5;
            var times = new List<long>();
            Model3DCount? last = null;
            for (int i = 0; i < runs; i++)
            {
                var sw = Stopwatch.StartNew();
                var model = renderer.BuildModel(dot, showLattice: true, showCloud: false, showCloud1P: false);
                sw.Stop();
                times.Add(sw.ElapsedMilliseconds);
                last = new Model3DCount(model.Children.Count, dot.Atoms.Count);
            }

            times.Sort();
            long median = times[times.Count / 2];
            Console.WriteLine($"R={radius,4:F1} nm | {last!.Atoms,8:N0} Atome | {last.Children,3} Kinder | Median {median,5} ms (n={runs})");
        }

        Console.WriteLine();
        Console.WriteLine("Vergleich Alt-Renderer (1 Kind pro Atom, Konstruktion):");
        Console.WriteLine("------------------------------------------------------------");

        // Alt-Renderer nachbauen: ein GeometryModel3D pro Atom. Nur kleine Radien —
        // der Aufwand skaliert linear mit der Atomzahl (10 nm: ~150k Objekte).
        foreach (double radius in new[] { 3.0, 5.0 })
        {
            QuantumDot dot = service.BuildQuantumDot(cdSe, radius);

            // Warmup
            BuildLegacy(dot);

            const int runs = 3;
            var times = new List<long>();
            for (int i = 0; i < runs; i++)
            {
                var sw = Stopwatch.StartNew();
                var group = BuildLegacy(dot);
                sw.Stop();
                times.Add(sw.ElapsedMilliseconds);
            }

            times.Sort();
            long median = times[times.Count / 2];
            double extrapolated = median * (151_301.0 / dot.Atoms.Count);
            Console.WriteLine($"R={radius,4:F1} nm | {dot.Atoms.Count,8:N0} Atome | {median,8:N0} ms | auf 10 nm extrapoliert: ~{extrapolated / 1000:F0} s (Konstruktion allein, ohne Rendering)");
        }

        Console.WriteLine();
        Console.WriteLine("Hinweis: Alt-Renderer erzeugte zusaetzlich ~150k Draw-Calls beim Rendern;");
        Console.WriteLine("der neue Renderer zeichnet 2 Kinder. Mesh-Bau erfolgt jetzt zudem im");
        Console.WriteLine("Hintergrund-Thread (Freeze()), der UI-Thread blockiert nie.");
    }

    /// <summary>
    /// Rekonstruiert den Alt-Renderer: ein GeometryModel3D pro Atom in einer Gruppe.
    /// </summary>
    private static System.Windows.Media.Media3D.Model3DGroup BuildLegacy(QuantumDot dot)
    {
        var group = new System.Windows.Media.Media3D.Model3DGroup();
        var builder = new MeshBuilder();
        foreach (var atom in dot.Atoms)
        {
            var center = new System.Numerics.Vector3(
                (float)atom.Position.X, (float)atom.Position.Y, (float)atom.Position.Z);
            builder.AddSphere(center, (float)(atom.Radius_nm * 2.5), 10, 8);
            var mesh = builder.ToMesh();
            builder = new MeshBuilder();
            var geometry = new System.Windows.Media.Media3D.MeshGeometry3D();
            foreach (var p in mesh.Positions)
                geometry.Positions.Add(new Point3D(p.X, p.Y, p.Z));
            foreach (var n in mesh.Normals)
                geometry.Normals.Add(new Vector3D(n.X, n.Y, n.Z));
            foreach (var idx in mesh.TriangleIndices)
                geometry.TriangleIndices.Add(idx);
            var material = MaterialHelper.CreateMaterial(Colors.Gray, 0.9);
            group.Children.Add(new System.Windows.Media.Media3D.GeometryModel3D(geometry, material));
        }
        return group;
    }

    private sealed record Model3DCount(int Children, int Atoms);
}