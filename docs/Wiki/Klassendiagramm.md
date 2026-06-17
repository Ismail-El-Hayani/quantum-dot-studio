# UML-Klassendiagramm: Quantum Dot Studio

```mermaid
classDiagram
    direction TB

    class Material {
        +string Name
        +double BandGap_eV
        +double EffectiveMassElectron
        +double EffectiveMassHole
        +double DielectricConstant
        +double LatticeConstant_A
        +LoadFromConfig(string path)
    }

    class QuantumDot {
        +Material Material
        +double Radius_nm
        +double ConfinementEnergyElectron_eV
        +double ConfinementEnergyHole_eV
        +double TotalBandGap_eV
        +double EmissionWavelength_nm
        +CalculateEnergyLevels()
        +GetWavefunctionDensity(int n, int l, double r)
    }

    class LatticeEngine {
        +GenerateZincBlende(Material mat, double radius)
        +List~Atom~ Atoms
        +FilterInsideSphere()
    }

    class Atom {
        +string Element
        +Vector3 Position
        +double Radius
    }

    class QuantumSolver {
        +SolveSphericalWell(double R, double mStar)
        +List~EnergyLevel~ EnergyLevels
        +double BrusBandGap(Material m, double R)
    }

    class EnergyLevel {
        +int PrincipalQuantumNumber_n
        +int AngularMomentum_l
        +double Energy_eV
        +string Label
    }

    class Renderer3D {
        +RenderAtoms(List~Atom~ atoms)
        +RenderProbabilityCloud(EnergyLevel level)
        +UpdateCamera(Vector3 target)
    }

    class ChartEngine {
        +PlotEnergyLevels(List~EnergyLevel~ levels)
        +PlotSpectrum(double lambda)
    }

    class LaTeXExporter {
        +ExportReport(QuantumDot qd)
        +string GenerateTexContent()
        +SaveToFile(string path)
    }

    class MainViewModel {
        +Material SelectedMaterial
        +double RadiusSlider
        +QuantumDot ActiveDot
        +Command UpdateCommand
        +Command ExportCommand
    }

    Material --> QuantumDot : verwendet
    QuantumDot --> QuantumSolver : delegiert Berechnung
    QuantumDot --> LatticeEngine : generiert Geometrie
    LatticeEngine --> Atom : erzeugt
    QuantumSolver --> EnergyLevel : berechnet
    MainViewModel --> QuantumDot : hält Instanz
    MainViewModel --> Renderer3D : aktualisiert
    MainViewModel --> ChartEngine : aktualisiert
    MainViewModel --> LaTeXExporter : ruft auf
```

## Architektur-Erklärung

| Schicht | Klassen | Aufgabe |
|---------|---------|---------|
| Domain | Material, QuantumDot, EnergyLevel, Atom | Reine Datenmodelle mit physikalischer Logik |
| Core/Services | QuantumSolver, LatticeEngine | Berechnungsalgorithmen, unabhängig von UI |
| Infrastructure | Renderer3D (Helix Toolkit), ChartEngine (OxyPlot), LaTeXExporter | Technologie-spezifische Ausgaben |
| Presentation (MVVM) | MainViewModel | Vermittlung zwischen UI und Core, Data-Binding |
