# UML-Sequenzdiagramm: Radius-Änderung

```mermaid
sequenceDiagram
    actor Benutzer
    participant UI as MainViewModel
    participant Solver as QuantumSolver
    participant Lattice as LatticeEngine
    participant Renderer as Renderer3D
    participant Chart as ChartEngine

    Benutzer->>UI: Slider bewegen (Radius ändern)
    UI->>Solver: CalculateEnergyLevels(R, Material)
    Solver->>Solver: Brus-Formel anwenden
    Solver->>Solver: Radiale Eigenwerte berechnen
    Solver-->>UI: EnergyLevels, Bandlücke, Wellenlänge

    UI->>Lattice: GenerateZincBlende(R, Material)
    Lattice->>Lattice: Unendliches Gitter erzeugen
    Lattice->>Lattice: FilterInsideSphere()
    Lattice-->>UI: List~Atom~

    UI->>Renderer: RenderAtoms(AtomList)
    UI->>Renderer: RenderProbabilityCloud(Grundzustand)
    Renderer-->>UI: 3D-Szene aktualisiert

    UI->>Chart: PlotEnergyLevels(EnergyLevels)
    UI->>Chart: PlotSpectrum(EmissionWellenlaenge)
    Chart-->>UI: Diagramme aktualisiert

    UI-->>Benutzer: Live-Vorschau (3D + Diagramme + Farbe)
```

## Beschreibung der Interaktion

1. **Trigger**: Benutzer bewegt den Radius-Slider.
2. **Berechnung**: QuantumSolver berechnet Energieniveaus (Brus-Korrektur) und Emissionswellenlänge.
3. **Geometrie**: LatticeEngine generiert neues sphärisches Gitterstück.
4. **Visualisierung**: Renderer3D zeichnet Atome und Wahrscheinlichkeitswolke neu. ChartEngine aktualisiert Energiediagramm und Spektrum.
5. **Feedback**: Benutzer sieht alle drei Darstellungen in Echtzeit.
