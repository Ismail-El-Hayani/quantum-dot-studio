namespace QuantumDotStudio.Core.Models;

/// <summary>
/// Teilchentyp, zu dem ein berechnetes Energieniveau gehört.
/// Ersetzt das frühere String-Präfix ("e-", "h-") im Label.
/// </summary>
public enum Particle
{
    /// <summary>Leitungsbands-Elektron.</summary>
    Electron,

    /// <summary>Valenzband-Loch.</summary>
    Hole
}