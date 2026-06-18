# UML-Sequenzdiagramme: Quantum Dot Studio

## 1. Radius-Änderung (Live-Vorschau)

```mermaid
sequenceDiagram
    actor Benutzer
    participant UI as SimulationViewModel
    participant Service as QuantumDotService
    participant Solver as QuantumSolver
    participant Lattice as LatticeEngine
    participant Cloud as ProbabilityCloudGenerator
    participant Renderer as QuantumDotRenderer3D
    participant Chart as MainWindow/Charts

    Benutzer->>UI: Material oder Radius ändern
    UI->>UI: ValidateRadius()
    alt Radius ungültig
        UI-->>Benutzer: ValidationMessage
    else Radius gültig
        UI->>Service: BuildQuantumDot(mat, R)
        Service->>Solver: SolveSphericalWell(R, m*)
        Solver-->>Service: List~EnergyLevel~
        Service->>Solver: BrusBandGap(mat, R)
        Solver-->>Service: totalBandGap
        Service->>Lattice: GenerateZincBlende(mat, R)
        Lattice-->>Service: List~Atom~
        Service->>Cloud: Generate(groundState, R)
        Cloud-->>Service: ElectronCloud
        Service-->>UI: QuantumDot
        UI->>UI: OnPropertyChanged(ActiveDot)
        UI-->>Chart: Energie-/Spektrum-Diagramme aktualisieren
        UI->>Renderer: BuildModel(dot, ShowLattice, ShowCloud)
        Renderer-->>UI: Model3DGroup
        UI-->>Benutzer: Live-Vorschau (3D + Diagramme + Farbe)
    end
```

## 2. LaTeX-Export mit 3D-Screenshot

```mermaid
sequenceDiagram
    actor Benutzer
    participant UI as MainWindow
    participant VM as ExportViewModel
    participant FileDlg as SaveFileDialog
    participant SH as ScreenshotHelper
    participant Gen as LatexReportGenerator
    participant FS as File System

    Benutzer->>UI: Checkbox "Screenshot einbetten"
    UI->>VM: IncludeScreenshot = true
    Benutzer->>VM: ExportCommand
    VM->>VM: CanExport()? ActiveDot != null
    VM->>FileDlg: .tex-Datei wählen
    FileDlg-->>VM: Zielpfad
    alt IncludeScreenshot && Provider != null
        VM->>VM: ScreenshotPfad = QuantumDot_{Material}_{Radius}nm_3d.png
        VM->>UI: ScreenshotProvider(pfad)
        UI->>SH: CaptureViewport(Viewport3D, pfad, 1280, 720)
        SH->>FS: PNG speichern
        SH-->>UI: true/false
        UI-->>VM: tatsächlicher Pfad
    end
    VM->>Gen: Generate(dot, screenshotPath, ...)
    Gen->>FS: screenshotPath vorhanden?
    alt Screenshot vorhanden
        Gen->>Gen: \includegraphics{Dateiname} einfügen
    else nicht vorhanden
        Gen->>Gen: Platzhalter-Kasten einfügen
    end
    Gen-->>VM: .tex Inhalt
    VM->>FS: .tex speichern
    VM->>VM: StatusMessage = Erfolg
    VM-->>Benutzer: Bestätigung
```

## Beschreibung der Interaktionen

### Radius-Änderung
1. **Trigger**: Benutzer ändert Material oder Radius in der WPF-UI.
2. **Validierung**: `SimulationViewModel` prüft, ob der Radius im gültigen Bereich liegt.
3. **Berechnung**: `QuantumDotService` ruft `QuantumSolver` für Energieniveaus und Brus-Bandlücke, `LatticeEngine` für das Gitter und `ProbabilityCloudGenerator` für die Wahrscheinlichkeitswolke.
4. **Visualisierung**: `QuantumDotRenderer3D` zeichnet Atome und Wahrscheinlichkeitswolke neu. OxyPlot-Diagramme aktualisieren sich über Data-Binding.
5. **Feedback**: Benutzer sieht 3D-Struktur, Energiediagramm und Emissionsspektrum in Echtzeit.

### LaTeX-Export
1. **Trigger**: Benutzer aktiviert optional die Screenshot-Checkbox und klickt auf Export.
2. **Dateiauswahl**: `ExportViewModel` öffnet einen `SaveFileDialog` für `.tex`.
3. **Screenshot (optional)**: Wenn aktiviert, ruft das ViewModel den registrierten `ScreenshotProvider` in `MainWindow` auf, der `ScreenshotHelper.CaptureViewport()` verwendet.
4. **Berichtsgenerierung**: `LatexReportGenerator.Generate()` erzeugt den LaTeX-Quelltext und bindet das Bild mit relativem Dateinamen ein oder fällt auf den Platzhalter zurück.
5. **Speichern**: `.tex`-Datei wird geschrieben und eine Statusmeldung angezeigt.
