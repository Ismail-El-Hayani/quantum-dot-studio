# Entwicklungszyklus

## Phase 1: Initiierung (bis 29. Juni 2026)

- [x] GitHub-Repository anlegen
- [x] README.md mit Projektbeschreibung erstellen
- [x] Wiki-Startseite (Idee & Zielstellung) schreiben
- [x] Repository auf GitHub veröffentlicht
- [x] Betreuer (Prof. Zug, Herr Göhler) als Collaborator einladen
- [x] Entwicklungszyklus-Dokumentation im Wiki ergänzen

## Phase 2: Spezifikation & Entwurf (bis 6. Juli 2026)

- [x] Detaillierte Fragestellung formulieren: Welche physikalischen Modelle (unendlicher vs. endlicher Potentialtopf? Welche Materialparameter?)
- [x] Anforderungsanalyse: Funktionale / nicht-funktionale Anforderungen
- [x] UML-Klassendiagramm: Domänenmodell (Material, QuantumDot, ElektronenZustand, LatticeEngine, Solver, Renderer)
- [x] UML-Sequenzdiagramm: Interaktion "Benutzer ändert Radius → System berechnet → Renderer aktualisiert"
- [x] Softwarearchitektur festlegen: MVVM-Pattern für WPF, Service-Driven Architecture für Core
- [ ] **Bestätigung der Idee und des Entwurfs durch Betreuer einholen** *(offen — Betreuer wurde eingeladen)*

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
- [ ] **Performance**
  - [ ] Caching der berechneten Energieniveaus pro (Material, Radius)
  - [ ] Lazy Loading für Gitter bei großen Radien (optional)
- [ ] **Dokumentation**
  - [ ] README: Build-Anleitung und .NET-Version auf 10 aktualisieren
  - [ ] Wiki-Seite: Tutorial für Endnutzer
  - [ ] Wiki-Seite: API-Beschreibung der Module (Core, Solver, Renderer, Reports)
- [ ] **LaTeX-Export finalisieren**
  - [ ] Screenshot der 3D-Ansicht in Bericht einbetten (Optional)
  - [ ] Bericht mit pdflatex testen
- [ ] **KI-Nutzungsdokumentation finalisieren**
  - [ ] Liste der KI-unterstützten Commits/Dateien ergänzen

## Phase 4: Abschluss (30. Juli 2026)

- [ ] Finaler Build getestet (`dotnet build`, `dotnet test`)
- [ ] Alle Wiki-Seiten vollständig
- [ ] UML-Diagramme final
- [ ] Letzter Commit auf main, Tag `v1.0.0`
- [ ] Repository-Archivierung durch Betreuer

## KI-Nutzung (laufend dokumentieren)

| KI-Tool | Einsatzzweck | Beitrag |
|---------|-----------|---------|
| GitHub Copilot / Claude Code | Code-Vorschläge, Boilerplate-Generierung für MVVM-Pattern, LaTeX-Template-Struktur | Beschleunigung repetitive Aufgaben |
| ChatGPT / Claude | Physikalische Formeln prüfen, LaTeX-Syntax validieren | Qualitätssicherung Dokumentation |
| Sonstige | (wird ergänzt) | |

> **Hinweis**: KI wird als Werkzeug eingesetzt, nicht als Ersatz für eigenständiges Design. Alle wesentlichen Architekturentscheidungen und komplexen Algorithmen werden eigenständig entwickelt und durch Unit-Tests verifiziert.
