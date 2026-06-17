# SoftwareentwicklungSoSe2026 — Quantum Dot Studio

Interaktive 3D-Visualisierung und Simulation von Halbleiter-Quantum Dots in C# (WPF).

## Projektbeschreibung

Dieses Repository enthält die praktische Prüfungsleistung für die Vorlesung **Softwareentwicklung** im Sommersemester 2026 an der TU Bergakademie Freiberg.

Ziel ist die Entwicklung eines C#-Desktop-Tools (WPF), das die Struktur, elektronischen Zustände und optischen Eigenschaften von Quantum Dots interaktiv simuliert und visualisiert.

## Technologie-Stack

- **Sprache**: C# (.NET 10)
- **UI**: WPF (Windows Presentation Foundation)
- **3D-Rendering**: Helix Toolkit (WPF)
- **Mathematik / Numerik**: Math.NET Numerics (vorbereitet)
- **Charting**: OxyPlot.Wpf v2
- **LaTeX-Export**: String-Templating + automatische `.tex`-Generierung

## Repository-Struktur

```
├── docs/                   # Dokumentation (Wiki-Clone, UML, Screenshots)
│   ├── Wiki/
│   │   ├── Home.md         # Projektidee und Zielstellung
│   │   ├── Entwicklungszyklus.md
│   │   ├── Fragestellung.md
│   │   ├── Anforderungsanalyse.md
│   │   └── KI-Nutzung.md
│   └── UML/
│       ├── Klassendiagramm.md
│       └── Sequenzdiagramm.md
├── src/
│   ├── QuantumDotStudio.Core/          # Domänenmodelle, Physik-Engine, Parser
│   ├── QuantumDotStudio.Renderer/      # 3D-Visualisierung (Helix Toolkit)
│   ├── QuantumDotStudio.Solver/        # Quantenmechanische Berechnungen
│   ├── QuantumDotStudio.Reports/       # LaTeX-Export
│   ├── QuantumDotStudio.Tests/           # Unit-Tests (xUnit)
│   └── QuantumDotStudio.WPF/           # Hauptanwendung (WPF)
└── README.md
```

## Getting Started

### Voraussetzungen

- Windows 10/11
- .NET 10 SDK
- Visual Studio 2022 (oder JetBrains Rider)

### Build

```bash
cd src/QuantumDotStudio.WPF
dotnet restore
dotnet build
```

### Test

```bash
cd "C:\Users\dimar\OneDrive\Desktop\Master Nano\NANO SS26\Softwareentwicklung\Quantum Dots\dimar"
dotnet test QuantumDotStudio.slnx
```

## Projektstatus

Aktueller Stand: **Phase 3, Sprint D — Integration & Polishing**.

## Git Workflow

- **Branching**: `feature/...`, `bugfix/...`, `docs/...`
- **Commits**: Klare, deskriptive Nachrichten auf Deutsch oder Englisch
- **Pull Requests**: Beschreibend, mit Bezug zum umgesetzten Feature

## Betreuer

- **Prof. Zug**
- **Herr Göhler**

---

> Dieses Projekt ist Teil der praktischen Prüfungsleistung und ersetzt die schriftliche Klausur.
