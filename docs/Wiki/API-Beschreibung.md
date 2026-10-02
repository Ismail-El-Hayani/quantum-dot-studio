# API-Beschreibung der Module

Diese Seite beschreibt die öffentliche Schnittstelle der fünf Projekte im Quantum Dot Studio.

## QuantumDotStudio.Core

Domänenmodelle und Datenhaltung. Keine externen UI-Abhängigkeiten.

### Klassen

| Klasse | Zweck |
|--------|-------|
| `Material` | Physikalische Parameter eines Halbleiters: Bandlücke, effektive Massen, Gitterkonstante, Kation/Anion. |
| `QuantumDot` | Zentrales Ergebnismodell: Atome, Energieniveaus, Elektronenwolke, Bandlücke, Wellenlänge. |
| `CoreShellQuantumDot` | Core/Shell-Heterostruktur: Shell-Material, Core-Radius, Schalendicke, Band-Offsets, Strain-Größen. |
| `Atom` | Ein Atom im Zinkblende-Gitter mit Element, Position und Darstellungsradius. |
| `EnergyLevel` | Ein berechnetes Energieniveau mit Quantenzahlen, Energie, Label und Teilchentyp (`Particle`). |
| `Vector3` | Eigener 3D-Vektor für Positionen (unabhängig von UI-Bibliotheken). |
| `MaterialDatabase` | Lädt Materialien aus `Data/materials.json` (Lazy, thread-sicher) mit eingebautem Fallback; `LoadFromFile`/`SaveToFile` für eigene Dateien. |

## QuantumDotStudio.Solver

Physikalische Berechnungen und Zusammenbau des `QuantumDot`-Objekts.

### Klassen

| Klasse | Zweck |
|--------|-------|
| `QuantumDotService` | Orchestriert Gitter, Solver und Cloud-Generator; cachet Ergebnisse pro (Material, Radius). |
| `QuantumSolver` | Berechnet Confinement-Energien, Brus-Bandlücke, Wellenlänge und Energieniveaus. |
| `LatticeEngine` | Erzeugt Zinkblende-Atompositionen innerhalb eines sphärischen Radius. |
| `ProbabilityCloudGenerator` | Berechnet eine diskrete 1S-Elektronen-Wahrscheinlichkeitsdichte-Wolke. |

### Wichtige Methoden

```csharp
QuantumDot BuildQuantumDot(Material material, double radius_nm, int maxLevels = 6);
CoreShellQuantumDot BuildCoreShellQuantumDot(Material core, Material shell, double coreRadius_nm, double shellThickness_nm, int maxStates = 3);
double ConfinementEnergy(double radius_nm, double effectiveMass, int n, int l);
double CoulombEnergy(double radius_nm, double dielectricConstant);
double BrusBandGap(Material material, double radius_nm);
double WavelengthFromBandGap(double bandGap_eV);
List<double> FindSEnergies_eV(double radius_nm, double mStar, double barrier_eV, int maxStates);
```

### Core/Shell-Klassen

| Klasse | Zweck |
|--------|-------|
| `FiniteWellSolver` | Gebundene S-Zustände im endlichen sphärischen Topf (k·cot(kR) = −κ, Bisektion je Zustand). |
| `BandAlignment` | Band-Offsets aus Elektronenaffinitäten: V0_e = χ_core − χ_shell, V0_h = (Eg_shell − Eg_core) − V0_e. |
| `StrainModel` | Gitterfehlanpassung f und kritische Schalendicke t_c ≈ b/(2·\|f\|) (Matthews–Blakeslee, b = 0,3 nm). |
| `CoreShellQuantumDot` | Core/Shell-Modell: Shell-Material, Dicken, Barrieren, Strain-Größen (erbt `QuantumDot`). |

## QuantumDotStudio.Renderer

WPF-3D-Visualisierung mit HelixToolkit.

### Klassen

| Klasse | Zweck |
|--------|-------|
| `QuantumDotRenderer3D` | Baut aus einem `QuantumDot` eine `Model3DGroup` mit Atomen und Wolke. |
| `AtomPalette` | Farbzuordnung für chemische Elemente. |

### Wichtige Methoden

```csharp
Model3DGroup BuildModel(QuantumDot dot, bool showLattice = true, bool showCloud = true);
Rect3D GetBounds(QuantumDot dot);
```

## QuantumDotStudio.Reports

LaTeX-Berichtsgenerierung per String-Templating.

### Klassen

| Klasse | Zweck |
|--------|-------|
| `LatexReportGenerator` | Statische Generator-Klasse für `.tex`-Inhalte. |

### Wichtige Methode

```csharp
string Generate(QuantumDot dot);
```

## QuantumDotStudio.WPF

Hauptanwendung mit MVVM.

### ViewModels

| Klasse | Zweck |
|--------|-------|
| `MainViewModel` | Aggregiert `SimulationViewModel` und `ExportViewModel`. |
| `SimulationViewModel` | Material, Radius, Validierung, Ergebnisse, Plots, 3D-Toggles. |
| `ExportViewModel` | LaTeX-Export, Statusmeldung. |
| `RelayCommand` | `ICommand`-Implementierung für Button-Bindings. |
| `LegendItem` | Eintrag für die Farblegende. |

### Views

| Klasse | Zweck |
|--------|-------|
| `MainWindow` | Hauptfenster mit Sidebar und 3D-/Diagramm-Bereich. |
| `App` | Anwendungsstart, globale Ressourcen (z. B. Converter). |

## Abhängigkeiten zwischen den Projekten

```
WPF
 ├─ Core
 ├─ Solver
 ├─ Renderer
 └─ Reports

Solver  ──▶ Core
Renderer ──▶ Core
Reports  ──▶ Core
Tests    ──▶ Core, Solver, Renderer, Reports
```

## Erweiterung

- Neue Materialien: Eintrag in `Data/materials.json` ergänzen — keine Code-Änderung nötig (getestet in `MaterialDatabaseTests`).
- Neue Visualisierung: Methode in `QuantumDotRenderer3D.BuildModel` erweitern.
- Neue Diagramme: Methode in `PlotFactory` hinzufügen und in `SimulationViewModel` binden.
