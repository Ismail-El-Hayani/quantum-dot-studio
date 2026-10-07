# Quantum Dot Studio — Backend and Calculations

This document explains how the backend of Quantum Dot Studio works: the data flow from a user-selected material and radius to the final 3D model, plots, and LaTeX report. It is meant as a reference for extending the project, e.g. with ternary materials or core/shell structures.

## 1. Overview

The application follows a small MVVM pipeline:

```
User input (Material + Radius + options)
          |
          v
SimulationViewModel
          |
          v
QuantumDotService.BuildQuantumDot(Material, radius)
          |
          +--> QuantumSolver       (energies, band gap, wavelength)
          +--> LatticeEngine       (zinc blende atom positions)
          +--> ProbabilityCloudGenerator (electron ground-state cloud)
          |
          v
QuantumDot (aggregate data object)
          |
          +--> QuantumDotRenderer3D  (Helix-Toolkit 3D model)
          +--> PlotFactory            (OxyPlot energy/spectrum charts)
          +--> LatexReportGenerator   (report source)
```

`QuantumDot` is the central DTO. Once it is built, the 3D view, plots, stats text, legend, and export all read from the same object.

## 2. Data models

### `Material`

Stores bulk semiconductor parameters:

| Property | Unit | Meaning |
|---|---|---|
| `Name` | - | Display name, e.g. `CdSe` |
| `BandGap_eV` | eV | Bulk band gap at room temperature |
| `EffectiveMassElectron` | `m_e` | Electron effective mass |
| `EffectiveMassHole` | `m_e` | Hole effective mass |
| `DielectricConstant` | - | Relative permittivity ε_r |
| `LatticeConstant_A` | Å | Zinc blende lattice constant |
| `Cation` | - | Cation element symbol |
| `Anion` | - | Anion element symbol |

Currently every material is treated as a binary zinc blende compound. Ternary/alloy materials would extend this model, see Section 12.

Materials live in `src/QuantumDotStudio.Core/Data/materials.json` and are loaded
once on first access (`MaterialDatabase.Defaults`, thread-safe via `Lazy`). The
file is copied to the output directory at build time; adding an entry there adds
a material to the application without any code change. A built-in fallback list
(CdSe, InP, PbS) is used only if no valid JSON file is found.

### `QuantumDot`

Aggregates everything that belongs to one simulation step:

| Property | Meaning |
|---|---|
| `Material` | Selected material |
| `Radius_nm` | Dot radius in nm |
| `ConfinementEnergyElectron_eV` | Electron confinement energy |
| `ConfinementEnergyHole_eV` | Hole confinement energy |
| `TotalBandGap_eV` | Effective QD band gap |
| `EmissionWavelength_nm` | Emission wavelength from the band gap |
| `EnergyLevels` | List of electron and hole levels |
| `Atoms` | Atom positions inside the spherical dot |
| `ElectronCloud` | 1S electron probability cloud |

### `Atom`

| Property | Meaning |
|---|---|
| `Element` | Chemical symbol, used for coloring |
| `Position` | `(X,Y,Z)` in nm |
| `Radius_nm` | Visual radius in nm |

### `EnergyLevel`

| Property | Meaning |
|---|---|
| `PrincipalQuantumNumber_n` | Radial quantum number |
| `AngularMomentum_l` | Angular momentum quantum number |
| `Energy_eV` | Calculated energy |
| `Label` | Spectroscopic label, e.g. `1S`, `1P`, `2S` |

## 3. The solver (`QuantumSolver`)

All calculations use the **infinite spherical quantum well** model with effective mass approximation and the **full Brus equation** including the leading-order Coulomb term.

### 3.1 Confinement energy

For a particle of effective mass `m*` in a spherical potential well of radius `R`:

```
E_{n,l} = ℏ² α_{n,l}² / (2 m* R²)
```

where `α_{n,l}` is the n-th zero of the spherical Bessel function `j_l`.

Implemented in:

```csharp
QuantumSolver.ConfinementEnergy(double R_nm, double mStar, int n, int l)
```

Steps:
1. Convert `R_nm` to meters.
2. Compute `α_{n,l}` numerically (`BesselZero`): n-th zero of `j_l` via interlacing bracket `(α_{n,l−1}, α_{n+1,l−1})` + bisection — exact for `l = 0` (`n·π`), no asymptotic fallback. Memoized per `(n, l)`, `l ≤ 10`.
3. Compute energy in joules with `ℏ = 1.054571817e-34 J·s`.
4. Convert to eV using `1 eV = 1.602176634e-19 J`.

