using System.Windows.Media;

namespace QuantumDotStudio.WPF.ViewModels;

/// <summary>
/// Ein Eintrag für die Farblegende der 3D-Visualisierung.
/// </summary>
public class LegendItem
{
    public string Label { get; set; } = string.Empty;
    public Brush Brush { get; set; } = Brushes.Gray;
}
