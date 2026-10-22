# Quantum Dot Studio — Backend and Calculations

This document explains how the backend of Quantum Dot Studio works: the data flow from a user-selected material and radius to the final 3D model, plots, and LaTeX report. It is meant as a reference for extending the project, e.g. with more transduction modes or a full environment model.

## 1. Overview

The application follows a small MVVM pipeline:

```
User input (Material + Radius + Shell options)
          |
          v
SimulationViewModel.RecalculateAsync   (background thread, staleness-guarded)
          |
          v
QuantumDotService.BuildQuantumDot / BuildCoreShellQuantumDot
          |
          +--> QuantumSolver           (confinement, Brus gap, wavelength, levels)
          +--> FiniteWellSolver       (core/shell bound S-states, l = 0)
          +--> BandAlignment          (electron/hole barriers from affinities)
          +--> StrainModel            (lattice mismatch, critical thickness)
          +--> OpticalTransitions     (selection-rule absorption spectrum)
          +--> LatticeEngine          (zinc blende atom positions)
          +--> ProbabilityCloudGenerator (1S cloud; 1P on demand)
          |
          v
QuantumDot (aggregate data object)
          |
          +--> QuantumDotRenderer3D   (merged-mesh Helix model, frozen, background-built)
          +--> PlotFactory            (OxyPlot: levels, spectrum, band profile, sensor responses)
          +--> LatexReportGenerator   (report source)
```

`QuantumDot` is the central DTO. Once it is built, the 3D view, plots, stats text,
legend, and export all read from the same object. All heavy work (physics, mesh
build) runs on `Task.Run`; the UI thread only assigns the finished, frozen model.

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
| `ElectronAffinity_eV` | eV | EA vs. vacuum — basis of the band alignment |
| `VarshniAlpha_eV_per_K` / `VarshniBeta_K` | eV/K, K | Band-gap temperature coefficients (§3.8) |
| `Cation` | - | Cation element symbol |
| `Anion` | - | Anion element symbol |

Every material is treated as a binary zinc blende compound. Ternary/alloy
materials would extend this model, see Section 12.

Materials live in `src/QuantumDotStudio.Core/Data/materials.json` and are loaded
once on first access (`MaterialDatabase.Defaults`, thread-safe via `Lazy`). The
file is copied to the output directory at build time; adding an entry there adds
a material to the application without any code change. A built-in fallback list
(CdSe, InP, PbS) is used only if no valid JSON file is found. The same
JSON-plus-compiled-fallback pattern is used for the ligand, analyte, sensor
template, and feasibility-weight databases (`Data/*.json`).

### `QuantumDot`

Aggregates everything that belongs to one simulation step:

| Property | Meaning |
|---|---|
| `Material` | Selected material |
| `Radius_nm` | Dot radius in nm (core + shell for heterostructures) |
| `ConfinementEnergyElectron_eV` | Electron confinement energy |
| `ConfinementEnergyHole_eV` | Hole confinement energy |
| `CoulombEnergy_eV` | Leading-order Coulomb term (negative) |
| `TotalBandGap_eV` | Effective QD band gap |
| `EmissionWavelength_nm` | Emission wavelength from the band gap |
| `EnergyLevels` | List of electron and hole levels |
| `OpticalTransitions` | Selection-rule transitions, ascending in energy (§3.6) |
| `Atoms` | Atom positions inside the spherical dot |
| `ElectronCloud` | 1S electron probability cloud |
| `ElectronCloud1P` | 1P cloud (n=1, l=1, m=0), lazily generated |
| `Environment` | Environment the dot was computed with (§3.8) |

### `CoreShellQuantumDot : QuantumDot`

| Property | Meaning |
|---|---|
| `ShellMaterial` | Shell semiconductor |
| `CoreRadius_nm` | Radius of the core (emission volume) |
| `ShellThickness_nm` | Shell thickness in nm (0..5) |
| `ElectronBarrier_eV` / `HoleBarrier_eV` | Band offsets from `BandAlignment` |
| `LatticeMismatch_f` | `f = (a_shell − a_core) / a_core` |
| `CriticalThickness_nm` | Matthews–Blakeslee estimate |
| `IsStrainRelaxed` | True if the shell exceeds the critical thickness |

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
| `Particle` | `Electron` or `Hole` (labels stay prefix-free) |

