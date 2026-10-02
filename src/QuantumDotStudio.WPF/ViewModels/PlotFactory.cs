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

        var categoryAxis = new CategoryAxis { Position = AxisPosition.Left, Title = "Zustand" };
        var valueAxis = new LinearAxis { Position = AxisPosition.Bottom, Title = "Energie (eV)", Minimum = 0 };
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
            bool isElectron = level.Particle == Particle.Electron;
            string baseLabel = level.Label;
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
    /// Radiales Bandkanten-Profil eines Core/Shell-Quantum Dots:
    /// Leitungsband (blau) und Valenzband (orange) entlang des Radius,
    /// mit Barriersprüngen an der Core/Shell-Grenzfläche und den gebundenen
    /// Elektron-/Loch-Niveaus als gestrichelte Linien.
    /// Für homogene Dots (keine Shell) wird ein flaches Profil gezeichnet.
    /// </summary>
    public static PlotModel CreateBandProfilePlot(QuantumDot dot)
    {
        var model = new PlotModel
        {
            Title = "Bandkantenprofil",
            Background = OxyColors.White
        };

        model.Axes.Add(new LinearAxis { Position = AxisPosition.Bottom, Title = "Radius (nm)" });
        model.Axes.Add(new LinearAxis { Position = AxisPosition.Left, Title = "Energie (eV)" });

        if (dot is not CoreShellQuantumDot csdot)
        {
            // Homogener Dot: flaches Profil mit Bulk-Bandkanten als Referenz.
            var flat = new LineSeries { Title = "Leitungsband (homogen)", Color = OxyColors.DodgerBlue };
            flat.Points.Add(new DataPoint(0, 0));
            flat.Points.Add(new DataPoint(dot.Radius_nm, 0));
            model.Series.Add(flat);
            return model;
        }

        double coreR = csdot.CoreRadius_nm;
        double totalR = csdot.Radius_nm;

        // Nullniveau: Leitungsbandminimum des Core-Materials.
        // Core: CB = 0, VB = -Eg_core. Shell: CB = V0_e, VB = V0_e - Eg_shell.
        double cbCore = 0.0;
        double vbCore = -csdot.Material.BandGap_eV;
        double cbShell = csdot.ElectronBarrier_eV;
        double vbShell = csdot.ElectronBarrier_eV - csdot.ShellMaterial.BandGap_eV;

        var cb = new LineSeries { Title = "Leitungsband", Color = OxyColors.DodgerBlue, StrokeThickness = 2 };
        cb.Points.Add(new DataPoint(0, cbCore));
        cb.Points.Add(new DataPoint(coreR, cbCore));
        cb.Points.Add(new DataPoint(coreR, cbShell));
        cb.Points.Add(new DataPoint(totalR, cbShell));
        model.Series.Add(cb);

        var vb = new LineSeries { Title = "Valenzband", Color = OxyColors.DarkOrange, StrokeThickness = 2 };
        vb.Points.Add(new DataPoint(0, vbCore));
        vb.Points.Add(new DataPoint(coreR, vbCore));
        vb.Points.Add(new DataPoint(coreR, vbShell));
        vb.Points.Add(new DataPoint(totalR, vbShell));
        model.Series.Add(vb);

        // Gebundene Niveaus als gestrichelte Linien im Core-Bereich.
        foreach (var level in csdot.EnergyLevels)
        {
            bool isElectron = level.Particle == Particle.Electron;
            var s = new LineSeries
            {
                Title = isElectron ? $"e: {level.Label}" : $"h: {level.Label}",
                Color = isElectron ? OxyColors.MediumBlue : OxyColors.OrangeRed,
                LineStyle = LineStyle.Dash,
                StrokeThickness = 1
            };
            double baseLine = isElectron ? cbCore : vbCore;
            s.Points.Add(new DataPoint(0, baseLine + level.Energy_eV));
            s.Points.Add(new DataPoint(coreR, baseLine + level.Energy_eV));
            model.Series.Add(s);
        }

        // Markierung der kritischen Schalendicke, wenn die Schale kritisch ist.
        if (csdot.IsStrainRelaxed && !double.IsInfinity(csdot.CriticalThickness_nm))
        {
            var relax = new LineAnnotation
            {
                Type = LineAnnotationType.Vertical,
                X = Math.Min(csdot.CoreRadius_nm + csdot.CriticalThickness_nm, totalR),
                Color = OxyColors.Red,
                LineStyle = LineStyle.Dot,
                Text = "t_krit"
            };
            model.Annotations.Add(relax);
        }

        model.Legends.Add(new Legend { LegendPosition = LegendPosition.RightTop, LegendFontSize = 9 });

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
