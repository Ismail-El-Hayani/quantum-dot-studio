Quantum Dot Studio

Interaktive 3D-Visualisierung und Simulation von Halbleiter-Quantum Dots in C# (WPF).



Ziel ist die Entwicklung eines C#-Desktop-Tools (WPF), das die Struktur, elektronischen Zustände und optischen Eigenschaften von Quantum Dots interaktiv simuliert und visualisiert.

## Technologie-Stack

- **Sprache**: C# (.NET 10)
- **UI**: WPF (Windows Presentation Foundation)
- **3D-Rendering**: HelixToolkit.Wpf v3
- **Charting**: OxyPlot.Wpf v2
- **LaTeX-Export**: String-Templating + automatische `.tex`-Generierung
- **Tests**: xUnit

## Repository-Struktur

```
├── docs/                   # Dokumentation (Wiki-Quellen, UML)
│   ├── ROADMAP.md               # Sensor-Design-Plattform: Entwicklungsplan
│   ├── Wiki/
│   │   ├── Home.md              # Projektidee und Zielstellung
│   │   ├── Entwicklungszyklus.md
│   │   ├── Fragestellung.md
│   │   ├── Anforderungsanalyse.md
│   │   ├── Klassendiagramm.md
│   │   ├── Sequenzdiagramm.md
│   │   └── KI-Nutzung.md
│   └── UML/
│       ├── Klassendiagramm.md
│       └── Sequenzdiagramm.md
├── src/
│   ├── QuantumDotStudio.Core/          # Domänenmodelle, Materialdatenbank
│   ├── QuantumDotStudio.Renderer/      # 3D-Visualisierung (Helix Toolkit)
│   ├── QuantumDotStudio.Solver/        # Quantenmechanische Berechnungen
│   ├── QuantumDotStudio.Reports/       # LaTeX-Reportgenerator
│   ├── QuantumDotStudio.Tests/          # Unit-Tests (xUnit)
│   ├── QuantumDotStudio.WPF/            # Hauptanwendung (WPF)
│   └── QuantumDotStudio.slnx            # Solution-Datei
└── README.md
```

## Voraussetzungen

- Windows 10/11
- [.NET 10 SDK](https://dotnet.microsoft.com/download/dotnet/10.0)
- Visual Studio 2022, JetBrains Rider oder `dotnet` CLI

## Build

Im Projekt-Stammverzeichnis (oder in PowerShell):

```powershell
cd src
dotnet build QuantumDotStudio.slnx
```

Oder direkt in der Solution-Datei aus Visual Studio heraus öffnen:

```
src\QuantumDotStudio.slnx
```

## Test

```powershell
cd src
dotnet test QuantumDotStudio.slnx
```

## Ausführen

Nach erfolgreichem Build findet sich die Startanwendung unter:

```
src\QuantumDotStudio.WPF\bin\Debug\net10.0-windows\QuantumDotStudio.WPF.exe
```

## Hauptfunktionen

- **Materialauswahl**: CdSe, InP, PbS, CdS, ZnSe, ZnS (erweiterbare JSON-Materialdatenbank)
- **Core/Shell-Modus**: Heterostrukturen mit endlichem Potentialtopf, Band-Offsets und Strain-Analyse (kritische Schalendicke)
- **Radius-Steuerung**: 1 nm bis 10 nm mit Live-Validierung
- **3D-Gitter**: Zinkblende-Struktur mit farbkodierten Atomen (homogen oder Kern/Hülle)
- **Wahrscheinlichkeitswolke**: 1S-Elektronen-Grundzustand (ein-/ausschaltbar)
- **Energieniveau-Diagramm**: Elektron- und Loch-Niveaus im Quantum Dot
- **Emissionsspektrum**: Größenabhängige Peak-Verschiebung
- **Bandkantenprofil**: Radiales Leitungs-/Valenzbandprofil der Core/Shell-Struktur mit gebundenen Niveaus
- **LaTeX-Export**: Automatischer Bericht mit Parametern, Formeln, Strain-Status und Ergebnissen

## Projektstatus

Eigenständiges Projekt, aktiv entwickelt. Aktueller Stand: **Core/Shell-Physik und -UI fertig** (Phase 1 des Entwicklungsplans), laufend erweitert Richtung Sensor-Design-Plattform — siehe [docs/ROADMAP.md](docs/ROADMAP.md).

## Git Workflow

- **Branching**: `feature/...`, `bugfix/...`, `docs/...`
- **Commits**: Klare, deskriptive Nachrichten auf Deutsch oder Englisch
- **Pull Requests**: Beschreibend, mit Bezug zum umgesetzten Feature



