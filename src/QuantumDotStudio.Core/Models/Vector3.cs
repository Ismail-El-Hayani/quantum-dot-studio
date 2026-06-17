namespace QuantumDotStudio.Core.Models;

/// <summary>
/// Einfacher 3D-Vektor mit double-Komponenten.
/// </summary>
public readonly record struct Vector3(double X, double Y, double Z)
{
    public double LengthSquared => X * X + Y * Y + Z * Z;

    public double Length => Math.Sqrt(LengthSquared);

    public static Vector3 operator +(Vector3 a, Vector3 b) => new(a.X + b.X, a.Y + b.Y, a.Z + b.Z);

    public static Vector3 operator -(Vector3 a, Vector3 b) => new(a.X - b.X, a.Y - b.Y, a.Z - b.Z);

    public static Vector3 operator *(Vector3 v, double s) => new(v.X * s, v.Y * s, v.Z * s);
}