Sample zeros (verified against independent ODE integration):
- `l = 0` (S): `α = n·π`
- `l = 1` (P): `4.4934, 7.7253, 10.9041, 14.0662, 17.2208, ...`
- `l = 2` (D): `5.7635, 9.0950, 12.3229, ...`
- Higher `l`: computed, not approximated (`6.9879, 8.1826, 9.3558, ...`)

### 3.2 Coulomb term

The electron-hole attraction in leading order (Brus 1984):

```
E_C = −1.786 · e² / (4π ε₀ ε_r R)
```

Implemented in:

```csharp
QuantumSolver.CoulombEnergy(double R_nm, double dielectricConstant)
```

For CdSe at R = 3 nm this contributes about −0.08 eV, moving the predicted
band gap toward the experimental sizing curve (Yu et al., Chem. Mater. 15
(2003): ~2.0–2.1 eV for 6 nm diameter).

### 3.3 Effective band gap (full Brus formula)

```
E_QD = E_g,bulk + ΔE_e + ΔE_h + E_C
```

Implemented in:

```csharp
QuantumSolver.BrusBandGap(Material material, double radius_nm)
```

It calls `ConfinementEnergy(..., n=1, l=0)` for electron and hole, computes the
Coulomb term from the dielectric constant, and adds all contributions to the
bulk band gap.

### 3.4 Emission wavelength

```
λ = h·c / E_QD
```

Implemented in:

```csharp
QuantumSolver.WavelengthFromBandGap(double bandGap_eV)
```

Uses `h·c = 1.98644586e-25 J·m`. Returns λ in nm.

### 3.5 Energy level series

```csharp
QuantumSolver.CalculateEnergyLevels(double R_nm, double mStar, int maxN, int maxL)
```

Builds all `(n,l)` combinations up to `maxN` and `maxL`, labels them with spectroscopic notation (`1S`, `1P`, `2S`, ...), and sorts by energy. Each level carries a `Particle` property (`Electron` or `Hole`) set by `QuantumDotService`; the label itself stays prefix-free.

## 4. Lattice generation (`LatticeEngine`)

Builds a spherical zinc blende atom cut-out from the selected material.

```csharp
LatticeEngine.GenerateZincBlende(
    double latticeConstant_A,
    double radius_nm,
    string cation,
    string anion)
```

### 4.1 Zinc blende basis

For each integer lattice cell `(ix, iy, iz)`:

- Cell origin = `(ix, iy, iz) · a`, where `a = latticeConstant_A · 0.1` (Å → nm).
- Four cations at fractional positions:
  - `(0, 0, 0)`
  - `(0, 0.5, 0.5)`
  - `(0.5, 0, 0.5)`
  - `(0.5, 0.5, 0)`
- Four anions at fractional positions:
  - `(0.25, 0.25, 0.25)`
  - `(0.25, 0.75, 0.75)`
  - `(0.75, 0.25, 0.75)`
  - `(0.75, 0.75, 0.25)`

Each atom position is checked against the requested sphere radius. Atoms inside are kept, with default visual radii:

- Cation: `0.12 nm`
- Anion: `0.14 nm`

The number of atoms grows roughly as `r³` (e.g. ~4 000 atoms for CdSe at 3 nm, ~11 000 at 5 nm).

## 5. Electron probability cloud (`ProbabilityCloudGenerator`)

Approximates the 1S electron ground state in the infinite spherical well.

```csharp
ProbabilityCloudGenerator.GenerateElectronCloud1S(double radius_nm)
```

### 5.1 Wave function

For `l = 0`, `n = 1`:

```
α = π
ψ(r) ∝ sin(α·r/R) / r
|ψ|² ∝ (sin(π·r/R) / r)²
```

### 5.2 Grid sampling

- `GridResolution = 40` → `40³ = 64 000` grid points inside the bounding cube.
- Two passes over the grid:
  1. Find maximum `|ψ|²` for normalization.
  2. Yield points where normalized `|ψ|² >= ProbabilityThreshold = 0.05`.

This produces thousands of cloud points. They are later rendered as small transparent spheres.

## 6. Service orchestration (`QuantumDotService`)

```csharp
QuantumDotService.BuildQuantumDot(Material material, double radius_nm, int maxLevels = 6)
```

What it does:
1. Checks the cache for `(material.Name, radius_nm)`.
2. Creates a fresh `QuantumDot`.
3. Calls `QuantumSolver` for energies, band gap, and wavelength.
4. Calls `LatticeEngine` for atoms.
5. Calls `ProbabilityCloudGenerator` for the cloud.
6. Stores the result in the cache and returns it.

The cache avoids recomputation when the same material/radius combination is requested again.

