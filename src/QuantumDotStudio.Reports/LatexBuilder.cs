using System.Text;

namespace QuantumDotStudio.Reports;

/// <summary>
/// Kleiner Zeilenschreiber für LaTeX-Dokumente: er besitzt den
/// <see cref="StringBuilder"/> und die Absatz-/Listen-Helfer. LaTeX-Inhalte
/// gehören IMMER in verbatim Strings (<c>$@"..."</c>) — sonst interpretiert
/// C# Backslash-Sequenzen wie <c>\item</c> als Escape.
/// </summary>
internal sealed class LatexBuilder
{
    private readonly StringBuilder _sb = new();

    /// <summary>Rohe Zeile (null wird als Leerzeile geschrieben).</summary>
    public void AppendLine(string? line) => _sb.AppendLine(line ?? string.Empty);

    /// <summary>Leere Zeile (Absatzabstand).</summary>
    public void AppendLine() => _sb.AppendLine();

    /// <summary>Absatz: Textzeile + Leerzeile.</summary>
    public void Paragraph(string? text)
    {
        AppendLine(text);
        AppendLine();
    }

    /// <summary>Listenpunkt für itemize-Umgebungen.</summary>
    public void Item(string? text) => AppendLine($@"\item {text}");

    /// <summary>Abschnittsüberschrift mit Leerzeile danach.</summary>
    public void Section(string? title)
    {
        AppendLine($@"\section{{{title}}}");
        AppendLine();
    }

    /// <summary>Der fertige LaTeX-Quelltext.</summary>
    public override string ToString() => _sb.ToString();
}