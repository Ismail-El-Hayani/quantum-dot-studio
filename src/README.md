# Quantum Dot Studio

## .NET-Version

Das Projekt wird mit **.NET 10** entwickelt, da dies die aktuell installierte SDK-Version im Entwicklungsumgebung ist.

## Solution-Struktur

| Projekt | Zweck |
|---------|-------|
| `QuantumDotStudio.Core` | Domänenmodelle, Materialdatenbank, gemeinsame Typen |
| `QuantumDotStudio.Solver` | Quantenmechanische Berechnungen (Brus-Formel, sphärischer Potentialtopf) |
| `QuantumDotStudio.Renderer` | 3D-Visualisierung (Helix Toolkit) |
| `QuantumDotStudio.Reports` | LaTeX-Export, Plotting-Vorbereitung |
| `QuantumDotStudio.WPF` | Hauptanwendung mit MVVM |
| `QuantumDotStudio.Tests` | xUnit-Tests für Solver und Core |

## Build

```bash
cd src
/mnt/c/Program\ Files/dotnet/dotnet.exe build QuantumDotStudio.sln
```

## Test

```bash
cd src
/mnt/c/Program\ Files/dotnet/dotnet.exe test QuantumDotStudio.sln
```