## 7. Rendering (`QuantumDotRenderer3D`)

```csharp
QuantumDotRenderer3D.BuildModel(QuantumDot dot, bool showLattice, bool showCloud)
```

### 7.1 Scaling

- `UnitsPerNanometer = 0.40` — how many WPF 3D units one physical nm occupies.
- `AtomScale = 0.55` — relative atom size.
- `CloudPointSize = 0.25` — relative cloud point size.

The model is centered by subtracting the atom/electron-cloud centroid.

### 7.2 Atom rendering

For each atom:
1. Translate position by `-centroid` and multiply by `visualScale`.
2. Look up color from `AtomPalette` by element symbol.
3. Build a sphere mesh with `MeshBuilder.AddSphere`.
4. Convert to WPF `MeshGeometry3D`.
5. Add a `GeometryModel3D` with that mesh and material to the `Model3DGroup`.

This is currently one mesh + one material per atom. It is accurate but not performant for thousands of atoms.

### 7.3 Cloud rendering

All surviving cloud points are merged into a single `GeometryModel3D` mesh with a semi-transparent Dodger-Blue material (`A=120`). Each point is a small sphere whose radius scales with local probability.

### 7.4 Color palette (`AtomPalette`)

Known element colors:

| Element | Color |
|---|---|
| Cd | Gold |
| Se | Orange-red |
| In | Warm gray-brown |
| P | Yellow |
| Pb | Steel blue-gray |
| S | Neon yellow |

Unknown elements fall back to light gray.

## 8. Plots (`PlotFactory`)

### 8.1 Energy level plot

Horizontal bar chart showing the first 12 electron/hole levels. Labels are grouped by base spectroscopic label (`1S`, `1P`, ...). Electrons are DodgerBlue, holes are Crimson.

### 8.2 Spectrum plot

A Gaussian line around `EmissionWavelength_nm` with a width of `2 %` of the center. A dashed red vertical line marks the peak.

## 9. Report generation (`LatexReportGenerator`)

Produces a `.tex` document containing:
1. Summary with material, radius, band gap, and wavelength.
2. Physical model section with the confinement-energy and Brus equations.
3. Material-parameter table.
4. Quantum-dot results table.
5. Energy-level table.
6. Spectrum section with an emission-color box.
7. Optional embedded 3D screenshot (PNG) or placeholder.

The emission color is computed from wavelength with a simple visible-spectrum RGB approximation (`WavelengthToRgb`).

## 10. WPF binding

The 3D view is no longer driven by a manual event handler. Instead, `MainWindow.xaml` binds the `Content` of a `ModelVisual3D` to a `MultiBinding` over:

- `Simulation.ActiveDot`
- `Simulation.ShowLattice`
- `Simulation.ShowCloud`

The `QuantumDotToModel3DConverter` calls `QuantumDotRenderer3D.BuildModel(...)` whenever any of those values changes. This makes the 3D view automatically update when the user changes material, radius, or visibility flags.

The camera is fitted once to the bounds of a 10 nm dot on load and stays fixed afterwards, so radius changes appear as visible scaling.

## 11. Known simplifications and limitations

- **Infinite well**: No finite barrier, no surface states.
- **Effective mass**: Isotropic and material-independent inside the dot.
- **Coulomb term in leading order only**: No full excitonic series, no exchange terms.
- **Zinc blende only**: No ternary alloys, no core/shell structures.
- **Static lattice**: No strain, relaxation, or ligand effects.
- **1S electron cloud only**: No excited-state wave functions.
- **Performance**: Atoms and cloud points are rendered one-by-one, which does not scale to very large dots.

## 12. Where to extend

| Feature | Files to touch |
|---|---|
| Ternary alloys (e.g. AgInS₂) | `Material.cs`, `materials.json`, `LatticeEngine.cs`, `AtomPalette.cs` |
| Core/shell (e.g. AIS/ZnS) | `QuantumDot.cs`, `QuantumDotService.cs`, `LatticeEngine.cs`, `QuantumDotRenderer3D.cs`, `MainWindow.xaml` |
| Better performance | `ProbabilityCloudGenerator.cs`, `QuantumDotService.cs`, `QuantumDotRenderer3D.cs` |
| More accurate physics | `QuantumSolver.cs` (finite barriers, strain, exciton corrections) |
| More plots | `PlotFactory.cs`, `MainWindow.xaml` |

## 13. Continuous integration

`.github/workflows/dotnet.yml` builds the solution and runs all xUnit tests on
every push to `main` and on every pull request (windows-latest, .NET 10). Test
results are uploaded as TRX artifacts.
