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
/// </summary>
public class QuantumDotRenderer3D
{
    /// <summary>
    /// Atomskalierungsfaktor für die 3D-Darstellung.
    /// </summary>
    public double AtomScale { get; set; } = 0.30;

    /// <summary>
    /// Punktgröße für Wahrscheinlichkeitswolken.
    /// </summary>
    public double CloudPointSize { get; set; } = 0.08;

    /// <summary>
    /// Zusätzlicher Rand um das Gitter für ZoomExtents (in nm).
    /// </summary>
    public double BoundsPadding_nm { get; set; } = 1.0;

    /// <summary>
    /// Erzeugt eine Model3DGroup mit Atomen und optionaler Wahrscheinlichkeitswolke.
    /// </summary>
    public Model3DGroup BuildModel(QuantumDot dot, bool showLattice = true, bool showCloud = true)
    {
        var group = new Model3DGroup();

        if (showLattice && dot?.Atoms != null)
        {
            foreach (var atom in dot.Atoms)
            {
                var center = new System.Numerics.Vector3(
                    (float)atom.Position.X,
                    (float)atom.Position.Y,
                    (float)atom.Position.Z);
                var radius = (float)(atom.Radius_nm * AtomScale);
                var color = AtomPalette.GetColor(atom);

                var meshBuilder = new MeshBuilder();
                meshBuilder.AddSphere(center, radius, 12, 10);

                var geometry = ConvertToWpf(meshBuilder.ToMesh());
                var material = MaterialHelper.CreateMaterial(color);

                var model = new GeometryModel3D(geometry, material)
                {
                    BackMaterial = material
                };

                group.Children.Add(model);
            }
        }

        if (showCloud && dot?.ElectronCloud != null)
        {
            group.Children.Add(BuildProbabilityCloud(dot.ElectronCloud));
        }

        return group;
    }

    /// <summary>
    /// Gibt eine BoundingBox für das gesamte Atomgitter zurück, um die Kamera zu zentrieren.
    /// Berücksichtigt den Atomradius und einen optionalen Padding-Rand.
    /// </summary>
    public Rect3D GetBounds(QuantumDot dot)
    {
        if (dot?.Atoms == null || dot.Atoms.Count == 0)
            return new Rect3D(0, 0, 0, 1, 1, 1);

        double maxAtomRadius = dot.Atoms.Max(a => a.Radius_nm * AtomScale);
        double pad = maxAtomRadius + BoundsPadding_nm;

        double minX = dot.Atoms.Min(a => a.Position.X) - pad;
        double maxX = dot.Atoms.Max(a => a.Position.X) + pad;
        double minY = dot.Atoms.Min(a => a.Position.Y) - pad;
        double maxY = dot.Atoms.Max(a => a.Position.Y) + pad;
        double minZ = dot.Atoms.Min(a => a.Position.Z) - pad;
        double maxZ = dot.Atoms.Max(a => a.Position.Z) + pad;

        return new Rect3D(minX, minY, minZ, maxX - minX, maxY - minY, maxZ - minZ);
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

    private GeometryModel3D BuildProbabilityCloud(List<(System.Numerics.Vector3 Position, float Probability)> cloud)
    {
        if (cloud.Count == 0)
        {
            var empty = new System.Windows.Media.Media3D.MeshGeometry3D();
            return new GeometryModel3D(empty, null);
        }

        var meshBuilder = new MeshBuilder();
        var baseRadius = (float)CloudPointSize;

        foreach (var point in cloud)
        {
            // Punktgröße proportional zur Wahrscheinlichkeit
            var r = baseRadius * (0.5f + 0.5f * point.Probability);
            meshBuilder.AddSphere(point.Position, r, 6, 5);
        }

        var geometry = ConvertToWpf(meshBuilder.ToMesh());
        var color = Color.FromArgb(140, 30, 144, 255); // halbtransparentes Dodger-Blau
        var material = MaterialHelper.CreateMaterial(color, 0.45);

        return new GeometryModel3D(geometry, material)
        {
            BackMaterial = material
        };
    }
}
