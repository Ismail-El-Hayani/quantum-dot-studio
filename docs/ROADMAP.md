# Quantum Dot Studio — Roadmap: From QD Simulator to Sensor Design Platform

> Vision: evolve the current QD visualization tool into a **sensor design assistant**.
> A user picks a target application (bio-imaging, chemical detection, ratiometric
> FRET sensor …), configures core/shell/functionalization — and the software
> estimates the **probability of successful fabrication** of that sensor, factor by
> factor, before any synthesis is attempted.

Language note: this is a strategic development document (English). Course-critical
docs (Anforderungsanalyse, Tutorial) stay German.

---

## 0. Current state (v1.0 — course deliverable)

| Capability | Status |
|---|---|
| Single homogeneous QD (CdSe, InP, PbS + JSON-extensible) | ✅ |
| Infinite spherical well + full Brus equation (incl. Coulomb term) | ✅ |
| Zinc blende lattice cut-out, 3D rendering (Helix) | ✅ |
| 1S electron cloud, energy levels, Gaussian emission peak | ✅ |
| LaTeX report export, CI (build + 39 tests) | ✅ |

Known tech debt (folded into Phase 1 below): one-mesh-per-atom rendering (perf),
UI-thread recalculation, 1P excited state missing (FR-005), asymptotic Bessel
zeros for n > 3 / l > 2.

---

## 1. Phase 1 — Core/Shell QDs (prerequisite for everything sensor-related)

Almost every real QD sensor is a **core/shell construct**: the shell passivates
surface traps (raises quantum yield), protects against the environment, and
sets the bio-compatibility. This phase makes the object model physically real.

### 1.1 Model extensions

| Change | Files |
|---|---|
| `Material` gains `ValenceBandOffset_eV`, `ConductionBandOffset_eV` (band alignment) | `Material.cs`, `materials.json` |
| `CoreShellQuantumDot : QuantumDot` with `ShellMaterial`, `ShellThickness_nm` | new file in Core |
| LatticeEngine: two-radius cutout (core sphere + shell shell) with correct elements per region | `LatticeEngine.cs` |
| Renderer: shell atoms in second color/transparency | `QuantumDotRenderer3D.cs` |

### 1.2 Finite spherical well solver (replaces infinite well when a shell exists)

Confinement with finite barrier `V0` (from band offsets): roots of

```
k · cot(k·R) = −κ ,   k = sqrt(2 m* E)/ħ ,  κ = sqrt(2 m* (V0 − E))/ħ
```

solved by bisection/Brent per `(n, l)` — replaces the asymptotic `BesselZero`
fallback and gives physically honest level lowering in the shell. The model
selector (infinite vs. finite) becomes a UI dropdown.

| Change | Files |
|---|---|
| `FiniteWellSolver` with numeric root finding + unit tests against the infinite-well limit | new in Solver |
| `BesselZero` → numeric spherical-Bessel zeros (removes hardcoded table) | `QuantumSolver.cs` |

### 1.3 Strain physics (first "feasibility" quantity)

Lattice mismatch `f = (a_shell − a_core) / a_core` and critical thickness
`t_c ≈ b / (2·|f|)` (Matthews–Blakeslee estimate, b ≈ 0.3 nm dislocation Burgers vector):

| Core/Shell | f | t_c (nm) | Verdict |
|---|---|---|---|
| InP/CdS | −0.63 % | ~24 | relaxed, excellent |
| InP/ZnSe | −3.42 % | ~4.4 | workable (thin shell) |
| CdSe/CdS | −3.60 % | ~4.2 | workable (industry standard) |
| CdSe/ZnSe | −6.31 % | ~2.4 | critical |
| CdSe/ZnS | −10.60 % | ~1.4 | needs graded interface |
| AgInS₂/ZnS | −7.22 % | ~2.1 | critical, cadmium-free alternative |

This table ships as `Data/core_shell_strain.json` and drives the first
feasibility factor (Section 3).

### 1.4 UI & tech debt in this phase

- Core material, shell material, shell-thickness slider; radial band-edge profile plot (OxyPlot)
- **Renderer mesh-merge**: one mesh per element instead of per atom (fixes 10 nm freeze)
- **Async recalculation**: `Task.Run` + debounce abstraction, UI stays responsive (NFR-001)
- 1P electron cloud (closes FR-005 gap)

