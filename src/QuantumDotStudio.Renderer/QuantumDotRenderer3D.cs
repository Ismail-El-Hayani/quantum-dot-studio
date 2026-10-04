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
    /// Erzeugt eine Model3DGroup mit Atomen (mesh-merged pro Element) und
    /// optionalen Wahrscheinlichkeitswolken (1S blau, 1P dunkelrot).
    /// Das Modell wird um seinen Schwerpunkt zentriert.
    /// </summary>
    public Model3DGroup BuildModel(QuantumDot dot, bool showLattice = true, bool showCloud = true, bool showCloud1P = false)
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
            // Mesh-Merging: alle Atome eines Elements werden in EIN Mesh zusammengefasst.
            // Statt tausender einzelner GeometryModel3D-Objekte (Draw-Call-Overhead,
            // UI-Freeze bei großen Radien) entstehen nur so viele Kinder wie Elemente.
            // Tessellation wird bei dichten Gittern stark reduziert (LOD): Atome sind
            // dort ohnehin kleiner als ein Pixel — die Kugel-Vertexzahl dominiert
            // Bauzeit und Speicher des Meshes.
            int tessU, tessV;
            if (dot.Atoms.Count > 40000)      { tessU = 4; tessV = 3; }
            else if (dot.Atoms.Count > 10000) { tessU = 6; tessV = 5; }
            else                              { tessU = 10; tessV = 8; }

            foreach (var elementGroup in dot.Atoms.GroupBy(a => a.Element, StringComparer.OrdinalIgnoreCase))
            {
                var meshBuilder = new MeshBuilder();
                Color color = AtomPalette.GetColorByElement(elementGroup.Key);
                bool useBoxes = dot.Atoms.Count > 40000;

                foreach (var atom in elementGroup)
                {
                    var center = new System.Numerics.Vector3(
                        (float)((atom.Position.X - centroid.X) * visualScale),
                        (float)((atom.Position.Y - centroid.Y) * visualScale),
                        (float)((atom.Position.Z - centroid.Z) * visualScale));
                    var radius = (float)(atom.Radius_nm * AtomScale * visualScale);

                    if (useBoxes)
                    {
                        // Bei >40k Atomen sind Kugeln unter einem Pixel groß:
                        // Würfel (8 Vertizes statt ~18) sind visuell identisch
                        // und senken die Mesh-Bauzeit drastisch.
                        meshBuilder.AddBox(center, radius, radius, radius);
                    }
                    else
                    {
                        meshBuilder.AddSphere(center, radius, tessU, tessV);
                    }
                }

                var geometry = ConvertToWpf(meshBuilder.ToMesh());
                var material = MaterialHelper.CreateMaterial(color);

                group.Children.Add(new GeometryModel3D(geometry, material)
                {
                    BackMaterial = material
                });
            }
        }

        if (showCloud && dot.ElectronCloud != null && dot.ElectronCloud.Count > 0)
        {
            group.Children.Add(BuildCloudMesh(dot.ElectronCloud, centroid, visualScale,
                Color.FromArgb(120, 30, 144, 255))); // 1S: halbtransparentes Dodger-Blau
        }

        if (showCloud1P && dot.ElectronCloud1P != null && dot.ElectronCloud1P.Count > 0)
        {
            group.Children.Add(BuildCloudMesh(dot.ElectronCloud1P, centroid, visualScale,
                Color.FromArgb(110, 200, 40, 60))); // 1P: halbtransparentes Dunkelrot
        }

        // Freeze: macht die gesamte Szene (Meshes, Materialien, Brushes) unveränderlich
        // und damit thread-übergreifend nutzbar. BuildModel kann so in einem
        // Hintergrund-Task laufen und das fertige Modell gefroren an den UI-Thread
        // übergeben werden — die UI blockiert nie auf dem Mesh-Bau.
        if (group.CanFreeze)
            group.Freeze();

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
        // Collections vorab passend zur Quellgröße anlegen: vermeidet wiederholte
        // interne Reallokationen beim schrittweisen Add von Millionen Vertizes.
        wpf.Positions = new System.Windows.Media.Media3D.Point3DCollection(source.Positions.Count);
        foreach (var p in source.Positions)
            wpf.Positions.Add(new Point3D(p.X, p.Y, p.Z));
        if (source.Normals != null)
        {
            wpf.Normals = new System.Windows.Media.Media3D.Vector3DCollection(source.Normals.Count);
            foreach (var n in source.Normals)
                wpf.Normals.Add(new Vector3D(n.X, n.Y, n.Z));
        }
        // Texturkoordinaten werden bewusst NICHT kopiert: alle Materialien dieses
        // Renderers sind einfarbige SolidBrushes (keine Texturen), die UVs wären
        // reiner Kopier-Overhead (bis zu Millionen Point-Add-Aufrufe bei dichten Gittern).
        wpf.TriangleIndices = new System.Windows.Media.Int32Collection(source.TriangleIndices.Count);
        foreach (var idx in source.TriangleIndices)
            wpf.TriangleIndices.Add(idx);
        return wpf;
    }

    private GeometryModel3D BuildCloudMesh(List<(System.Numerics.Vector3 Position, float Probability)> cloud, System.Numerics.Vector3 centroid, double visualScale, Color color)
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
        var material = MaterialHelper.CreateMaterial(color, 0.45);

        return new GeometryModel3D(geometry, material)
        {
            BackMaterial = material
        };
    }
}