## 3. The solver (`QuantumSolver`)

Bare dots use the **infinite spherical quantum well** model with effective mass
approximation and the **full Brus equation** including the leading-order Coulomb
term. Core/shell dots replace the radial confinement with the **finite well**
(§3.7).

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

### 3.6 Optical transitions

```csharp
OpticalTransitions.Compute(double bulkBandGap_eV, double effectiveBandGap_eV,
                           IEnumerable<EnergyLevel> levels, int maxTransitions = 5)
```

Pure functions in `Core/Physics/OpticalTransitions.cs` (namespace
`QuantumDotStudio.Core.Physics`). Strong-confinement model for the absorption
spectrum:

- **Selection rule Δn = Δl = 0.** Electron and hole envelope functions live in
  the same spherical well and are orthogonal — only states with identical
  `(n,l)` overlap. Transition energy: `E(n,l) = E_g + E_e(n,l) + E_h(n,l)`.
- **Ground state anchored.** The 1S→1S transition is set to the effective band
  gap of the model (`QuantumDot.TotalBandGap_eV`, which carries the Brus Coulomb
  term), so the emission line is identical with the Brus result.
- **Relative oscillator strength** `f ∝ (2l+1)` — degeneracy over the magnetic
  quantum number: `1S : 1P : 1D = 1 : 3 : 5` (overlap = 1 for identical
  quantum numbers).
- **Core/shell dots:** `FiniteWellSolver` provides only S-states, so shell-mode
  spectra contain pure S-transitions (`1S→1S`, `2S→2S`, `3S→3S`) computed from
  the finite-well energies.
- Wavelengths use the same `h·c/e` constant chain as
  `WavelengthFromBandGap`, keeping transition and emission wavelengths
  consistent to the last digit.

The result is stored on the model (`QuantumDot.OpticalTransitions`) by
`QuantumDotService` and consumed by the spectrum plot (§8.2). Tests
(`OpticalTransitionsTests`) pin the anchor identity, the degeneracy ratios, the
energy ordering `1S < 1P < 1D < 2S < 2P`, and the S-only shell case.

### 3.7 Core/shell stack (finite well, band alignment, strain)

- **`BandAlignment`** — electron/hole barriers at the core/shell interface from
  the electron affinities: `V0_e = EA_core − EA_shell` (positive = confinement
  in the core); the sum rule `V0_e + V0_h = E_g,shell − E_g,core` is unit-tested.
- **`FiniteWellSolver`** — bound S-states (`l = 0`) of the finite spherical
  well, matching condition `k·cot(kR) = −κ` with `k = √(2m*E)/ħ`,
  `κ = √(2m*(V0−E))/ħ`. The n-th state lies in exactly one interval
  `kR ∈ ((n−1)π, nπ)` where the condition is strictly monotonic — one bisection
  per state. Binding threshold (3D): a state exists only for
  `V0 > ħ²/(2m*R²)`. A too-shallow barrier or a type-II alignment yields no
  bound states; the effective gap then falls back to the bulk core value.
- **`StrainModel`** — lattice mismatch `f = (a_shell − a_core)/a_core` and the
  Matthews–Blakeslee critical thickness `t_c ≈ b/(2|f|)` with Burgers vector
  `b ≈ 0.3 nm`. A shell thicker than `t_c` is flagged relaxed
  (`IsStrainRelaxed`); the band-profile plot marks `t_krit`.

### 3.8 Environment model (Roadmap 2.3)

```csharp
EnvironmentModel.VarshniShift_eV / BandGapAtTemperature /
                 EffectiveDielectric / PhWindowScore
QuantumDotService.BuildQuantumDot(..., EnvironmentSettings? environment = null)
QuantumDotService.BuildCoreShellQuantumDot(..., EnvironmentSettings? environment = null)
```

