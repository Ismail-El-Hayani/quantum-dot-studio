# Quantum Dot Studio — src

## .NET version

The project targets **.NET 10**.

## Solution structure

| Project | Purpose |
|---------|---------|
| `QuantumDotStudio.Core` | Domain models, sensor + feasibility physics, JSON databases (materials, ligands, analytes, templates, feasibility weights) |
| `QuantumDotStudio.Solver` | Quantum mechanics (Brus formula with Coulomb term, numeric Bessel zeros, finite spherical well), band alignment, strain, optical transitions, feasibility engine, service orchestration |
| `QuantumDotStudio.Renderer` | 3D visualization (Helix Toolkit, merged meshes, LOD) |
| `QuantumDotStudio.Reports` | LaTeX report generation |
| `QuantumDotStudio.WPF` | MVVM application (simulation, sensor, feasibility views) |
| `QuantumDotStudio.Tests` | xUnit tests for Solver, Core, Renderer, Reports and the WPF pipeline |
| `QuantumDotStudio.Bench` | Renderer performance benchmark (median of 5 runs) |

## Build

```bash
cd src
dotnet build QuantumDotStudio.slnx
```

## Test

```bash
cd src
dotnet test QuantumDotStudio.slnx
```

## Run

```bash
cd src
dotnet run --project QuantumDotStudio.WPF
```

## Adding materials

The material database lives in `src/QuantumDotStudio.Core/Data/materials.json`
and is copied to the output directory. New materials (e.g. ZnS, CdTe) can be
added there without any code change:

```json
{
  "Name": "ZnS",
  "BandGap_eV": 3.68,
  "EffectiveMassElectron": 0.25,
  "EffectiveMassHole": 0.59,
  "DielectricConstant": 8.9,
  "LatticeConstant_A": 5.41,
  "Cation": "Zn",
  "Anion": "S"
}
```

If the file is missing at runtime, a built-in fallback
(CdSe, InP, PbS) in `MaterialDatabase` is used. The same pattern applies to the
ligand, analyte, sensor-template and feasibility-weight databases in `Data/`.