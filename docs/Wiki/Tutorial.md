# Tutorial für Endnutzer

Diese Seite erklärt die Bedienung des **Quantum Dot Studio** Schritt für Schritt.

## 1. Start der Anwendung

Nach dem Build liegt die Startdatei unter:

```
src\QuantumDotStudio.WPF\bin\Debug\net10.0-windows\QuantumDotStudio.WPF.exe
```

Doppelklicken Sie auf die Datei oder starten Sie sie über `dotnet run` im Projektverzeichnis `QuantumDotStudio.WPF`.

## 2. Hauptfenster

Das Fenster ist in zwei Bereiche aufgeteilt:

| Bereich | Inhalt |
|---------|--------|
| Linke Seite | Steuerung, Ergebnisse, 3D-Toggles, Legende |
| Rechte Seite | 3D-Ansicht oben, Diagramme unten |

## 3. Simulation einstellen

1. **Material wählen**: Wählen Sie im Dropdown eines der vordefinierten Materialien aus (CdSe, InP, PbS).
2. **Radius einstellen**: Verschieben Sie den Slider zwischen **1 nm** und **10 nm**.
   - Werte außerhalb des Bereichs werden rot markiert und mit einer Fehlermeldung abgewiesen.
3. **Berechnen**: Klicken Sie auf **Berechnen**, um die Simulation neu zu starten.
   - Änderungen an Material oder Radius lösen automatisch eine Neuberechnung aus.

> Hinweis für Fortgeschrittene: Die Materialliste stammt aus `Data/materials.json`.
> Sie können dort weitere Halbleiter (z. B. ZnS oder CdTe) ergänzen, ohne den
> Programmcode zu ändern — nach dem Neustart erscheinen sie im Dropdown.

## 4. Core/Shell-Modus

Für realistischere Nanostruktur-Simulationen kann eine Schale aktiviert werden:

1. Aktivieren Sie die Checkbox **Shell aktiv (Core/Shell-Heterostruktur)**.
2. Wählen Sie ein **Shell-Material** (z. B. CdS oder ZnS für einen CdSe-Core).
3. Stellen Sie die **Schalendicke** über den Slider ein (0–5 nm).

Im Shell-Modus wird der Radius-Slider zum **Core-Radius**, und die Software berechnet zusätzlich:

- Band-Offsets an der Grenzfläche (aus Elektronenaffinitäten)
- Gebundene Zustände im **endlichen** Potentialtopf (statt des unendlichen)
- Gitterfehlanpassung und kritische Schalendicke (Strain-Analyse)
- Im Ergebnispanel erscheint der Strain-Status: **kohärent** (gut) oder **kritisch** (Versetzungen wahrscheinlich)
- Der Tab **Bandprofil** zeigt das radiale Leitungs-/Valenzbandprofil mit den gebundenen Niveaus

## 5. 3D-Ansicht anpassen

Unterhalb des Radius-Sliders befindet sich der Bereich **3D-Ansicht**:

- **Atomgitter anzeigen**: Schaltet die Darstellung der Zinkblende-Atome ein/aus.
- **Wahrscheinlichkeitswolke anzeigen**: Schaltet die 1S-Elektronenwolke ein/aus.

Die Farblegende zeigt, welche Farbe welchem Element (Kation/Anion) zugeordnet ist.

## 6. Ergebnisse lesen

Im Panel **Ergebnisse** werden angezeigt:

- **Bandlücke** in eV (bulk + quantenmechanische Confinement-Korrektur)
- **Emissionswellenlänge** in nm
- **Anzahl der Atome** im sphärischen Zinkblende-Ausschnitt

## 7. Diagramme

Unten finden Sie drei Tabs mit Plots:

- **Energie**: Balkendiagramm der berechneten Elektron- und Loch-Niveaus.
- **Spektrum**: Gauß-förmiger Peak, der die größenabhängige Bandlückenverschiebung visualisiert.
- **Bandprofil**: (im Shell-Modus) Radiales Leitungs-/Valenzbandprofil mit Band-Offsets und gebundenen Niveaus.

## 8. Bericht exportieren

1. Klicken Sie auf **LaTeX-Export**.
2. Wählen Sie im Speichern-Dialog einen Zielordner und Dateinamen.
3. Die `.tex`-Datei enthält:
   - Eingabeparameter (Material, Radius)
   - Verwendete physikalische Formeln
   - Ergebnistabelle (Bandlücke, Wellenlänge, Energieniveaus)

> Hinweis: Für die PDF-Erzeugung wird eine lokale LaTeX-Installation (z. B. TeX Live oder MiKTeX) mit `pdflatex` benötigt.

## 9. Tipps

- Größere Radien erzeugen mehr Atome und können die 3D-Ansicht verlangsamen.
- Berechnete Quantum Dots werden zwischengespeichert: Material + Radius erneut wählen ist sofort sichtbar.
- Das Fenster kann während der Berechnung weiter bedient werden.
