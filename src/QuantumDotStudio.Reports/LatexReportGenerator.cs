using System.Globalization;
using System.Text;
using QuantumDotStudio.Core.Models;

namespace QuantumDotStudio.Reports;

/// <summary>
/// Generiert einen LaTeX-Technischen Bericht aus einem berechneten Quantum Dot.
/// </summary>
public static class LatexReportGenerator
{
    /// <summary>
    /// Erzeugt den vollständigen LaTeX-Quelltext für den Bericht.
    /// </summary>
    /// <param name="dot">Das zu dokumentierende Quantum Dot.</param>
    /// <param name="screenshotPath">Optionaler Pfad zu einem PNG-Screenshot der 3D-Ansicht, der eingebettet wird.</param>
    /// <param name="title">Titel des Berichts.</param>
    /// <param name="author">Autor des Berichts.</param>
    /// <returns>LaTeX-Quelltext als UTF-8 String.</returns>
    public static string Generate(QuantumDot dot, string? screenshotPath = null, string title = "Quantum Dot Studio — Technischer Bericht", string author = "Quantum Dot Studio")
    {
        ArgumentNullException.ThrowIfNull(dot);
        ArgumentNullException.ThrowIfNull(dot.Material);

        var sb = new StringBuilder();

        sb.AppendLine(@"\documentclass[11pt,a4paper]{article}");
        sb.AppendLine(@"\usepackage[utf8]{inputenc}");
        sb.AppendLine(@"\usepackage[T1]{fontenc}");
        sb.AppendLine(@"\usepackage[ngerman]{babel}");
        sb.AppendLine(@"\usepackage{amsmath,amssymb}");
        sb.AppendLine(@"\usepackage{siunitx}");
        sb.AppendLine(@"\usepackage{booktabs}");
        sb.AppendLine(@"\usepackage{geometry}");
        sb.AppendLine(@"\usepackage{xcolor}");
        sb.AppendLine(@"\usepackage{graphicx}");
        sb.AppendLine(@"\usepackage{hyperref}");
        sb.AppendLine(@"\geometry{a4paper, margin=2.5cm}");
        sb.AppendLine(@"\definecolor{emissioncolor}{RGB}{" + WavelengthToRgb(dot.EmissionWavelength_nm) + @"}");
        sb.AppendLine();
        sb.AppendLine(@"\title{" + Escape(title) + @"}");
        sb.AppendLine(@"\author{" + Escape(author) + @"}");
        sb.AppendLine(@"\date{" + DateTime.Now.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture) + @"}");
        sb.AppendLine(@"\begin{document}");
        sb.AppendLine(@"\maketitle");
        sb.AppendLine();

        AppendSummary(sb, dot);
        AppendTheory(sb);
        AppendMaterialTable(sb, dot.Material);
        AppendQuantumDotTable(sb, dot);
        AppendEnergyLevels(sb, dot);
        AppendSpectrum(sb, dot);

        if (!string.IsNullOrWhiteSpace(screenshotPath) && File.Exists(screenshotPath))
        {
            AppendScreenshot(sb, screenshotPath);
        }
        else
        {
            AppendScreenshotPlaceholder(sb);
        }

        sb.AppendLine(@"\end{document}");

