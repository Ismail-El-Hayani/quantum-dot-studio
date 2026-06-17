# Quantum Dot Studio — Idee und Zielstellung

## Zielstellung

Entwicklung eines interaktiven 3D-Visualisierungstools in C# (WPF + Helix Toolkit) zur Simulation und Darstellung von Halbleiter-Quantum Dots (Nanokristalle). Das Tool berechnet größenabhängige Bandlücken und Eigenzustände in sphärischen Potentialtöpfen und stellt das Ergebnis in Echtzeit als dreidimensionale Atomstruktur, Elektronen-Wahrscheinlichkeitswolke sowie Emissionsspektrum dar.

Die Software richtet sich an Studierende und Forschende im Bereich Nanotechnologie und Festkörperphysik.

## Motivation

Quantum Dots sind zero-dimensional nanoskalige Halbleiterstrukturen, deren optische und elektronische Eigenschaften stark von der geometrischen Größe abhängen. Die manuelle Berechnung und Visualisierung dieser Zusammenhänge ist abstrakt und wenig intuitiv. Ein interaktives Tool, bei dem der Benutzer den Radius und das Material in Echtzeit variieren kann und sofort die Auswirkungen auf die Energieniveaus und das Emissionsfarbe sieht, verbessert das Verständnis deutlich.

## Kernidee

1. 3D-Modellierung: Aufbau einer Kristallstruktur (Zinkblende, z. B. CdSe, InP, PbS) und Ausschneiden eines sphärischen Nanokristalls.
2. Quantenmechanische Berechnung: Lösung des Teilchen-im-Kasten-Modells für eine sphärische Geometrie (vereinfachter Potentialtopf).
3. Echtzeit-Visualisierung: Darstellung der Atompositionen, der Elektronen- und Loch-Wahrscheinlichkeitsdichten sowie des Größenabhängigen Emissionsspektrums.
4. Dokumentationsexport: Automatische Generierung eines LaTeX-Technischen Berichts mit verwendeten Formeln, Parametern und Ergebnissen.

## Bezug zum Studium

Das Projekt verbindet direkt meinen Hintergrund in Nanotechnologie (TU Bergakademie Freiberg) mit softwaretechnischen Methoden aus der Vorlesung Softwareentwicklung. Besondere Relevanz hat die Umsetzung physikalischer Gleichungen (Brus-Gleichung, Schrödinger-Gleichung in Kugelkoordinaten) in sauber strukturierte Softwaremodule.
