using System.Numerics;
using System.Windows;
using System.Windows.Media;
using System.Windows.Media.Media3D;
using HelixToolkit.Geometry;
using HelixToolkit.Wpf;
using QuantumDotStudio.Core.Models;

namespace QuantumDotStudio.Renderer;

/// <summary>
/// Erzeugt aus einem Quantum Dot eine Helix-Toolkit-Model3D-Gruppe.
/// Skaliert das Modell so, dass es unabhängig vom physikalischen Radius
/// immer den gleichen sichtbaren Bereich im Viewport füllt.
/// </summary>
public class QuantumDotRenderer3D
{
    /// <summary>
    /// Skalierungsfaktor: wie viele 3D-Einheiten ein physikalischer Nanometer einnimmt.
    /// Höherer Wert = größer dargestelltes Quantum Dot.
    /// </summary>
    public double UnitsPerNanometer { get; set; } = 0.40;

    /// <summary>
    /// Atomradius relativ zum Gitterabstand. Sorgt für überlappende Kugeln.
    /// </summary>
    public double AtomScale { get; set; } = 0.55;

    /// <summary>
    /// Punktgröße für Wahrscheinlichkeitswolken relativ zum Gitterabstand.
    /// </summary>
    public double CloudPointSize { get; set; } = 0.25;

    /// <summary>
    /// Erzeugt eine Model3DGroup mit Atomen und optionaler Wahrscheinlichkeitswolke.
    /// Das Modell wird so skaliert, dass der gewählte Radius konstant aussieht,
    /// während die atomare Dichte mit dem Radius zunimmt.
    /// Das Modell wird zusätzlich um seinen Schwerpunkt zentriert.
    /// </summary>
    public Model3DGroup BuildModel(QuantumDot dot, bool showLattice = true, bool showCloud = true)
    {
        var group = new Model3DGroup();

        if (dot == null || dot.Radius_nm <= 0)
            return group;

        // Schwerpunkt des Atomgitters berechnen, um das Modell zu zentrieren.
        System.Numerics.Vector3 centroid = System.Numerics.Vector3.Zero;
        if (dot.Atoms != null && dot.Atoms.Count > 0)
        {
            double cx = dot.Atoms.Average(a => a.Position.X);
            double cy = dot.Atoms.Average(a => a.Position.Y);
            double cz = dot.Atoms.Average(a => a.Position.Z);
            centroid = new System.Numerics.Vector3((float)cx, (float)cy, (float)cz);
        }
        else if (dot.ElectronCloud != null && dot.ElectronCloud.Count > 0)
        {
            double cx = dot.ElectronCloud.Average(p => p.Position.X);
            double cy = dot.ElectronCloud.Average(p => p.Position.Y);
            double cz = dot.ElectronCloud.Average(p => p.Position.Z);
            centroid = new System.Numerics.Vector3((float)cx, (float)cy, (float)cz);
        }

        // Skalierungsfaktor: konstante Einheiten pro Nanometer, damit größere Radien auch visuell größer werden.
        double visualScale = UnitsPerNanometer;

        if (showLattice && dot.Atoms != null && dot.Atoms.Count > 0)
        {
            foreach (var atom in dot.Atoms)
            {
                var center = new System.Numerics.Vector3(
                    (float)((atom.Position.X - centroid.X) * visualScale),
                    (float)((atom.Position.Y - centroid.Y) * visualScale),
                    (float)((atom.Position.Z - centroid.Z) * visualScale));
                var radius = (float)(atom.Radius_nm * AtomScale * visualScale);
                var color = AtomPalette.GetColor(atom);

                var meshBuilder = new MeshBuilder();
                meshBuilder.AddSphere(center, radius, 14, 12);

                var geometry = ConvertToWpf(meshBuilder.ToMesh());
                var material = MaterialHelper.CreateMaterial(color);

                var model = new GeometryModel3D(geometry, material)
                {
                    BackMaterial = material
                };

                group.Children.Add(model);
            }
        }

        if (showCloud && dot.ElectronCloud != null && dot.ElectronCloud.Count > 0)
        {
            group.Children.Add(BuildProbabilityCloud(dot.ElectronCloud, centroid, visualScale));
        }

        return group;
    }

    /// <summary>
    /// Gibt eine BoundingBox für das gesamte Modell zurück.
    /// Die Größe hängt vom aktuellen Radius ab, damit ZoomExtents passend arbeitet.
    /// </summary>
    public Rect3D GetBounds(QuantumDot dot)
    {
        if (dot == null || dot.Radius_nm <= 0)
            return new Rect3D(-2, -2, -2, 4, 4, 4);

        double half = dot.Radius_nm * UnitsPerNanometer * 1.3;
        return new Rect3D(-half, -half, -half, half * 2, half * 2, half * 2);
    }

    private static System.Windows.Media.Media3D.MeshGeometry3D ConvertToWpf(HelixToolkit.Geometry.MeshGeometry3D source)
    {
        var wpf = new System.Windows.Media.Media3D.MeshGeometry3D();
        foreach (var p in source.Positions)
            wpf.Positions.Add(new Point3D(p.X, p.Y, p.Z));
        if (source.Normals != null)
        {
            foreach (var n in source.Normals)
                wpf.Normals.Add(new Vector3D(n.X, n.Y, n.Z));
        }
        if (source.TextureCoordinates != null)
        {
            foreach (var tc in source.TextureCoordinates)
                wpf.TextureCoordinates.Add(new Point(tc.X, tc.Y));
        }
        foreach (var idx in source.TriangleIndices)
            wpf.TriangleIndices.Add(idx);
        return wpf;
    }

    private GeometryModel3D BuildProbabilityCloud(List<(System.Numerics.Vector3 Position, float Probability)> cloud, System.Numerics.Vector3 centroid, double visualScale)
    {
        if (cloud.Count == 0)
        {
            var empty = new System.Windows.Media.Media3D.MeshGeometry3D();
            return new GeometryModel3D(empty, null);
        }

        var meshBuilder = new MeshBuilder();
        var baseRadius = (float)(CloudPointSize * visualScale);

        foreach (var point in cloud)
        {
            var pos = new System.Numerics.Vector3(
                (float)((point.Position.X - centroid.X) * visualScale),
                (float)((point.Position.Y - centroid.Y) * visualScale),
                (float)((point.Position.Z - centroid.Z) * visualScale));
            var r = baseRadius * (0.5f + 0.5f * point.Probability);
            meshBuilder.AddSphere(pos, r, 8, 6);
        }

        var geometry = ConvertToWpf(meshBuilder.ToMesh());
        var color = Color.FromArgb(120, 30, 144, 255); // halbtransparentes Dodger-Blau
        var material = MaterialHelper.CreateMaterial(color, 0.45);

        return new GeometryModel3D(geometry, material)
        {
            BackMaterial = material
        };
    }
}
