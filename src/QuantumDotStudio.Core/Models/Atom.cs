namespace QuantumDotStudio.Core.Models;

/// <summary>
/// Ein Atom im Zinkblende-Gitterausschnitt.
/// </summary>
public class Atom
{
    /// <summary>
    /// Chemisches Element-Symbol.
    /// </summary>
    public string Element { get; set; } = string.Empty;

    /// <summary>
    /// Position in nm.
    /// </summary>
    public Vector3 Position { get; set; }

    /// <summary>
    /// Darstellungsradius für die 3D-Kugel in nm.
    /// </summary>
    public double Radius_nm { get; set; } = 0.1;
}
