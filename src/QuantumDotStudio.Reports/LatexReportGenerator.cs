using System.Globalization;
using System.Text;
using QuantumDotStudio.Core.Models;
using QuantumDotStudio.Core.Physics;

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
    public static string Generate(QuantumDot dot, string? screenshotPath = null, string title = "Quantum Dot Studio — Technischer Bericht", string author = "Quantum Dot Studio", Core.Models.FeasibilityResult? feasibility = null)
    {
        var result = TryGenerate(dot, screenshotPath, title, author, feasibility);
        if (!result.Success)
            throw new InvalidOperationException(string.Join("; ", result.Errors));
        return result.Document;
    }

    /// <summary>
    /// Erzeugt den Bericht, ohne zu werfen: recoverbare Probleme (fehlendes
    /// Material, leere Atomsammlung, fehlende Energieniveaus) werden gesammelt
    /// und im Ergebnis zurückgegeben. Der Aufrufer kann Dokument und Warnungen
    /// anzeigen, statt an einer Ausnahme zu scheitern.
    /// </summary>
    public static ReportResult TryGenerate(QuantumDot dot, string? screenshotPath = null, string title = "Quantum Dot Studio — Technischer Bericht", string author = "Quantum Dot Studio", Core.Models.FeasibilityResult? feasibility = null)
    {
        var errors = new List<string>();
        if (dot is null)
        {
            errors.Add("QuantumDot ist null.");
            return new ReportResult { Document = string.Empty, Errors = errors };
        }

        if (dot.Material is null)
            errors.Add("QuantumDot.Material ist null; Materialtabelle entfaellt.");
        if (dot.Atoms is null)
            errors.Add("QuantumDot.Atoms ist null; Atomzahl wird als 0 ausgegeben.");
        if (dot.EnergyLevels is null)
            errors.Add("QuantumDot.EnergyLevels ist null; Energieniveaus-Tabelle bleibt leer.");

        try
        {
            // Zahlenformate deterministisch: der Bericht ist deutsch (babel ngerman),
            // also Dezimalkomma — unabhaengig von der Locale des Rechners (sonst
            // druckt dieselbe Berechnung je nach System "10.60" oder "10,60").
            var previousCulture = CultureInfo.CurrentCulture;
            CultureInfo.CurrentCulture = GermanCulture;
            try
            {
                return new ReportResult
                {
                    Document = GenerateCore(dot, screenshotPath, title, author, feasibility),
                    Errors = errors
                };
            }
            finally
            {
                CultureInfo.CurrentCulture = previousCulture;
            }
        }
        catch (Exception ex)
        {
            errors.Add($"Berichtserzeugung fehlgeschlagen: {ex.Message}");
            return new ReportResult { Document = string.Empty, Errors = errors };
        }
    }

    /// <summary>Feste Kultur für alle Zahlen im Bericht (deutsches Dezimalkomma).</summary>
    private static readonly CultureInfo GermanCulture = CultureInfo.GetCultureInfo("de-DE");

    private static string GenerateCore(QuantumDot dot, string? screenshotPath, string title, string author, Core.Models.FeasibilityResult? feasibility)
    {
        var latex = new LatexBuilder();

        latex.AppendLine(@"\documentclass[11pt,a4paper]{article}");
        latex.AppendLine(@"\usepackage[utf8]{inputenc}");
        latex.AppendLine(@"\usepackage[T1]{fontenc}");
        latex.AppendLine(@"\usepackage[ngerman]{babel}");
        latex.AppendLine(@"\usepackage{amsmath,amssymb}");
        latex.AppendLine(@"\usepackage{siunitx}");
        latex.AppendLine(@"\usepackage{booktabs}");
        latex.AppendLine(@"\usepackage{geometry}");
        latex.AppendLine(@"\usepackage{xcolor}");
        latex.AppendLine(@"\usepackage{graphicx}");
        latex.AppendLine(@"\usepackage{hyperref}");
        latex.AppendLine(@"\geometry{a4paper, margin=2.5cm}");
        latex.AppendLine(@"\definecolor{emissioncolor}{RGB}{" + WavelengthToRgb(dot.EmissionWavelength_nm) + @"}");
        latex.AppendLine();
        latex.AppendLine(@"\title{" + Escape(title) + @"}");
        latex.AppendLine(@"\author{" + Escape(author) + @"}");
        latex.AppendLine(@"\date{" + DateTime.Now.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture) + @"}");
        latex.AppendLine(@"\begin{document}");
        latex.AppendLine(@"\maketitle");
        latex.AppendLine();

        AppendSummary(latex, dot);
        AppendTheory(latex, dot);
        if (dot.Material is { } material)
            AppendMaterialTable(latex, material);
        AppendQuantumDotTable(latex, dot);
        AppendEnergyLevels(latex, dot);
        AppendSpectrum(latex, dot);

        if (dot is CoreShellQuantumDot cs)
        {
            AppendCoreShellSection(latex, cs);
        }

        if (feasibility is not null)
        {
            AppendFeasibilitySection(latex, feasibility);
        }

        if (!string.IsNullOrWhiteSpace(screenshotPath) && File.Exists(screenshotPath))
        {
            AppendScreenshot(latex, screenshotPath);
        }
        else
        {
            AppendScreenshotPlaceholder(latex);
        }

        latex.AppendLine(@"\end{document}");

        return latex.ToString();
    }

    /// <summary>
    /// True, wenn für den Dot eine echte Umgebung gesetzt ist (Temperatur ≠
    /// Referenz, Solvens mit ε_r &gt; 0 oder pH). Der Standard (300 K, kein
    /// Solvens) bleibt ohne Umgebungsblock — der Report dokumentiert nur
    /// Abweichungen von der Referenzbedingung.
    /// </summary>
    private static bool HasEnvironment(QuantumDot dot)
    {
        if (dot.Environment is not { } env)
            return false;

        return env.Temperature_K != QuantumDotStudio.Core.Physics.EnvironmentModel.ReferenceTemperature_K
            || env.Solvent is { DielectricConstant: > 0 }
            || env.Ph is not null;
    }

    private static void AppendSummary(LatexBuilder latex, QuantumDot dot)
    {
        latex.AppendLine(@"\section{Zusammenfassung}");
        latex.AppendLine(@"Dieser Bericht dokumentiert die quantenmechanische Berechnung eines sphärischen Halbleiter-Nanokristalls (Quantum Dot).");
        latex.AppendLine($"Für das Material {Escape(dot.Material?.Name ?? "unbekannt")} bei einem Radius von {dot.Radius_nm:F2}\\,nm ergibt sich eine effektive Bandlücke von {dot.TotalBandGap_eV:F3}\\,eV,");
        latex.AppendLine($"was einer Emissionswellenlänge von {dot.EmissionWavelength_nm:F1}\\,nm entspricht.");
        if (HasEnvironment(dot) && dot.Environment is { } env)
        {
            string solvent = env.Solvent is { DielectricConstant: > 0 } s ? s.Name : "ohne Solvens";
            string phPart = env.Ph is double p ? $", pH {p:F1}" : string.Empty;
            latex.AppendLine($"Die Berechnung berücksichtigt eine Umgebung bei {env.Temperature_K:F0}\\,K im Medium {Escape(solvent)}{phPart};");
            latex.AppendLine("Bandlücke und Coulomb-Abschirmung sind entsprechend korrigiert.");
        }
        latex.AppendLine();
    }

    private static void AppendTheory(LatexBuilder latex, QuantumDot dot)
    {
        latex.AppendLine(@"\section{Physikalisches Modell}");
        latex.AppendLine(@"\subsection{Annahmen}");
        latex.AppendLine(@"\begin{itemize}");
        latex.AppendLine(dot is CoreShellQuantumDot
            ? @"\item Endlicher sphärischer Potentialtopf im Core (Barriere = Band-Offset) mit Radius $R$"
            : @"\item Unendlich hoher sphärischer Potentialtopf mit Radius $R$");
        latex.AppendLine(@"\item Isotrope effektive Masse $m^*$");
        latex.AppendLine(@"\item Coulomb-Wechselwirkung in führender Ordnung (Brus-Gleichung)");
        if (HasEnvironment(dot) && dot.Environment is { } env)
        {
            string solvent = env.Solvent is { DielectricConstant: > 0 } s ? s.Name : "ohne Solvens";
            string phPart = env.Ph is double p ? $", pH {p:F1}" : string.Empty;
            latex.AppendLine($@"\item Umgebung: {env.Temperature_K:F0}\,K im Medium {Escape(solvent)}{phPart} (Varshni-Bandlücke, dielektrische Abschirmung)");
        }
        latex.AppendLine(@"\end{itemize}");
        latex.AppendLine();

        latex.AppendLine(@"\subsection{Energieniveaus im sphärischen Potentialtopf}");
        latex.AppendLine(@"Die Confinement-Energie eines Teilchens im unendlichen sphärischen Topf ist");
        latex.AppendLine(@"\begin{equation}");
        latex.AppendLine(@"E_{n,l} = \frac{\hbar^2 \alpha_{n,l}^2}{2 m^* R^2},");
        latex.AppendLine(@"\end{equation}");
        latex.AppendLine(@"wobei $\alpha_{n,l}$ die $l$-te Nullstelle der sphärischen Bessel-Funktion $j_n$ ist.");
        latex.AppendLine(@"Für den Grundzustand gilt $\alpha_{1,0}=\pi$.");
        latex.AppendLine();

        latex.AppendLine(@"\subsection{Effektive Bandlücke}");
        latex.AppendLine(@"Die effektive Bandlücke des Quantum Dots folgt aus der Bulk-Bandlücke $E_g$,");
        latex.AppendLine(@"den Confinement-Energien von Elektron und Loch sowie dem Coulomb-Term:");
        latex.AppendLine(@"\begin{equation}");
        latex.AppendLine(@"E_{\text{QD}} = E_g + \Delta E_e + \Delta E_h - \frac{1{,}786\, e^2}{4 \pi \varepsilon_0 \varepsilon_r R}.");
        latex.AppendLine(@"\end{equation}");
        latex.AppendLine();

        latex.AppendLine(@"\subsection{Emissionswellenlänge}");
        latex.AppendLine(@"Aus der Bandlücke wird die Emissionswellenlänge berechnet:");
        latex.AppendLine(@"\begin{equation}");
        latex.AppendLine(@"\lambda = \frac{h c}{E_{\text{QD}}}.");
        latex.AppendLine(@"\end{equation}");
        latex.AppendLine();
    }

    private static void AppendMaterialTable(LatexBuilder latex, Material material)
    {
        latex.AppendLine(@"\section{Materialparameter}");
        latex.AppendLine(@"\begin{table}[h]");
        latex.AppendLine(@"\centering");
        latex.AppendLine(@"\begin{tabular}{lr}");
        latex.AppendLine(@"\toprule");
        latex.AppendLine(@"\textbf{Parameter} & \textbf{Wert} \\");
        latex.AppendLine(@"\midrule");
        latex.AppendLine($"Material & {Escape(material.Name)} \\\\ ");
        latex.AppendLine($"Bulk-Bandlücke $E_g$ & {material.BandGap_eV:F3}\\,eV \\\\ ");
        latex.AppendLine($"Effektive Elektronenmasse $m_e^*$ & {material.EffectiveMassElectron:F3}\\,$m_0$ \\\\ ");
        latex.AppendLine($"Effektive Löchermasse $m_h^*$ & {material.EffectiveMassHole:F3}\\,$m_0$ \\\\ ");
        latex.AppendLine($@"Dielektrizitätskonstante $\varepsilon_r$ & {material.DielectricConstant:F2} \\ ");
        latex.AppendLine($@"Gitterkonstante $a$ & {material.LatticeConstant_A:F3}\,\AA \\ ");
        latex.AppendLine(@"\bottomrule");
        latex.AppendLine(@"\end{tabular}");
        latex.AppendLine(@"\caption{Physikalische Parameter des gewählten Halbleitermaterials.}");
        latex.AppendLine(@"\end{table}");
        latex.AppendLine();
    }

    private static void AppendQuantumDotTable(LatexBuilder latex, QuantumDot dot)
    {
        latex.AppendLine(@"\section{Quantum-Dot-Parameter und Ergebnisse}");
        latex.AppendLine(@"\begin{table}[h]");
        latex.AppendLine(@"\centering");
        latex.AppendLine(@"\begin{tabular}{lr}");
        latex.AppendLine(@"\toprule");
        latex.AppendLine(@"\textbf{Größe} & \textbf{Wert} \\\\");
        latex.AppendLine(@"\midrule");
        latex.AppendLine($"Radius $R$ & {dot.Radius_nm:F2}\\,nm \\\\ ");
        latex.AppendLine($"Anzahl Atome im Gitter & {dot.Atoms?.Count ?? 0} \\\\ ");
        latex.AppendLine($"Confinement-Energie Elektron & {dot.ConfinementEnergyElectron_eV:F4}\\,eV \\\\ ");
        latex.AppendLine($"Confinement-Energie Loch & {dot.ConfinementEnergyHole_eV:F4}\\,eV \\\\ ");
        latex.AppendLine($@"Coulomb-Term $E_C$ & {dot.CoulombEnergy_eV:F4}\,eV \\ ");
        latex.AppendLine($@"Effektive Bandlücke $E_{{\text{{QD}}}}$ & {dot.TotalBandGap_eV:F4}\,eV \\ ");
        latex.AppendLine($@"Emissionswellenlänge $\lambda$ & {dot.EmissionWavelength_nm:F1}\,nm \\ ");
        if (HasEnvironment(dot) && dot.Environment is { } env)
        {
            string solvent = env.Solvent is { DielectricConstant: > 0 } s ? s.Name : "ohne Solvens";
            double epsEff = EnvironmentModel.EffectiveDielectric(
                dot.Material.DielectricConstant,
                env.Solvent is { DielectricConstant: > 0 } s2 ? s2.DielectricConstant : null);
            latex.AppendLine($@"Temperatur & {env.Temperature_K:F0}\,K \\ ");
            latex.AppendLine($@"Solvens & {Escape(solvent)} \\ ");
            if (env.Ph is double p)
                latex.AppendLine($@"pH & {p:F1} \\ ");
            latex.AppendLine($@"Effektive Dielektrizitätskonstante $\varepsilon_r^{{\text{{eff}}}}$ & {epsEff:F2} \\ ");
        }
        latex.AppendLine(@"\bottomrule");
        latex.AppendLine(@"\end{tabular}");
        latex.AppendLine(@"\caption{Ergebnisse der Quantum-Dot-Berechnung.}");
        latex.AppendLine(@"\end{table}");
        latex.AppendLine();
    }

    private static void AppendEnergyLevels(LatexBuilder latex, QuantumDot dot)
    {
        latex.AppendLine(@"\section{Berechnete Energieniveaus}");
        latex.AppendLine(@"\begin{table}[h]");
        latex.AppendLine(@"\centering");
        latex.AppendLine(@"\begin{tabular}{cccr}");
        latex.AppendLine(@"\toprule");
        latex.AppendLine(@"\textbf{Teilchen} & \textbf{Label} & \textbf{$(n,l)$} & \textbf{Energie (eV)} \\\\");
        latex.AppendLine(@"\midrule");

        foreach (var level in (dot.EnergyLevels ?? new List<EnergyLevel>()).OrderBy(e => e.Energy_eV))
        {
            var particle = level.Particle == Particle.Electron ? "Elektron" : "Loch";
            latex.AppendLine($@"{Escape(particle)} & {Escape(level.Label)} & ({level.PrincipalQuantumNumber_n},{level.AngularMomentum_l}) & {level.Energy_eV:F4} \\ ");
        }

        latex.AppendLine(@"\bottomrule");
        latex.AppendLine(@"\end{tabular}");
        latex.AppendLine(@"\caption{Energieniveaus des Elektrons und des Lochs im sphärischen Potentialtopf.}");
        latex.AppendLine(@"\end{table}");
        latex.AppendLine();
    }

    private static void AppendSpectrum(LatexBuilder latex, QuantumDot dot)
    {
        latex.AppendLine(@"\section{Emissionsspektrum}");
        latex.AppendLine($@"Der berechnete Übergang liegt bei $\lambda = {dot.EmissionWavelength_nm:F1}$\,nm,");
        latex.AppendLine($"was im folgenden Farbblock visualisiert ist:");
        latex.AppendLine(@"\begin{center}");
        latex.AppendLine(@"\fcolorbox{black}{emissioncolor}{\parbox{0.4\textwidth}{\centering\vspace{1cm}Emissionsfarbe\vspace{1cm}}}");
        latex.AppendLine(@"\end{center}");
        latex.AppendLine();
    }

    /// <summary>
    /// Machbarkeits-Kapitel (Roadmap 3.3): Verdikt, Gesamtwahrscheinlichkeit,
    /// Faktor-Tabelle mit Gewichten und Vorschlaegen, Monte-Carlo-Band und
    /// Varianzzerlegung.
    /// </summary>
    private static void AppendFeasibilitySection(LatexBuilder latex, FeasibilityResult feasibility)
    {
        latex.AppendLine(@"\section{Machbarkeit des Sensor-Entwurfs}");
        string verdictWord = feasibility.Verdict switch
        {
            "green" => "machbar (grün)",
            "amber" => "riskant (gelb)",
            _ => "nicht empfohlen (rot)"
        };
        latex.AppendLine($@"Die Bewertung des Sensor-Entwurfs ergibt \textbf{{P(Erfolg) = {feasibility.P * 100:F0}\,\%}} — {verdictWord}.");
        latex.AppendLine();
        latex.AppendLine(@"\begin{table}[h]");
        latex.AppendLine(@"\centering");
        latex.AppendLine(@"\begin{tabular}{lrr}");
        latex.AppendLine(@"\toprule");
        latex.AppendLine(@"\textbf{Faktor} & \textbf{Gewicht} & \textbf{Score} \\");
        latex.AppendLine(@"\midrule");
        foreach (var f in feasibility.Factors)
        {
            latex.AppendLine($@"{Escape(f.Name)} & {f.Weight * 100:F0}\,\% & {f.Score * 100:F0}\,\% \\");
        }
        latex.AppendLine(@"\midrule");
        latex.AppendLine($@"\textbf{{Geometrisches Mittel}} & \textbf{{100\,\%}} & \textbf{{{feasibility.P * 100:F0}\,\%}} \\");
        latex.AppendLine(@"\bottomrule");
        latex.AppendLine(@"\end{tabular}");
        latex.AppendLine(@"\caption{Machbarkeitsfaktoren des Sensor-Entwurfs (gewichtetes geometrisches Mittel; ein disqualifizierender Faktor kann nicht kompensiert werden).}");
        latex.AppendLine(@"\end{table}");
        latex.AppendLine();

        var suggestions = feasibility.Factors.Where(f => !string.IsNullOrWhiteSpace(f.Suggestion)).ToList();
        if (suggestions.Count > 0)
        {
            latex.AppendLine(@"\subsection{Empfehlungen}");
            latex.AppendLine(@"\begin{itemize}");
            foreach (var f in suggestions)
            {
                latex.AppendLine($@"\item \textbf{{{Escape(f.Name)}}}: {Escape(f.Suggestion)}");
            }
            latex.AppendLine(@"\end{itemize}");
            latex.AppendLine();
        }

        if (feasibility.HasMonteCarlo)
        {
            latex.AppendLine($@"\subsection{{Unsicherheit (Monte-Carlo, N = {feasibility.McSampleCount})}}");
            latex.AppendLine($@"Materialparameter-Unsicherheiten ($\pm 0{{,}}05$\,eV Bandlücke, $\pm 10$\,\% Massen, $\pm 0{{,}}05$\,\AA{{}} Gitterkonstante) ergeben:");
            latex.AppendLine(@"\begin{itemize}");
            latex.AppendLine($@"\item Median: {feasibility.McMedian * 100:F0}\,\%");
            latex.AppendLine($@"\item 5--95-\%-Band: {feasibility.McP05 * 100:F0}--{feasibility.McP95 * 100:F0}\,\%");
            latex.AppendLine(@"\end{itemize}");
            latex.AppendLine(@"\paragraph{Varianzzerlegung.} Der dominante Unsicherheitsbeitrag:");
            latex.AppendLine(@"\begin{itemize}");
            foreach (var v in feasibility.Decomposition)
            {
                latex.AppendLine($@"\item {Escape(v.Input)}: {v.Fraction * 100:F0}\,\%");
            }
            latex.AppendLine(@"\end{itemize}");
        }
        latex.AppendLine();
    }

    private static void AppendScreenshotPlaceholder(LatexBuilder latex)
    {
        latex.AppendLine(@"\section{Visualisierung}");
        latex.AppendLine(@"\begin{figure}[h]");
        latex.AppendLine(@"\centering");
        latex.AppendLine(@"\fbox{\parbox{0.8\textwidth}{\centering\vspace{3cm}Screenshot der 3D-Visualisierung (manuell einfügen)\vspace{3cm}}}");
        latex.AppendLine(@"\caption{Platzhalter für den Screenshot der 3D-Ansicht.}");
        latex.AppendLine(@"\end{figure}");
        latex.AppendLine();
    }

    /// <summary>
    /// Dokumentiert die Core/Shell-Heterostruktur: Geometrie, Band-Offsets,
    /// Strain-Status und gebundene Zustände im endlichen Potentialtopf.
    /// </summary>
    private static void AppendCoreShellSection(LatexBuilder latex, CoreShellQuantumDot cs)
    {
        latex.AppendLine(@"\section{Core/Shell-Heterostruktur}");
        latex.AppendLine($@"Core-Material: {Escape(cs.Material.Name)} ({cs.CoreRadius_nm:F2}\,nm), Shell-Material: {Escape(cs.ShellMaterial.Name)} ({cs.ShellThickness_nm:F2}\,nm).");
        latex.AppendLine(@"\begin{table}[h]");
        latex.AppendLine(@"\centering");
        latex.AppendLine(@"\begin{tabular}{lr}");
        latex.AppendLine(@"\toprule");
        latex.AppendLine(@"\textbf{Größe} & \textbf{Wert} \\ ");
        latex.AppendLine(@"\midrule");
        latex.AppendLine($@"Elektronenbarriere $V_{{0,e}}$ & {cs.ElectronBarrier_eV:F3}\,eV \\ ");
        latex.AppendLine($@"Lochbarriere $V_{{0,h}}$ & {cs.HoleBarrier_eV:F3}\,eV \\ ");
        latex.AppendLine($@"Gitterfehlanpassung $f$ & {cs.LatticeMismatch_f * 100:F2}\,\% \\ ");
        latex.AppendLine(cs.IsStrainRelaxed
            ? $@"Kritische Schalendicke $t_c$ & {cs.CriticalThickness_nm:F2}\,nm \\\\ \emph{{Status}} & \textbf{{relaxiert (Versetzungen wahrscheinlich)}} \\ "
            : $@"Kritische Schalendicke $t_c$ & {cs.CriticalThickness_nm:F2}\,nm \\\\ \emph{{Status}} & kohärent (unter $t_c$) \\ ");
        latex.AppendLine(@"\bottomrule");
        latex.AppendLine(@"\end{tabular}");
        latex.AppendLine(@"\caption{Band-Offsets und Strain-Charakteristik der Core/Shell-Struktur.}");
        latex.AppendLine(@"\end{table}");
        latex.AppendLine();
        latex.AppendLine(@"Die gebundenen Zustände wurden im endlichen sphärischen Potentialtopf");
        latex.AppendLine(@"(Anschlussbedingung $k \cot(kR) = -\kappa$, numerische Bisektion) berechnet.");
        latex.AppendLine();
    }

    private static void AppendScreenshot(LatexBuilder latex, string screenshotPath)
    {
        var fileName = Path.GetFileName(screenshotPath);
        latex.AppendLine(@"\section{Visualisierung}");
        latex.AppendLine(@"\begin{figure}[h]");
        latex.AppendLine(@"\centering");
        latex.AppendLine($@"\includegraphics[width=0.8\textwidth]{{{fileName}}}");
        latex.AppendLine(@"\caption{Screenshot der 3D-Ansicht mit Atomgitter und Wahrscheinlichkeitswolke.}");
        latex.AppendLine(@"\end{figure}");
        latex.AppendLine();
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
    public static string Escape(string? text)
    {
        if (string.IsNullOrEmpty(text))
            return string.Empty;

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
