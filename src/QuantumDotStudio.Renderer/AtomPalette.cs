using System.Windows.Media;
using System.Windows.Media.Media3D;
using HelixToolkit.Geometry;
using HelixToolkit.Wpf;
using QuantumDotStudio.Core.Models;

namespace QuantumDotStudio.Renderer;

/// <summary>
/// Farbschema für Atome in der 3D-Visualisierung.
/// </summary>
public static class AtomPalette
{
    private static readonly Dictionary<string, Color> ElementColors = new(StringComparer.OrdinalIgnoreCase)
    {
        ["Cd"] = Color.FromRgb(255, 217, 0),   // Cadmium gelb
        ["Se"] = Color.FromRgb(255, 100, 0),   // Selen orange
        ["In"] = Color.FromRgb(166, 117, 115), // Indium graubraun
        ["P"]  = Color.FromRgb(255, 149, 0),   // Phosphor orange-gelb
        ["Pb"] = Color.FromRgb(87, 89, 99),    // Blei dunkelgrau
        ["S"]  = Color.FromRgb(255, 255, 0)    // Schwefel gelb
    };

    /// <summary>
    /// Gibt die Farbe für ein Atom zurück. Unbekannte Elemente werden grau dargestellt.
    /// </summary>
    public static Color GetColor(Atom atom)
    {
        if (string.IsNullOrEmpty(atom.Element))
            return Colors.Gray;

        return ElementColors.TryGetValue(atom.Element, out var color)
            ? color
            : Colors.LightGray;
    }

    /// <summary>
    /// Gibt den VisualBrush/Brush-Material für Helix Toolkit zurück.
    /// </summary>
    public static Brush GetBrush(Atom atom)
    {
        return new SolidColorBrush(GetColor(atom));
    }
}
