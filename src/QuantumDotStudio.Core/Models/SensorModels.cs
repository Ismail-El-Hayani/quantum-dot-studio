namespace QuantumDotStudio.Core.Models;

/// <summary>
/// Funktionalisierungsligand: verankert am QD-Oberflaechenkation, definiert
/// Abstand und Ladung des Rezeptors und damit die FRET-Geometrie sowie die
/// kolloidale Stabilitaet.
/// </summary>
public class Ligand
{
    public string LigandId { get; set; } = "";
    public string DisplayName { get; set; } = "";
    /// <summary>Bindungsatom am QD-Oberflaechenkation (S, P, O, N, ...).</summary>
    public string AnchorAtom { get; set; } = "";
    /// <summary>Ausgestreckte Ligandlaenge in nm (Abstand QD-Oberflaeche bis Rezeptor).</summary>
    public double Length_nm { get; set; }
    /// <summary>Nettoladung des Liganden in Einheiten von e.</summary>
    public int Charge_e { get; set; }
    /// <summary>Oberflaechenkationen, an die dieser Ligand bindet.</summary>
    public List<string> Targets { get; set; } = new();
    /// <summary>"aqueous" oder "organic" — Medium, in dem der Ligand stabilitaet verleiht.</summary>
    public string Solubility { get; set; } = "aqueous";
}

/// <summary>
/// Analyt mit Wechselwirkungsmodell. Der Mode entscheidet ueber die
/// verwendete Physik: FRET (Foerster-Radius R0), Quenching (Stern-Volmer-
/// Konstante), Charge (Nernst-Steigung), PET (Elektronenaffinitaet vs. Bandkanten).
/// </summary>
public class Analyte
{
    public string AnalyteId { get; set; } = "";
    public string DisplayName { get; set; } = "";
    /// <summary>Transduktionsmodus: "FRET", "Quenching", "Charge", "PET".</summary>
    public string Mode { get; set; } = "";
    /// <summary>Foerster-Radius R0 in nm (nur Mode=FRET).</summary>
    public double ForsterRadius_nm { get; set; }
    /// <summary>Stern-Volmer-Konstante K_SV in M^-1 (nur Mode=Quenching).</summary>
    public double SternVolmerConstant_M { get; set; }
    /// <summary>Nernst-Steigung in mV pro Dekade (nur Mode=Charge).</summary>
    public double NernstSlope_mV_per_decade { get; set; }
    /// <summary>Redoxpotential E0 in V vs. NHE (nur Mode=PET).</summary>
    public double RedoxPotential_V { get; set; }
    /// <summary>Freie Liternotiz (Quelle der Konstanten).</summary>
    public string Notes { get; set; } = "";
}

/// <summary>
/// Sensor-Transduktionsmodus. Jeder Modus ist eine reine Physik-Funktion
/// (Roadmap-Regel 1: keine Faktoren ohne Test).
/// </summary>
public enum SensorMode
{
    FRET,
    Quenching,
    Charge,
    PET
}