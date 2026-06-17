using OxyPlot;
using OxyPlot.Annotations;
using OxyPlot.Axes;
using OxyPlot.Legends;
using OxyPlot.Series;
using QuantumDotStudio.Core.Models;

namespace QuantumDotStudio.WPF.ViewModels;

/// <summary>
/// Erzeugt OxyPlot-Modelle für Energieniveaus und Emissionsspektrum.
/// </summary>
public static class PlotFactory
{
    /// <summary>
    /// Balkendiagramm der Elektronen- und Loch-Energieniveaus.
    /// </summary>
    public static PlotModel CreateEnergyLevelPlot(QuantumDot dot)
    {
        var model = new PlotModel
        {
            Title = "Energieniveaus",
            Background = OxyColors.White
        };

        var categoryAxis = new CategoryAxis { Position = AxisPosition.Bottom, Title = "Zustand" };
        var valueAxis = new LinearAxis { Position = AxisPosition.Left, Title = "Energie (eV)", Minimum = 0 };
        model.Axes.Add(categoryAxis);
        model.Axes.Add(valueAxis);

        if (dot?.EnergyLevels == null || dot.EnergyLevels.Count == 0)
            return model;

        var electronSeries = new BarSeries
        {
            Title = "Elektron",
            FillColor = OxyColors.DodgerBlue,
            StrokeColor = OxyColors.Black,
            StrokeThickness = 1
        };
        var holeSeries = new BarSeries
        {
            Title = "Loch",
            FillColor = OxyColors.Crimson,
            StrokeColor = OxyColors.Black,
            StrokeThickness = 1
        };

        var labels = new List<string>();

        foreach (var level in dot.EnergyLevels.Take(12))
        {
            bool isElectron = level.Label.StartsWith("e-");
            string baseLabel = level.Label.Replace("e-", "").Replace("h-", "");
            if (!labels.Contains(baseLabel))
                labels.Add(baseLabel);

            int categoryIndex = labels.IndexOf(baseLabel);

            if (isElectron)
                electronSeries.Items.Add(new BarItem(level.Energy_eV, categoryIndex));
            else
                holeSeries.Items.Add(new BarItem(level.Energy_eV, categoryIndex));
        }

        categoryAxis.Labels.AddRange(labels);
        model.Series.Add(electronSeries);
        model.Series.Add(holeSeries);
        model.Legends.Add(new Legend { LegendPosition = LegendPosition.TopRight });

        return model;
    }

    /// <summary>
    /// Emissionsspektrum: Gauß-Peak bei der Bandlücken-Energie.
    /// </summary>
    public static PlotModel CreateSpectrumPlot(QuantumDot dot)
    {
        var model = new PlotModel
        {
            Title = "Emissionsspektrum",
            Background = OxyColors.White
        };

        model.Axes.Add(new LinearAxis { Position = AxisPosition.Bottom, Title = "Wellenlänge (nm)" });
        model.Axes.Add(new LinearAxis { Position = AxisPosition.Left, Title = "Intensität (a.u.)" });

        if (dot == null || dot.EmissionWavelength_nm <= 0 || double.IsInfinity(dot.EmissionWavelength_nm))
            return model;

        double center = dot.EmissionWavelength_nm;
        double sigma = center * 0.02; // 2 % FWHM-artige Breite
        var series = new LineSeries
        {
            Title = "Emission",
            Color = OxyColors.ForestGreen,
            StrokeThickness = 2
        };

        for (double lambda = center * 0.7; lambda <= center * 1.3; lambda += center * 0.005)
        {
            double intensity = Math.Exp(-0.5 * Math.Pow((lambda - center) / sigma, 2));
            series.Points.Add(new DataPoint(lambda, intensity));
        }

        model.Series.Add(series);

        // Vertikale Linie für Peakposition
        var peakAnnotation = new LineAnnotation
        {
            Type = LineAnnotationType.Vertical,
            X = center,
            Color = OxyColors.Red,
            LineStyle = LineStyle.Dash,
            Text = $"{center:F1} nm"
        };
        model.Annotations.Add(peakAnnotation);

        return model;
    }
}