        return sb.ToString();
    }

    private static void AppendSummary(StringBuilder sb, QuantumDot dot)
    {
        sb.AppendLine(@"\section{Zusammenfassung}");
        sb.AppendLine(@"Dieser Bericht dokumentiert die quantenmechanische Berechnung eines sphärischen Halbleiter-Nanokristalls (Quantum Dot).");
        sb.AppendLine($"Für das Material {Escape(dot.Material.Name)} bei einem Radius von {dot.Radius_nm:F2}\\,nm ergibt sich eine effektive Bandlücke von {dot.TotalBandGap_eV:F3}\\,eV,");
        sb.AppendLine($"was einer Emissionswellenlänge von {dot.EmissionWavelength_nm:F1}\\,nm entspricht.");
        sb.AppendLine();
    }

    private static void AppendTheory(StringBuilder sb)
    {
        sb.AppendLine(@"\section{Physikalisches Modell}");
        sb.AppendLine(@"\subsection{Annahmen}");
        sb.AppendLine(@"\begin{itemize}");
        sb.AppendLine(@"\item Unendlich hoher sphärischer Potentialtopf mit Radius $R$");
        sb.AppendLine(@"\item Isotrope effektive Masse $m^*$");
        sb.AppendLine(@"\item Näherung ohne Coulomb-Wechselwirkung (vereinfachte Brus-Formel)");
        sb.AppendLine(@"\end{itemize}");
        sb.AppendLine();

        sb.AppendLine(@"\subsection{Energieniveaus im sphärischen Potentialtopf}");
        sb.AppendLine(@"Die Confinement-Energie eines Teilchens im unendlichen sphärischen Topf ist");
        sb.AppendLine(@"\begin{equation}");
        sb.AppendLine(@"E_{n,l} = \frac{\hbar^2 \alpha_{n,l}^2}{2 m^* R^2},");
        sb.AppendLine(@"\end{equation}");
        sb.AppendLine(@"wobei $\alpha_{n,l}$ die $l$-te Nullstelle der sphärischen Bessel-Funktion $j_n$ ist.");
        sb.AppendLine(@"Für den Grundzustand gilt $\alpha_{1,0}=\pi$.");
        sb.AppendLine();

        sb.AppendLine(@"\subsection{Effektive Bandlücke}");
        sb.AppendLine(@"Die effektive Bandlücke des Quantum Dots ergibt sich aus der Bulk-Bandlücke $E_g$ und den Confinement-Energien von Elektron und Loch:");
        sb.AppendLine(@"\begin{equation}");
        sb.AppendLine(@"E_{\text{QD}} = E_g + \Delta E_e + \Delta E_h.");
        sb.AppendLine(@"\end{equation}");
        sb.AppendLine();

        sb.AppendLine(@"\subsection{Emissionswellenlänge}");
        sb.AppendLine(@"Aus der Bandlücke wird die Emissionswellenlänge berechnet:");
        sb.AppendLine(@"\begin{equation}");
        sb.AppendLine(@"\lambda = \frac{h c}{E_{\text{QD}}}.");
        sb.AppendLine(@"\end{equation}");
        sb.AppendLine();
    }

    private static void AppendMaterialTable(StringBuilder sb, Material material)
    {
        sb.AppendLine(@"\section{Materialparameter}");
        sb.AppendLine(@"\begin{table}[h]");
        sb.AppendLine(@"\centering");
        sb.AppendLine(@"\begin{tabular}{lr}");
        sb.AppendLine(@"\toprule");
        sb.AppendLine(@"\textbf{Parameter} & \textbf{Wert} \\\ ");
        sb.AppendLine(@"\midrule");
        sb.AppendLine($"Material & {Escape(material.Name)} \\\\ ");
        sb.AppendLine($"Bulk-Bandlücke $E_g$ & {material.BandGap_eV:F3}\\,eV \\\\ ");
        sb.AppendLine($"Effektive Elektronenmasse $m_e^*$ & {material.EffectiveMassElectron:F3}\\,$m_0$ \\\\ ");
        sb.AppendLine($"Effektive Löchermasse $m_h^*$ & {material.EffectiveMassHole:F3}\\,$m_0$ \\\\ ");
        sb.AppendLine($@"Dielektrizitätskonstante $\varepsilon_r$ & {material.DielectricConstant:F2} \\ ");
        sb.AppendLine($@"Gitterkonstante $a$ & {material.LatticeConstant_A:F3}\,\AA \\ ");
        sb.AppendLine(@"\bottomrule");
        sb.AppendLine(@"\end{tabular}");
        sb.AppendLine(@"\caption{Physikalische Parameter des gewählten Halbleitermaterials.}");
        sb.AppendLine(@"\end{table}");
        sb.AppendLine();
    }

    private static void AppendQuantumDotTable(StringBuilder sb, QuantumDot dot)
    {
        sb.AppendLine(@"\section{Quantum-Dot-Parameter und Ergebnisse}");
        sb.AppendLine(@"\begin{table}[h]");
        sb.AppendLine(@"\centering");
        sb.AppendLine(@"\begin{tabular}{lr}");
        sb.AppendLine(@"\toprule");
        sb.AppendLine(@"\textbf{Größe} & \textbf{Wert} \\\ ");
        sb.AppendLine(@"\midrule");
        sb.AppendLine($"Radius $R$ & {dot.Radius_nm:F2}\\,nm \\\\ ");
        sb.AppendLine($"Anzahl Atome im Gitter & {dot.Atoms.Count} \\\\ ");
        sb.AppendLine($"Confinement-Energie Elektron & {dot.ConfinementEnergyElectron_eV:F4}\\,eV \\\\ ");
        sb.AppendLine($"Confinement-Energie Loch & {dot.ConfinementEnergyHole_eV:F4}\\,eV \\\\ ");
        sb.AppendLine($@"Effektive Bandlücke $E_{{\text{{QD}}}}$ & {dot.TotalBandGap_eV:F4}\,eV \\ ");
        sb.AppendLine($@"Emissionswellenlänge $\lambda$ & {dot.EmissionWavelength_nm:F1}\,nm \\ ");
        sb.AppendLine(@"\bottomrule");
        sb.AppendLine(@"\end{tabular}");
        sb.AppendLine(@"\caption{Ergebnisse der Quantum-Dot-Berechnung.}");
        sb.AppendLine(@"\end{table}");
        sb.AppendLine();
    }

    private static void AppendEnergyLevels(StringBuilder sb, QuantumDot dot)
    {
        sb.AppendLine(@"\section{Berechnete Energieniveaus}");
        sb.AppendLine(@"\begin{table}[h]");
        sb.AppendLine(@"\centering");
        sb.AppendLine(@"\begin{tabular}{cccr}");
        sb.AppendLine(@"\toprule");
        sb.AppendLine(@"\textbf{Teilchen} & \textbf{Label} & \textbf{$(n,l)$} & \textbf{Energie (eV)} \\\ ");
        sb.AppendLine(@"\midrule");

        foreach (var level in dot.EnergyLevels.OrderBy(e => e.Energy_eV))
        {
            var particle = level.Label.StartsWith("e-") ? "Elektron" : "Loch";
            var label = level.Label.Replace("e-", "").Replace("h-", "");
            sb.AppendLine($"{Escape(particle)} & {Escape(label)} & ({level.PrincipalQuantumNumber_n},{level.AngularMomentum_l}) & {level.Energy_eV:F4} \\\\ ");
        }

        sb.AppendLine(@"\bottomrule");
        sb.AppendLine(@"\end{tabular}");
        sb.AppendLine(@"\caption{Energieniveaus des Elektrons und des Lochs im sphärischen Potentialtopf.}");
        sb.AppendLine(@"\end{table}");
        sb.AppendLine();
    }

    private static void AppendSpectrum(StringBuilder sb, QuantumDot dot)
    {
        sb.AppendLine(@"\section{Emissionsspektrum}");
        sb.AppendLine($@"Der berechnete Übergang liegt bei $\lambda = {dot.EmissionWavelength_nm:F1}$\,nm,");
        sb.AppendLine($"was im folgenden Farbblock visualisiert ist:");
        sb.AppendLine(@"\begin{center}");
        sb.AppendLine(@"\fcolorbox{black}{emissioncolor}{\parbox{0.4\textwidth}{\centering\vspace{1cm}Emissionsfarbe\vspace{1cm}}}");
        sb.AppendLine(@"\end{center}");
        sb.AppendLine();
    }

    private static void AppendScreenshotPlaceholder(StringBuilder sb)
    {
        sb.AppendLine(@"\section{Visualisierung}");
        sb.AppendLine(@"\begin{figure}[h]");
        sb.AppendLine(@"\centering");
        sb.AppendLine(@"\fbox{\parbox{0.8\textwidth}{\centering\vspace{3cm}Screenshot der 3D-Visualisierung (manuell einfügen)\vspace{3cm}}}");
        sb.AppendLine(@"\caption{Platzhalter für den Screenshot der 3D-Ansicht.}");
        sb.AppendLine(@"\end{figure}");
        sb.AppendLine();
    }

    private static void AppendScreenshot(StringBuilder sb, string screenshotPath)
    {
        var fileName = Path.GetFileName(screenshotPath);
        sb.AppendLine(@"\section{Visualisierung}");
        sb.AppendLine(@"\begin{figure}[h]");
        sb.AppendLine(@"\centering");
        sb.AppendLine($@"\includegraphics[width=0.8\textwidth]{{{fileName}}}");
        sb.AppendLine(@"\caption{Screenshot der 3D-Ansicht mit Atomgitter und Wahrscheinlichkeitswolke.}");
        sb.AppendLine(@"\end{figure}");
        sb.AppendLine();
    }

    /// <summary>
    /// Wandelt eine Wellenlänge in nm in einen RGB-Wert für LaTeX um (Spektralfarbenapproximation).
    /// </summary>
    public static string WavelengthToRgb(double wavelength_nm)
    {
        double lambda = Math.Clamp(wavelength_nm, 380.0, 750.0);
        double r, g, b;

        if (lambda < 440)
        {
            r = (440 - lambda) / (440 - 380);
            g = 0.0;
            b = 1.0;
        }
        else if (lambda < 490)
        {
            r = 0.0;
            g = (lambda - 440) / (490 - 440);
            b = 1.0;
        }
        else if (lambda < 510)
        {
            r = 0.0;
            g = 1.0;
            b = (510 - lambda) / (510 - 490);
        }
        else if (lambda < 580)
        {
            r = (lambda - 510) / (580 - 510);
            g = 1.0;
            b = 0.0;
        }
        else if (lambda < 645)
        {
            r = 1.0;
            g = (645 - lambda) / (645 - 580);
            b = 0.0;
        }
        else
        {
            r = 1.0;
            g = 0.0;
            b = 0.0;
        }

        // Intensitätsfall am Rand des sichtbaren Spektrums
        double factor = lambda < 420 ? 0.3 + 0.7 * (lambda - 380) / (420 - 380) :
                        lambda > 700 ? 0.3 + 0.7 * (750 - lambda) / (750 - 700) : 1.0;

        int R = (int)Math.Round(255 * Math.Clamp(r * factor, 0, 1));
        int G = (int)Math.Round(255 * Math.Clamp(g * factor, 0, 1));
        int B = (int)Math.Round(255 * Math.Clamp(b * factor, 0, 1));

        return $"{R},{G},{B}";
    }

    /// <summary>
    /// Maskiert Sonderzeichen für LaTeX.
    /// </summary>
    public static string Escape(string text)
    {
        if (string.IsNullOrEmpty(text))
            return text;

        return text
            .Replace("\\", "\\textbackslash{}")
            .Replace("&", "\\&")
            .Replace("%", "\\%")
            .Replace("$", "\\$")
            .Replace("#", "\\#")
            .Replace("_", "\\_")
            .Replace("{", "\\{")
            .Replace("}", "\\}")
            .Replace("~", "\\textasciitilde{}")
            .Replace("^", "\\textasciicircum{}")
            .Replace("<", "\\textless{}")
            .Replace(">", "\\textgreater{}");
    }
}
