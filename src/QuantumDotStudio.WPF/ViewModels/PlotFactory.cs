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

    // =============================================================== Sensor

    /// <summary>
    /// Platzhalter-Plot, wenn kein Sensor konfiguriert werden kann.
    /// </summary>
    public static PlotModel CreateSensorPlaceholderPlot(string message)
    {
        return new PlotModel
        {
            Title = message,
            Background = OxyColors.White
        };
    }

    /// <summary>
    /// FRET-Antwort: Transfer-Effizienz E(r) mit Markierung der ungebundenen
    /// und gebundenen Donor-Akzeptor-Abstände (ratiometrisches Design).
    /// </summary>
    public static PlotModel CreateFretResponsePlot(double forsterRadius_nm, double rUnbound, double rBound)
    {
        var model = new PlotModel
        {
            Title = $"FRET-Antwort (R₀ = {forsterRadius_nm:F1} nm)",
            Background = OxyColors.White
        };
        model.Axes.Add(new LinearAxis { Position = AxisPosition.Bottom, Title = "Donor-Akzeptor-Abstand r (nm)" });
        model.Axes.Add(new LinearAxis { Position = AxisPosition.Left, Title = "Transfer-Effizienz E", Minimum = 0, Maximum = 1 });

        double rMax = Math.Max(Math.Max(rUnbound, rBound) * 1.4, forsterRadius_nm * 2);
        var curve = new LineSeries { Title = "E(r) = 1/(1+(r/R₀)⁶)", Color = OxyColors.DodgerBlue, StrokeThickness = 2 };
        for (double r = 0.5; r <= rMax; r += rMax / 200.0)
        {
            double e = 1.0 / (1.0 + Math.Pow(r / forsterRadius_nm, 6));
            curve.Points.Add(new DataPoint(r, e));
        }
        model.Series.Add(curve);

        foreach (var (r, label, color) in new[]
        {
                 (rUnbound, "ungebunden", OxyColors.DarkOrange),
                 (rBound, "gebunden", OxyColors.Green)
             })
        {
            if (r <= 0) continue;
            double e = 1.0 / (1.0 + Math.Pow(r / forsterRadius_nm, 6));
            model.Annotations.Add(new LineAnnotation
            {
                Type = LineAnnotationType.Vertical,
                X = r,
                Color = color,
                LineStyle = LineStyle.Solid,
                Text = $"{label}: r={r:F2} nm, E={e * 100:F0} %"
            });
        }

        model.Legends.Add(new Legend { LegendPosition = LegendPosition.TopRight, LegendFontSize = 9 });
        return model;
    }

    /// <summary>
    /// Quenching-Kalibrierkurve (Stern-Volmer): I/I0 vs. Konzentration
    /// (log-Skala) mit Markierung der aktuellen Konzentration und LOD.
    /// </summary>
    public static PlotModel CreateQuenchingResponsePlot(double kSV_M, double currentConcentration_M)
    {
        var model = new PlotModel
        {
            Title = "Stern-Volmer-Quenching",
            Background = OxyColors.White
        };
        model.Axes.Add(new LogarithmicAxis { Position = AxisPosition.Bottom, Title = "Konzentration [A] (M)" });
        model.Axes.Add(new LinearAxis { Position = AxisPosition.Left, Title = "I/I₀", Minimum = 0, Maximum = 1.05 });

        var curve = new LineSeries { Title = "I/I₀ = 1/(1+K_SV[A])", Color = OxyColors.DodgerBlue, StrokeThickness = 2 };
        double lod = (1.0 / 0.9 - 1.0) / kSV_M;
        double cMin = Math.Max(lod / 100.0, 1e-12);
        double cMax = 100.0 / kSV_M;
        for (double c = cMin; c <= cMax; c *= 1.12)
        {
            curve.Points.Add(new DataPoint(c, 1.0 / (1.0 + kSV_M * c)));
        }
        model.Series.Add(curve);

        model.Annotations.Add(new LineAnnotation
        {
            Type = LineAnnotationType.Vertical,
            X = lod,
            Color = OxyColors.Red,
            LineStyle = LineStyle.Dot,
            Text = $"LOD = {lod:E1} M"
        });

        if (currentConcentration_M > cMin && currentConcentration_M < cMax)
        {
            double i = 1.0 / (1.0 + kSV_M * currentConcentration_M);
            model.Annotations.Add(new LineAnnotation
            {
                Type = LineAnnotationType.Vertical,
                X = currentConcentration_M,
                Color = OxyColors.Green,
                LineStyle = LineStyle.Solid,
                Text = $"[A]={currentConcentration_M:E1}, I/I₀={i * 100:F0} %"
            });
        }

        model.Legends.Add(new Legend { LegendPosition = LegendPosition.TopRight, LegendFontSize = 9 });
        return model;
    }

    /// <summary>
    /// Charge/pH-Antwort: Nernst-Potential vs. pH (log-Aktivitaet).
    /// </summary>
    public static PlotModel CreateChargeResponsePlot(double slope_mV_per_decade)
    {
        var model = new PlotModel
        {
            Title = "Nernst-Antwort (Oberflächenpotential)",
            Background = OxyColors.White
        };
        model.Axes.Add(new LinearAxis { Position = AxisPosition.Bottom, Title = "pH", Minimum = 0, Maximum = 14 });
        model.Axes.Add(new LinearAxis { Position = AxisPosition.Left, Title = "ψ (mV)" });

        var curve = new LineSeries { Title = "ψ = slope · log₁₀(10⁻pH)", Color = OxyColors.DodgerBlue, StrokeThickness = 2 };
        for (double ph = 0; ph <= 14; ph += 0.25)
        {
            curve.Points.Add(new DataPoint(ph, slope_mV_per_decade * Math.Log10(Math.Pow(10, -ph))));
        }
        model.Series.Add(curve);

        model.Legends.Add(new Legend { LegendPosition = LegendPosition.TopRight, LegendFontSize = 9 });
        return model;
    }

    /// <summary>
    /// PET-Antwort: Quench-Fraktion vs. Analyt-Redoxpotential mit Markierung
    /// der CB-Kante des aktiven QD.
    /// </summary>
    public static PlotModel CreatePetResponsePlot(QuantumDot dot, Analyte analyte)
    {
        var model = new PlotModel
        {
            Title = "PET-Antwort (Energetik)",
            Background = OxyColors.White
        };
        model.Axes.Add(new LinearAxis { Position = AxisPosition.Bottom, Title = "Redoxpotential E₀ (V vs. NHE)" });
        model.Axes.Add(new LinearAxis { Position = AxisPosition.Left, Title = "Quench-Fraktion", Minimum = 0, Maximum = 1 });

        double cb = dot.Material.ElectronAffinity_eV - 4.44;
        var curve = new LineSeries { Title = "Quench-Fraktion(E₀)", Color = OxyColors.DodgerBlue, StrokeThickness = 2 };
        for (double e0 = cb - 0.5; e0 <= cb + 0.5; e0 += 0.01)
        {
            double gap = cb - e0;
            double frac = 1.0 / (1.0 + Math.Exp(gap / 0.0257));
            curve.Points.Add(new DataPoint(e0, frac));
        }
        model.Series.Add(curve);

        model.Annotations.Add(new LineAnnotation
        {
            Type = LineAnnotationType.Vertical,
            X = cb,
            Color = OxyColors.Red,
            LineStyle = LineStyle.Dash,
            Text = $"CB-Kante = {cb:F2} V"
        });

        if (analyte.RedoxPotential_V != 0)
        {
            model.Annotations.Add(new LineAnnotation
            {
                Type = LineAnnotationType.Vertical,
                X = analyte.RedoxPotential_V,
                Color = OxyColors.Green,
                LineStyle = LineStyle.Solid,
                Text = $"E₀ = {analyte.RedoxPotential_V:F2} V"
            });
        }

        model.Legends.Add(new Legend { LegendPosition = LegendPosition.TopLeft, LegendFontSize = 9 });
        return model;
    }
}