**Exit criteria:** CdSe/CdS 3 nm + 0.6 nm shell renders, finite-well energies
within 10 % of literature, recalc at 10 nm < 200 ms, all tests green in CI.

---

## 2. Phase 2 — Sensor physics modes

The app learns what a sensor *is*: a QD whose optical response changes
predictably in the presence of an analyte.

### 2.1 Functionalization model

`Data/ligands.json` + `Data/analytes.json`:

```json
{
  "LigandId": "MPA",
  "DisplayName": "Mercaptopropionic acid",
  "AnchorAtom": "S",
  "Length_nm": 0.6,
  "Charge_e": -1,
  "Targets": ["Cd", "Zn", "Pb"],
  "Solubility": "aqueous"
}
```

Analytes carry the interaction model: `FRET` (Förster radius R₀),
`Quenching` (Stern–Volmer constant K_SV), `Charge` (Nernst shift).

### 2.2 Sensor modes (each a tab in the UI)

| Mode | Physics | Readout | Typical application |
|---|---|---|---|
| **FRET ratiometric** | E = 1/(1+(r/R₀)⁶), r = ligand length + receptor | donor/acceptor ratio change | immunoassays, glucose |
| **Quenching** | I₀/I = 1 + K_SV·[A] | intensity drop | heavy metals (Pb²⁺, Hg²⁺), O₂ |
| **Charge/pH** | QD emission shift vs. surface potential (Nernst) | spectral shift | pH, ion sensing |
| **Photoinduced electron transfer (PET)** | donor/acceptor energetics vs. band offsets | on/off switch | neurotransmitters |

FRET reference table (R₀ = 5 nm, computed):

| Donor–acceptor distance r | Transfer efficiency E |
|---|---|
| 3 nm | 95.5 % |
| 4 nm | 79.2 % |
| 5 nm | 50.0 % |
| 6 nm | 25.1 % |
| 8 nm | 5.6 % |
| 10 nm | 1.5 % |

Design rule the app will enforce: baseline distance and analyte-bound distance
must differ by ≳ 1 nm to produce a resolvable ratiometric signal.

### 2.3 Environment model

Solvent dielectric (screens Coulomb term), temperature (Varshni band-gap shift),
pH window (ligand stability). Feeds both the physics and the feasibility scores.

**Exit criteria:** a CdSe/ZnS–MPA–fluorescein FRET pair can be configured and
its ratiometric response plotted; quenching calibration curve matches
Stern–Volmer form in unit tests.

---

## 3. Phase 3 — The Probability Engine (core of the vision)

> "Show me the probability that this sensor can actually be built."

### 3.1 Factor model

Each design is scored on independent factors `fᵢ ∈ [0, 1]`. The overall
probability is the **weighted geometric mean**

```
P(success) = Π fᵢ^wᵢ ,  Σ wᵢ = 1
```

Geometric (not arithmetic) so that one disqualifying factor (e.g. impossible
strain) cannot be compensated by good ones — mirroring real synthesis.

| # | Factor | Score basis | Weight (initial) |
|---|---|---|---|
| 1 | **Emission window match** | Gaussian score of λ_em against application window (bio: NIR-I 650–900 nm; ratiometric: donor/acceptor separation ≥ 40 nm; chemical: visible shift detectability) | 0.25 |
| 2 | **Strain / relaxation** | from f and shell thickness vs. t_c; graded-interface option raises score | 0.20 |
| 3 | **Quantum yield proxy** | QY ≈ Γ_rad/(Γ_rad + Γ_nr); Γ_nr grows with strain and surface-trap density, falls with shell passivation | 0.20 |
| 4 | **Signal transduction** | FRET contrast (ΔE between bound/unbound ≥ 20 %) or K_SV above detection threshold | 0.15 |
| 5 | **Colloidal & chemical stability** | ligand charge/solubility vs. medium, pH window, ZnS protection layer present | 0.10 |
| 6 | **Bioconjugation affinity** | receptor–analyte K_D vs. target concentration range (needs ≥ 10× span) | 0.10 |

Weights live in `Data/feasibility_weights.json` (user-tunable per application
template). Every factor is a pure function of design parameters → fully unit-testable.

### 3.2 Uncertainty, not just point estimate

