# Entwicklungszyklus

## Phase 1: Initiierung (bis 29. Juni 2026)

- [x] GitHub-Repository anlegen
- [x] README.md mit Projektbeschreibung erstellen
- [x] Wiki-Startseite (Idee & Zielstellung) schreiben
- [x] Repository auf GitHub veröffentlicht
- [x] Entwicklungszyklus-Dokumentation im Wiki ergänzen

## Phase 2: Spezifikation & Entwurf (bis 6. Juli 2026)

- [x] Detaillierte Fragestellung formulieren: Welche physikalischen Modelle (unendlicher vs. endlicher Potentialtopf? Welche Materialparameter?)
- [x] Anforderungsanalyse: Funktionale / nicht-funktionale Anforderungen
- [x] UML-Klassendiagramm: Domänenmodell (Material, QuantumDot, ElektronenZustand, LatticeEngine, Solver, Renderer)
- [x] UML-Sequenzdiagramm: Interaktion "Benutzer ändert Radius → System berechnet → Renderer aktualisiert"
- [x] Softwarearchitektur festlegen: MVVM-Pattern für WPF, Service-Driven Architecture für Core
- [ ] **Konzept-Review durch externe Fachleute einholen** *(offen)*

## Phase 3: Implementierung (6. Juli – 30. Juli 2026)

### Sprint A: Domänenmodell & Solver (Woche 1) — abgeschlossen
- [x] Projektstruktur in Visual Studio anlegen (.sln + 5 Projekte)
- [x] Material-Datenbank (CdSe, InP, PbS: Bandlücke, effektive Massen, Dielektrizitätskonstante)
- [x] Lattice-Generator (Zinkblende-Gitter, sphärische Ausschnittlogik)
- [x] Quantenmechanischer Solver (Lösung Schrödinger-Gleichung in Kugelkoordinaten, Eigenwertberechnung)
- [x] Unit-Tests für Solver-Modul

### Sprint B: Renderer & UI (Woche 2) — abgeschlossen
- [x] Helix-Toolkit-Integration in WPF
- [x] 3D-Gitterdarstellung (Atompositionen als farbige Kugeln)
- [x] Wahrscheinlichkeitsdichte-Cloud (Punktwolke für 1S-Elektronenzustand)
- [x] UI-Controls: Material-Auswahl, Radius-Slider, Energieanzeige
- [x] Farbschema für Atomtypen implementieren

### Sprint C: Diagramme & Export (Woche 3) — abgeschlossen
- [x] OxyPlot-Integration für Energieniveau-Diagramm
- [x] Emissionsspektrum-Plot (Größenabhängige Bandlücken -> Peak-Verschiebung)
- [x] LaTeX-Templating-Engine (String-basiert)
- [x] Berichtsexport: Parameter, Formeln, Ergebnistabelle (Screenshot-Platzhalter vorbereitet)

### Sprint D: Integration & Polishing (Woche 4) — in Arbeit

- [x] **MVVM & Validierung**
  - [x] `MainViewModel` in kleinere ViewModels aufsplitten (z. B. `SimulationViewModel`, `ExportViewModel`)
  - [x] Radius-Validierung (1 nm ≤ R ≤ 10 nm) mit Fehlermeldung
  - [x] Null-Prüfungen für Material/QuantumDot in Commands
- [x] **Performance**
  - [x] Caching der berechneten Energieniveaus pro (Material, Radius)
  - [ ] Lazy Loading für Gitter bei großen Radien (optional)
- [x] **Dokumentation**
  - [x] README: Build-Anleitung und .NET-Version auf 10 aktualisieren
  - [x] Wiki-Seite: Tutorial für Endnutzer
  - [x] Wiki-Seite: API-Beschreibung der Module (Core, Solver, Renderer, Reports)
- [x] **LaTeX-Export finalisieren**
  - [x] Screenshot der 3D-Ansicht in Bericht einbetten (Optional)
  - [x] Bericht mit pdflatex testen
- [x] **KI-Nutzungsdokumentation finalisieren**
  - [x] Liste der KI-unterstützten Commits/Dateien ergänzen

## Phase 4: Abschluss des initialen Entwicklungszyklus — abgeschlossen

- [x] Finaler Build getestet (`dotnet build`, `dotnet test`)
- [x] CI: GitHub Actions Workflow baut und testet bei jedem Push/PR (`.github/workflows/dotnet.yml`)
- [x] UML-Diagramme final
- [x] Repository in eigenes Konto überführt (quantum-dot-studio)

> Die weitere Entwicklung folgt dem Plan in [`docs/ROADMAP.md`](../ROADMAP.md)
> (Core/Shell-Physik, Sensormodi, Wahrscheinlichkeits-Engine).

### Qualitätsverbesserungen (Review-Runde)

- [x] Volle Brus-Gleichung mit Coulomb-Term implementiert (`QuantumSolver.CoulombEnergy`), Literatur-Abgleich mit Yu et al. 2003
- [x] `EnergyLevel.Particle`-Enum ersetzt String-Präfix-Parsing (`e-`/`h-`)
- [x] Materialdatenbank JSON-basiert: `Data/materials.json` wird geladen statt Hardcode (NFR-003, AC-004)
- [x] README-Build-Anleitung korrigiert (`.slnx` statt veralteter `.sln`-Pfade)
- [x] Tote Platzhalter-Klassen entfernt (`Class1.cs` ×2, `UnitTest1.cs`), inkl. totem Code in `ConfinementEnergy`
- [x] Anforderungsanalyse aktualisiert: FR-003 (volle Brus-Gleichung), NFR-004 (.NET 10), AC-001 (Literaturwert 2,1 eV), AC-004 (JSON-Test statt Code-Review)

### Aktueller Stand 3D-Ansicht

- [x] Radius-Slider aktualisiert das 3D-Modell live (`UpdateSourceTrigger=PropertyChanged`).
- [x] Kamera wird einmalig so gesetzt, dass Radius-Änderungen das QD sichtbar wachsen/schrumpfen lassen.
- [x] Materialwechsel wechselt die Atomelemente und damit die Farben.
- [x] "3D-Ansicht zurücksetzen"-Button setzt die Kamera auf die Ausgangsansicht zurück.
- [x] Checkboxes "Atomgitter" / "Wahrscheinlichkeitswolke" blenden die jeweiligen Geometrien aus.

## KI-Nutzung (laufend dokumentieren)

| KI-Tool | Einsatzzweck | Beitrag |
|---------|-----------|---------|
| GitHub Copilot / Claude Code | Code-Vorschläge, Boilerplate-Generierung für MVVM-Pattern, LaTeX-Template-Struktur | Beschleunigung repetitive Aufgaben |
| ChatGPT / Claude | Physikalische Formeln prüfen, LaTeX-Syntax validieren | Qualitätssicherung Dokumentation |
| Sonstige | (wird ergänzt) | |

> **Hinweis**: KI wird als Werkzeug eingesetzt, nicht als Ersatz für eigenständiges Design. Alle wesentlichen Architekturentscheidungen und komplexen Algorithmen werden eigenständig entwickelt und durch Unit-Tests verifiziert.
