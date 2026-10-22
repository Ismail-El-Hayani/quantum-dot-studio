namespace QuantumDotStudio.Reports;

/// <summary>
/// Ergebnis einer Report-Erzeugung: das erzeugte Dokument plus die Liste
/// recoverbarer Probleme. <see cref="Success"/> ist genau dann true, wenn
/// keine Fehler aufgetreten sind — <see cref="LatexReportGenerator.TryGenerate"/>
/// liefert auch bei Teilergebnissen ein nutzbares Dokument.
/// </summary>
public sealed class ReportResult
{
    /// <summary>True, wenn keine Fehler aufgetreten sind.</summary>
    public bool Success => Errors.Count == 0;

    /// <summary>Der erzeugte LaTeX-Quelltext (leer bei Totalausfall).</summary>
    public string Document { get; init; } = string.Empty;

    /// <summary>Recoverbare Fehler (fehlendes Material, leere Sammlungen, ...).</summary>
    public List<string> Errors { get; init; } = new();
}