- **Temperature (Varshni):** `E_g(T) = E_g,ref − α·[T²/(T+β) − T_ref²/(T_ref+β)]`,
  referenced to the 300 K value in `materials.json` — at reference temperature
  the shift is exactly zero, so the standard environment reproduces the legacy
  numbers bit-identically. CdSe: α = 4.4e-4 eV/K, β = 181 K (Varshni, Physica
  34, 149 (1967)); measured anchor: CdSe/ZnS QD PL red-shifts ~0.022 eV over
  300→373 K (Superlattices & Microstructures 46 (2009), QD α ≈ 2.07e-4).
- **Solvent:** the Coulomb term is screened by the phenomenological blend
  `ε_eff = (2·ε_dot + ε_sol)/3` (both carriers sit inside the dot → 2/3 weight).
  Solvents live in `Data/solvents.json` (CRC Handbook values, 20–25 °C);
  a solvent with `ε_r <= 0` is the "no correction" sentinel.
- **pH:** each aqueous ligand carries a stability window
  (`StabilityPhMin`/`StabilityPhMax`); `FeasibilityFactors.StabilityScore`
  multiplies by `PhWindowScore` (1 inside, 0.25 outside, 1 when not applicable —
  organic ligands have no window). The pH reaches the engine via
  `SensorDesign.EnvironmentPh`.
- The environment is part of the service cache key and stored on the dot
  (`QuantumDot.Environment`); emission, spectrum anchor, sensor and feasibility
  views all read the same object, so an environment change flows through the
  entire pipeline on the next recalculation.

## 4. Lattice generation (`LatticeEngine`)

Builds a spherical zinc blende atom cut-out from the selected material.

```csharp
LatticeEngine.GenerateZincBlende(
    double latticeConstant_A,
    double radius_nm,
    string cation,
    string anion)
```

For core/shell dots:

```csharp
LatticeEngine.GenerateZincBlendeCoreShell(
    double coreLatticeConstant_A, double coreRadius_nm, double shellThickness_nm,
    string coreCation, string coreAnion,
    string shellCation, string shellAnion)
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

Each atom position is checked against the requested sphere radius (core radius
inside the core region, core + shell thickness in the shell). Atoms inside are
kept, with default visual radii:

- Cation: `0.12 nm`
- Anion: `0.14 nm`

The number of atoms grows roughly as `r³` (e.g. ~4 000 atoms for CdSe at 3 nm,
~151 000 at 10 nm — the case the renderer benchmark uses).

## 5. Electron probability cloud (`ProbabilityCloudGenerator`)

Approximates the 1S electron ground state in the infinite spherical well.

```csharp
ProbabilityCloudGenerator.GenerateElectronCloud1S(double radius_nm)
ProbabilityCloudGenerator.GenerateElectronCloud1P(double radius_nm)
```

### 5.1 Wave function

For `l = 0`, `n = 1`:

```
α = π
ψ(r) ∝ sin(α·r/R) / r
|ψ|² ∝ (sin(π·r/R) / r)²
```

The 1P cloud (`n = 1, l = 1, m = 0`) is the radial `j₁` function times
`cos²θ` — a dumbbell along z with a node at the origin and at the equatorial
plane, peak at `r ≈ 0.65·R`.

### 5.2 Grid sampling

- `GridResolution = 40` → `40³ = 64 000` grid points inside the bounding cube.
- Two passes over the grid:
  1. Find maximum `|ψ|²` for normalization.
  2. Yield points where normalized `|ψ|² >= ProbabilityThreshold = 0.05`.

This produces thousands of cloud points, rendered as merged semi-transparent
meshes (§7.3). The 1P cloud is generated lazily
(`QuantumDotService.EnsureElectronCloud1P`) because the second 64k-point pass
is noticeable — it is only computed when the 1P checkbox is first enabled.

## 6. Service orchestration (`QuantumDotService`)

```csharp
QuantumDotService.BuildQuantumDot(Material material, double radius_nm, int maxLevels = 6)
QuantumDotService.BuildCoreShellQuantumDot(Material core, Material shell,
    double coreRadius_nm, double shellThickness_nm, int maxStates = 3)
