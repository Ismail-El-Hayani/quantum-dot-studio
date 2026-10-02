using System.Windows.Media;
using QuantumDotStudio.Core.Models;

namespace QuantumDotStudio.Renderer;

/// <summary>
/// Farbschema für Atome in der 3D-Visualisierung.
/// </summary>
public static class AtomPalette
{
    private static readonly Dictionary<string, Color> ElementColors = new(StringComparer.OrdinalIgnoreCase)
    {
        ["Cd"] = Color.FromRgb(255, 215, 0),    // Cadmium: kräftiges Gold
        ["Se"] = Color.FromRgb(255, 80, 0),     // Selen: tiefes Orange-Rot
        ["In"] = Color.FromRgb(180, 130, 120),  // Indium: warmes Graubraun
        ["P"]  = Color.FromRgb(255, 200, 50),   // Phosphor: hellgelb
        ["Pb"] = Color.FromRgb(70, 75, 85),     // Blei: dunkles Stahlblau-Grau
        ["S"]  = Color.FromRgb(220, 255, 0),    // Schwefel: giftiges Neongelb
        ["Zn"] = Color.FromRgb(160, 165, 175)   // Zink: kühles Hellgrau
    };

    /// <summary>
    /// Gibt die Farbe für ein Element-Symbol zurück.
    /// </summary>
    public static Color GetColorByElement(string element)
    {
        if (string.IsNullOrEmpty(element))
            return Colors.Gray;

        return ElementColors.TryGetValue(element, out var color)
            ? color
            : Colors.LightGray;
    }

    /// <summary>
    /// Gibt die Farbe für ein Atom zurück.
    /// </summary>
    public static Color GetColor(Atom atom)
    {
        return GetColorByElement(atom.Element);
    }

    /// <summary>
    /// Gibt den SolidColorBrush für Helix Toolkit zurück.
    /// </summary>
    public static Brush GetBrush(Atom atom)
    {
        return new SolidColorBrush(GetColor(atom));
    }

    /// <summary>
    /// Liste der bekannten Elemente mit Farbe für UI-Legenden.
    /// </summary>
    public static IReadOnlyDictionary<string, Color> KnownColors =
        new Dictionary<string, Color>(ElementColors, StringComparer.OrdinalIgnoreCase);
}
