# Anforderungsanalyse

## 1. Funktionale Anforderungen

### FR-001: Materialauswahl
Der Benutzer kann aus einer vordefinierten Liste von Halbleitermaterialien (CdSe, InP, PbS) auswählen. Jedes Material besitzt feste physikalische Parameter (Bandlücke E_g, effektive Elektronenmasse m_e*, effektive Löchermasse m_h*, statische Dielektrizitätskonstante ε).

### FR-002: Radius-Variation
Der Benutzer kann den Nanokristall-Radius R über einen Slider im Bereich 1 nm ≤ R ≤ 10 nm variieren. Alle abhängigen Berechnungen werden in Echtzeit aktualisiert.

### FR-003: Quantenmechanische Berechnung
Das System berechnet für den gewählten Radius die Energieniveaus E_n,l des Elektrons und Lochs im unendlichen sphärischen Potentialtopf (analytische Lösung der radialen Schrödinger-Gleichung). Die Bandlücke des Quantum-Dots ergibt sich aus der vollen Brus-Gleichung: E_g + ΔE_e + ΔE_h − 1,786 e²/(4πε₀εᵣR).

### FR-004: 3D-Atomstruktur
Das System generiert ein sphärisches Ausschnitt aus einem kubisch-flächenzentrierten Zinkblende-Gitter und stellt die Atompositionen (Kationen und Anionen) als farbige Kugeln in 3D dar. Die Anzahl der Atome skaliert mit dem Radius.

### FR-005: Wahrscheinlichkeitsdichte-Cloud
Das System berechnet die radiale Wahrscheinlichkeitsdichte |R_n,l(r)|² für den Grundzustand (n=1, l=0) und das erste angeregte Niveau (n=1, l=1) und stellt sie als transparente Isoflächen oder radiales Profil in der 3D-Ansicht dar.

### FR-006: Energieniveau-Diagramm
Das System zeigt ein Balkendiagramm der berechneten Elektronen- und Loch-Niveaus mit der Fermi-Kante (Vakkum-Energie) als Referenz an.

### FR-007: Emissionsspektrum
Das System berechnet aus der effektiven Bandlücke die Emissionswellenlänge λ = hc / E_QD und stellt diese als farbigen Peak in einem Spektrum dar. Die Peakposition ändert sich dynamisch mit dem Radius (Quantum-Size-Effekt).

### FR-008: LaTeX-Export
Der Benutzer kann einen technischen Bericht als `.tex`-Datei exportieren, der die verwendeten Formeln (Brus-Gleichung, radialer Potentialtopf), eingestellten Parameter, Screenshots der Visualisierung und eine Ergebnistabelle enthält.

## 2. Nicht-funktionale Anforderungen

### NFR-001: Performance
Die Berechnung und Neurenderung bei Radius-Änderung darf nicht länger als 500 ms dauern (Ziel: <200 ms für flüssige Interaktion).

### NFR-002: Korrektheit
Die berechneten Bandlücken-Verschiebungen müssen mit Literaturwerten (z. B. Bimberg et al. "Quantum Dot Heterostructures") auf ≤10 % Abweichung übereinstimmen (für R ≥ 2 nm).

### NFR-003: Wartbarkeit
Der Code folgt MVVM-Pattern für die UI-Schicht und einer Service-Schicht für die Physik-Engine. Neue Materialien sollen durch Hinzufügen eines Eintrags in eine JSON/XML-Konfiguration ergänzbar sein, ohne Core-Code zu ändern.

### NFR-004: Plattform
Das Programm läuft unter Windows 10/11 mit .NET 10 und benötigt keine externen Lizenzen oder API-Keys.

### NFR-005: Dokumentation
Jedes öffentliche Interface und jede nicht-triviale Funktion besitzt XML-Dokumentationskommentare. Das Wiki enthält ein Tutorial für Endnutzer.

## 3. Abnahmekriterien

| ID | Kriterium | Messmethode |
|---|---|---|
| AC-001 | Material CdSe liefert bei R=3 nm eine Bandlücke von ca. 2,1 eV (volle Brus-Gleichung inkl. Coulomb-Term; Literatur-Sizing-Curve Yu et al. 2003: ~2,0–2,1 eV für 6 nm Durchmesser) | Unit-Test gegen analytische Brus-Formel |
| AC-002 | Slider-Bewegung von 1 nm → 10 nm aktualisiert 3D-Ansicht, Diagramm und Spektrum innerhalb von 500 ms | Manuelle Messung mit Stoppuhr im UI |
| AC-003 | LaTeX-Export erzeugt compilierbare `.tex` mit korrekten Formeln und Einheiten | Kompilierung mit pdflatex |
| AC-004 | Projektstruktur ermöglicht Hinzufügen eines neuen Materials durch Editieren von `Data/materials.json` (ohne Core-Code-Änderung) | Unit-Test: ZnS wird nur per JSON-Konfiguration hinzugefügt und vollständig durchgerechnet |