```

What the bare-dot build does:
1. Checks the cache for `(material.Name, radius_nm)`.
2. Creates a fresh `QuantumDot`.
3. Calls `QuantumSolver` for energies, Brus gap, wavelength, and the level series.
4. Computes the optical transitions (§3.6) from the levels.
5. Calls `LatticeEngine` for atoms and `ProbabilityCloudGenerator` for the cloud.
6. Stores the result in the cache and returns it.

The core/shell build adds band alignment, finite-well S-states, strain analysis,
and the core/shell lattice; the radius slider doubles as the core radius in
shell mode. The cache avoids recomputation for repeated material/radius pairs;
it is locked on read AND write so concurrent background recalculations cannot
race. `EnsureElectronCloud1P(dot)` adds the 1P cloud on demand;
`ClearCache()` empties the cache when material data change at runtime.

## 7. Rendering (`QuantumDotRenderer3D`)

```csharp
QuantumDotRenderer3D.BuildModel(QuantumDot dot,
    bool showLattice = true, bool showCloud = true, bool showCloud1P = false)
```

### 7.1 Scaling

- `UnitsPerNanometer = 0.40` — how many WPF 3D units one physical nm occupies.
- `AtomScale = 0.55` — relative atom size.
- `CloudPointSize = 0.25` — relative cloud point size.

The model is centered by subtracting the atom/electron-cloud centroid.

### 7.2 Atom rendering (merged meshes)

All atoms of one element are merged into a single mesh: one `MeshBuilder` per
element, producing exactly one `GeometryModel3D` per element instead of one per
atom. A 10 nm CdSe dot (~151 000 atoms) renders as 2 children where the
per-atom approach created ~151 000.

Tiered level of detail (construction cost, benchmark-verified):

- `> 10 000` atoms: sphere tessellation reduced to `(6, 5)`.
- `> 40 000` atoms: tessellation `(4, 3)` and boxes instead of spheres
  (individually sub-pixel objects at that size).

Texture coordinates are not generated (solid-color brushes), collections are
presized, and the finished `Model3DGroup` is `Freeze()`d so the whole scene can
be built on a background thread and handed to the UI frozen.

Benchmark (`QuantumDotStudio.Bench`, median of 5 runs, 10 nm CdSe):
legacy per-atom construction ~13 s on the UI thread (extrapolated) vs.
~1.1 s merged and in the background.

### 7.3 Cloud rendering

All surviving cloud points are merged into a single `GeometryModel3D` mesh per
cloud (`BuildCloudMesh(color)`): the 1S cloud is a semi-transparent Dodger-Blue
overlay, the 1P cloud a dark-red overlay. Each point is a small sphere whose
radius scales with local probability.

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
| Zn | Cool light gray |

Unknown elements fall back to light gray.

## 8. Plots (`PlotFactory`)

### 8.1 Energy level plot

Horizontal bar chart showing the first 12 electron/hole levels. Labels are grouped by base spectroscopic label (`1S`, `1P`, ...). Electrons are DodgerBlue, holes are Crimson.

### 8.2 Spectrum plot

Absorption (blue) and emission (green) in one model:

- The **absorption series** is the sum of Gaussian-broadened optical transitions
  (§3.6), each peak weighted by its relative oscillator strength.
- The **emission series** is the 1S photoluminescence line at
  `EmissionWavelength_nm` (radiative recombination after relaxation into the
  ground state).
- Broadening is phenomenological: 2 % of each transition's wavelength — a
  stand-in for size dispersion, not a lineshape model.
- A dashed red vertical line marks the emission peak.

Without transitions (e.g. a type-II-like alignment without bound states) the
plot falls back to the pure emission line.

### 8.3 Band profile plot

Radial conduction/valence band edges of a core/shell dot with barrier jumps at
the interface, the bound S-states as dashed lines, and a red `t_krit` marker
when the shell exceeds the critical thickness. Bare dots get a flat profile.

### 8.4 Sensor response plots

One response curve per transduction mode: FRET `E(r)` with bound/unbound
markers, Stern–Volmer log calibration with LOD and current concentration,
Nernst `ψ(pH)`, PET quench fraction vs. `E₀` with the CB edge.

## 9. Report generation (`LatexReportGenerator`)

Produces a `.tex` document containing:
1. Summary with material, radius, band gap, and wavelength.
2. Physical model section with the confinement-energy and Brus equations;
   the assumption list states the finite well for core/shell dots and the
   environment (temperature, medium, pH) when it deviates from the reference.
3. Material-parameter table.
4. Quantum-dot results table — with an environment block (temperature, solvent,
   pH, effective permittivity) when the environment is set.
5. Energy-level table.
6. Spectrum section with an emission-color box.
7. Optional core/shell chapter (heterostructures only).
8. Optional feasibility chapter (factor table, Monte Carlo band) when a
   feasibility result is passed in.
9. Optional embedded 3D screenshot (PNG) or placeholder.

The emission color is computed from wavelength with a simple visible-spectrum RGB approximation (`WavelengthToRgb`). All numbers are formatted with a fixed German culture (decimal comma), so the same calculation yields the same document on any machine. Generated reports are compile-verified with pdflatex.

## 10. WPF binding

The 3D view binds the `Content` of a `ModelVisual3D` directly to
`Simulation.Pending3DModel` — a frozen `Model3DGroup` built entirely on a
background thread by `SimulationViewModel.RecalculateAsync()` (physics, 1P
cloud, and mesh build each on `Task.Run`; a staleness guard discards superseded
results, so fast slider drags only ever apply the newest model). There is no
value converter on the UI thread. The camera is fitted once to the bounds of a
10 nm dot on load and stays fixed afterwards, so radius changes appear as
visible scaling.

## 11. Known simplifications and limitations

- **Bare dots: infinite well** (finite well for core/shell); no surface states.
- **Effective mass**: isotropic and material-independent inside the dot.
- **Coulomb term in leading order only**: no full excitonic series, no exchange terms.
- **Excited-state binding neglected**: optical transitions above 1S carry no
  Coulomb term; only the ground-state transition is anchored to the Brus gap
  (§3.6).
- **Spectrum broadening phenomenological**: fixed 2 % Gaussian per transition —
  a stand-in for size dispersion, not a lineshape model.
- **Finite well: S-states only** (`l = 0`): shell-mode spectra contain no P/D
  transitions.
- **1P cloud: single orientation** (`m = 0`, dumbbell along z).
- **Zinc blende binaries only**: no ternary alloys; graded interfaces enter as
  a strain-relief factor, not as a composition profile.
- **Feasibility engine**: heuristic factor scores anchored to literature
  designs (not ab-initio); Monte Carlo priors are simple ± ranges.
- **Solvent screening phenomenological**: `ε_eff = (2ε_dot + ε_sol)/3` — a
  blending ansatz, not a full image-charge treatment (§3.8).
- **Varshni parameters partly generic**: CdSe/CdS/InP carry cited sources;
  PbS, ZnSe, CdTe and ZnS use generic II-VI estimates until verified
  sources are added to `materials.json`.

## 12. Where to extend

| Feature | Files to touch |
|---|---|
| P/D states of the finite well | `FiniteWellSolver.cs`, `OpticalTransitions.cs` |
| Ternary alloys (e.g. AgInS₂) | `Material.cs`, `materials.json`, `LatticeEngine.cs`, `AtomPalette.cs` |
| Exciton corrections beyond leading order | `QuantumSolver.cs`, `OpticalTransitions.cs` |
| Verify Varshni constants per material (PbS, ZnSe, CdTe, ZnS — currently generic estimates) | `materials.json` + `EnvironmentModelTests` |
| Response-time estimate (diffusion + kinetics) | `SensorPhysics.cs`, `SensorViewModel.cs` |
| Real lineshape model | `PlotFactory.cs` |

## 13. Continuous integration

`.github/workflows/dotnet.yml` builds the solution and runs all xUnit tests on
every push to `main` and on every pull request (windows-latest, .NET 10). Test
results are uploaded as TRX artifacts.