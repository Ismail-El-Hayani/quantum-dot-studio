using QuantumDotStudio.Core.Models;
using QuantumDotStudio.Renderer;
using System.Globalization;
using System.Windows.Data;
using System.Windows.Media.Media3D;

namespace QuantumDotStudio.WPF.Converters;

/// <summary>
/// Konvertiert ein Quantum Dot zusammen mit den Anzeige-Flags in ein 3D-Modell.
/// Wird per MultiBinding an den Content eines ModelVisual3D gebunden.
/// </summary>
public class QuantumDotToModel3DConverter : IMultiValueConverter
{
    private readonly QuantumDotRenderer3D _renderer = new();

    public object? Convert(object[] values, Type targetType, object parameter, CultureInfo culture)
    {
        var dot = values.ElementAtOrDefault(0) as QuantumDot;
        var showLattice = values.ElementAtOrDefault(1) as bool? ?? true;
        var showCloud = values.ElementAtOrDefault(2) as bool? ?? true;
        var showCloud1P = values.ElementAtOrDefault(3) as bool? ?? false;

        if (dot == null)
            return new Model3DGroup();

        return _renderer.BuildModel(dot, showLattice, showCloud, showCloud1P);
    }

    public object[] ConvertBack(object value, Type[] targetTypes, object parameter, CultureInfo culture)
    {
        throw new NotSupportedException();
    }
}