Material parameters carry experimental error bars (±0.05 eV band gap, ±10 %
masses, ±0.05 Å lattice constant). The engine runs **Monte Carlo (N ≈ 2000)** over
these priors and reports:

- median P with 5–95 % confidence band,
- **factor variance decomposition** — which input uncertainty dominates the risk.

This is what turns the tool from a calculator into a *design critiquer*: it tells
the user *which* parameter to fix first.

### 3.3 UI: Feasibility panel

- Overall verdict: P ≥ 0.7 **green** / 0.4–0.7 **amber** / < 0.4 **red**
- Horizontal bars per factor with weight and one-line fix suggestion
- Monte Carlo band around the overall number
- LaTeX report gains a "Feasibility" chapter with the factor table

### 3.4 Worked example (shipped as a unit test / demo scenario)

Bio-imaging sensor, two candidate designs (numbers from Phase 1/2 tables):

| Factor | InP/ZnSe (R = 3 nm, 2 ML shell) | CdSe/ZnS (R = 3 nm, graded) |
|---|---|---|
| Emission window | λ ≈ 653 nm → edge of NIR-I, score ~0.6 | λ ≈ 590 nm → visible only, score ~0.25 |
| Strain | f = −3.4 %, t within t_c → score ~0.75 | f = −10.6 % but graded interface → score ~0.55 |
| QY proxy | thin ZnSe shell, moderate → ~0.7 | thick graded ZnS, industry-proven → ~0.85 |
| Transduction | FRET Δr design-dependent | same |
| Stability | ZnSe in water weaker → ~0.5 | ZnS robust → ~0.9 |
| Affinity | same receptor model | same |
| **P (illustrative)** | **≈ 0.6 — amber** | **≈ 0.5 — amber** |

The honest output: *neither design is ideal for NIR bio-imaging; a larger InP
core (λ → 700 nm) or an alloy core (CuInS₂/AgInS₂) would raise factor 1.*
Exactly this kind of guided reasoning is the product goal.

**Exit criteria:** all six factors implemented as pure functions with unit
tests (monotonicity + boundary values); Monte Carlo band stable at N = 2000
(< 1 s); feasibility chapter compiles in the LaTeX export.

---

## 4. Phase 4 — Sensor templates & literature validation

- **Template library** (`Data/sensor_templates.json`): pre-parameterized designs
  with references — glucose-GOx FRET sensor, Pb²⁺ quenching sensor, pH sensor,
  immunosensor (IgG/anti-IgG). Each template pins weights and target windows.
- **Validation set:** reproduce published emission/QY of known constructs
  (CdSe/ZnS commercial QDs, InP/ZnS quantum-dot bio-labels) within ±20 % —
  becomes the NFR-002-style accuracy criterion for the probability engine.
- **Selectivity matrix:** response of the chosen sensor to off-target analytes
  (interference scoring, factor 4b).
- **Response-time estimate** (optional): ligand diffusion + reaction kinetics order-of-magnitude.

**Exit criteria:** ≥ 3 templates whose predicted P correctly ranks the
literature-validated designs above known-failure designs.

---

## 5. Milestone summary

| Version | Theme | Key deliverable |
|---|---|---|
| v1.1 | Core/Shell + finite well | physically real core/shell objects, strain table |
| v1.2 | Sensor modes | FRET/quenching/charge response simulation |
| v1.3 | **Probability engine** | P(success) with factor breakdown + Monte Carlo |
| v2.0 | Templates + validation | ranked, literature-validated sensor designs |

Dependency chain: **1 → 2 → 3 → 4** (each phase's data model feeds the next).
Phases 1–2 are conventional engineering; Phase 3 is the scientific contribution
(this is where a thesis-grade novelty would live); Phase 4 makes it credible.

---

## 6. Standing engineering rules (all phases)

1. Every physics factor is a pure, unit-tested function — no factor without a test.
2. Every new dataset is JSON (`Data/*.json`) — the AC-004 pattern, no code changes for content.
3. No magic constants: each constant carries a comment with source (e.g. "Brus 1984", "Yu et al. 2003", "Matthews–Blakeslee 1974").
4. CI must stay green; every phase adds tests, the report gains a chapter.
5. Push to GitHub only with explicit approval (course repo rule).