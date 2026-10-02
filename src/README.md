# Quantum Dot Studio

## .NET-Version

Das Projekt wird mit **.NET 10** entwickelt, da dies die aktuell installierte SDK-Version in der Entwicklungsumgebung ist.

## Solution-Struktur

| Projekt | Zweck |
|---------|-------|
| `QuantumDotStudio.Core` | Domänenmodelle, Materialdatenbank (JSON-basiert), gemeinsame Typen |
| `QuantumDotStudio.Solver` | Quantenmechanische Berechnungen (Brus-Formel mit Coulomb-Term, sphärischer Potentialtopf) |
| `QuantumDotStudio.Renderer` | 3D-Visualisierung (Helix Toolkit) |
| `QuantumDotStudio.Reports` | LaTeX-Export, Plotting-Vorbereitung |
| `QuantumDotStudio.WPF` | Hauptanwendung mit MVVM |
| `QuantumDotStudio.Tests` | xUnit-Tests für Solver, Core, Renderer und Reports |

## Build

```bash
cd src
dotnet build QuantumDotStudio.slnx
```

## Test

```bash
cd src
dotnet test QuantumDotStudio.slnx
```

## Materialien hinzufügen

Die Materialdatenbank liegt in `src/QuantumDotStudio.Core/Data/materials.json`
und wird ins Ausgabeverzeichnis kopiert. Neue Materialien (z. B. ZnS, CdTe)
können dort ohne Code-Änderung ergänzt werden:

```json
{
  "Name": "ZnS",
  "BandGap_eV": 3.68,
  "EffectiveMassElectron": 0.25,
  "EffectiveMassHole": 0.59,
  "DielectricConstant": 8.9,
  "LatticeConstant_A": 5.41,
  "Cation": "Zn",
  "Anion": "S"
}
```

Fehlt die Datei zur Laufzeit, greift ein eingebauter Fallback
(CdSe, InP, PbS) in `MaterialDatabase`.