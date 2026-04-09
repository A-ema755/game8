---
name: full-audit
description: "The most thorough review in CCGS. 11 agents across 4 phases audit all implemented systems, code, design alignment, and test coverage. Produces a unified report with severity-ranked findings and prioritized action items. Auto-syncs systems-index.md with discovered implementations."
argument-hint: "[scope: all|combat|creatures|core|ui] [output-path]"
user-invocable: true
allowed-tools: Read, Glob, Grep, Bash, Agent, Task, AskUserQuestion, Write, Edit
---

# Full Project Audit

The nuclear option. 11 specialized agents review every implemented system from
their unique angle. Agents are grouped into phased batches to control cost and
prevent duplicate work. Each agent has a **scoped mandate** — they review only
what falls in their domain and ignore the rest.

This skill is **read-only** for code — it does not modify any source files.
The one exception: it **auto-syncs `design/gdd/systems-index.md`** during
discovery to ensure newly implemented systems are tracked. If the user wants the
audit report saved, they will ask and confirm the filepath per the collaboration
protocol.

---

## 0. Discovery — What Exists?

Before launching any agents, build the audit manifest:

1. **Glob `Assets/Scripts/`** recursively to list all implemented `.cs` files.
2. **Glob `Assets/Tests/`** recursively to list all test files.
3. **Read `design/gdd/systems-index.md`** (if it exists) to map which systems
   have GDDs and which are marked as implemented.
4. **Grep for `// TODO`, `// FIXME`, `// HACK`** across `Assets/Scripts/`.
5. **Read `.claude/docs/technical-preferences.md`** for ADRs and forbidden patterns.
6. **Read `.claude/docs/coding-standards.md`** for standards checklist.

Build a **manifest** listing:
- Every script file grouped by namespace/folder
- Every test file and what it covers
- Every GDD and whether matching code exists
- TODO/FIXME/HACK counts per folder

This manifest is passed to every agent so they know what exists without
re-scanning the codebase themselves.

### Systems-Index Sync

After building the manifest, cross-reference discovered implementations against
`design/gdd/systems-index.md`. Any system that has implemented code but is still
marked as `Draft` or missing from the index gets flagged.

**Procedure:**

1. For each script folder in `Assets/Scripts/` (Core, Combat, Creatures, AI, UI,
   Gameplay, World, Infrastructure), identify which systems-index entries they
   correspond to. Use class names, namespaces, and file contents to map code to
   systems.

2. Compare against the systems-index Status column:
   - If a system has substantial implementation (not just stubs or empty classes)
     but systems-index says `Draft` → update to `**Implemented**`
   - If implemented code exists that maps to NO system in the index → flag as
     **Untracked System** and ask user whether to add a new row

3. **Ask before writing:** Present the proposed changes as a table:
   ```
   Systems-Index Sync:
   | System | Current Status | Proposed Status | Evidence |
   |--------|---------------|-----------------|----------|
   | Party System | Draft | **Implemented** | Assets/Scripts/Creatures/PartyManager.cs (250 lines, full CRUD) |
   | [New] CombatController | NOT IN INDEX | Add to Core Layer? | Assets/Scripts/Combat/CombatController.cs |
   ```
   Wait for user approval before editing `design/gdd/systems-index.md`.

4. Apply approved changes via `Edit` tool. Preserve existing table format exactly.

**What counts as "implemented":**
- Class has meaningful logic (not just empty methods or `// TODO` stubs)
- Has at least one public API that does real work
- Passes basic compilation (no `#if false` blocks wrapping everything)

**What does NOT count:**
- Interface-only files with no implementation
- Test files (tests prove something works, they aren't the system)
- Config/data classes with no behavior (ScriptableObjects are data, not systems)

---

## 1. Parse Arguments

- **scope** (optional, default `all`):
  - `all` — audit everything in `Assets/Scripts/` and related GDDs
  - `combat` — only `Assets/Scripts/Combat/` and combat-related GDDs
  - `creatures` — only `Assets/Scripts/Creatures/` and creature GDDs
  - `core` — only `Assets/Scripts/Core/` and core system GDDs
  - `ui` — only `Assets/Scripts/UI/` and UI-related GDDs
- **output-path** (optional): where to save the report. If omitted, display
  in conversation only.

---

## 2. Agent Mandates

Each agent reviews ONLY their assigned domain. Findings outside their mandate
are ignored — another agent owns that domain.

### Phase 1 — Code & Architecture (launch in parallel)

**lead-programmer**
- Scope: ALL implemented `.cs` files in the manifest
- Mandate: Code quality only. No design opinions, no Unity API opinions.
- Checklist:
  - [ ] Public APIs have XML doc comments (`/// <summary>`)
  - [ ] No method exceeds 40 lines (excluding data declarations)
  - [ ] Cyclomatic complexity under 10 per method
  - [ ] Dependencies injected, not static singletons (except ADR-003 exceptions)
  - [ ] No public fields — `[SerializeField]` private or properties
  - [ ] SOLID compliance (SRP, OCP, LSP, ISP, DIP)
  - [ ] No circular dependencies between namespaces
  - [ ] Events used for cross-system communication (ADR-004)
  - [ ] Naming conventions match technical-preferences.md
- Output: Per-file findings with line references. Severity: CRITICAL / MAJOR / MINOR.

**unity-specialist**
- Scope: ALL implemented `.cs` files in the manifest
- Mandate: Unity-specific patterns only. No code style, no design alignment.
- Checklist:
  - [ ] No `FindObjectOfType` in hot paths
  - [ ] No `Update()` polling where events suffice
  - [ ] Proper use of `[SerializeField]` vs public fields
  - [ ] ScriptableObjects used for config data (ADR-001)
  - [ ] No hardcoded gameplay values
  - [ ] Correct Unity 6 API usage (check `docs/engine-reference/unity/`)
  - [ ] No deprecated Unity APIs (check `deprecated-apis.md`)
  - [ ] Assembly definitions properly configured
  - [ ] MonoBehaviour vs plain C# class decisions appropriate (ADR-002)
  - [ ] Resource cleanup (no leaks, proper OnDestroy)
- Output: Per-file findings with line references. Severity: CRITICAL / MAJOR / MINOR.

**technical-director**
- Scope: Project architecture as a whole — folder structure, assembly refs,
  dependency graph, ADR compliance.
- Mandate: Architecture only. No line-level code quality, no Unity API details.
- Checklist:
  - [ ] Namespace structure matches ADR-008 (`GeneForge.Core`, `.Combat`, etc.)
  - [ ] Dependency direction correct (no upward references)
  - [ ] All ADRs in technical-preferences.md are honored in code
  - [ ] No forbidden patterns present
  - [ ] Structs vs classes per ADR-009
  - [ ] Assembly definitions enforce namespace boundaries
  - [ ] Cross-system coupling is event-based, not direct references
  - [ ] Tech debt inventory (TODO/FIXME/HACK from manifest)
- Output: Architecture findings + dependency graph issues. Severity: CRITICAL / MAJOR / MINOR.

---

### Phase 2 — Design & Vision (launch in parallel, after Phase 1 completes)

Phase 2 receives Phase 1 findings so design reviewers know what's actually
implemented (not just what the GDD says should exist).

**game-designer**
- Scope: All GDDs in `design/gdd/` that have corresponding implementations.
- Mandate: GDD-to-code alignment only. No code quality, no architecture.
- Checklist:
  - [ ] Every implemented mechanic matches its GDD specification
  - [ ] Formulas in code match GDD formulas exactly
  - [ ] Edge cases documented in GDD are handled in code
  - [ ] Tuning knobs from GDD are exposed as configurable values
  - [ ] No implemented mechanics lack a GDD
  - [ ] No GDD acceptance criteria are unmet by current code
  - [ ] Gameplay values in ScriptableObjects match GDD-specified ranges
- Output: Per-system alignment report. Status: ALIGNED / DRIFT / MISSING GDD.

**systems-designer**
- Scope: System interfaces and cross-system dependencies.
- Mandate: System coherence only. No individual code quality, no pillar review.
- Checklist:
  - [ ] System boundaries match `design/gdd/systems-index.md`
  - [ ] Cross-system interfaces are clean (no hidden coupling)
  - [ ] Data flows between systems match GDD dependency maps
  - [ ] Shared types (enums, configs) live in correct namespaces
  - [ ] No system makes assumptions about another system's internals
  - [ ] Formula inputs/outputs chain correctly across systems
- Output: System interface map + coherence findings. Severity: CRITICAL / MAJOR / MINOR.

**creative-director**
- Scope: Overall project vision and pillar adherence.
- Mandate: Vision only. No code, no formulas, no architecture.
- Checklist:
  - [ ] Each implemented system serves at least one game pillar
  - [ ] No implemented system contradicts a pillar
  - [ ] Feature priorities align with pillar hierarchy
  - [ ] Player fantasy described in GDDs is achievable with current implementation
  - [ ] Tone and identity are consistent across all implemented systems
- Output: Pillar alignment report. Status per system: ON-PILLAR / DRIFT / ANTI-PILLAR.

---

### Phase 3 — Domain Specialists (launch in parallel, after Phase 2 completes)

Phase 3 receives Phase 1 + Phase 2 findings so specialists focus on
implementation correctness within their domain, not issues already flagged.

**gameplay-programmer**
- Scope: `Assets/Scripts/Combat/`, `Assets/Scripts/Gameplay/`, and related tests.
- Mandate: Gameplay implementation correctness only. No general code style
  (lead-programmer covers that), no Unity patterns (unity-specialist covers that).
- Checklist:
  - [ ] Combat flow logic is correct (turn order, action resolution, win/loss)
  - [ ] Damage formulas compute correctly per GDD
  - [ ] Type chart interactions are complete and correct
  - [ ] Capture rate calculations match GDD
  - [ ] State transitions are clean (no invalid states reachable)
  - [ ] Frame-rate independence (delta time where needed)
  - [ ] No allocations in hot paths (combat update loops)
- Output: Per-system correctness findings. Severity: CRITICAL / MAJOR / MINOR.

**ai-programmer**
- Scope: `Assets/Scripts/AI/` and AI-related config ScriptableObjects.
- Mandate: AI system correctness only.
- Checklist:
  - [ ] Decision scoring produces sensible results across edge cases
  - [ ] AI personality configs properly influence behavior
  - [ ] No infinite loops or deadlocks in AI evaluation
  - [ ] AI respects game rules (doesn't cheat, uses valid moves)
  - [ ] Performance of AI evaluation is bounded (no unbounded searches)
  - [ ] AI behavior is deterministic given same inputs (testable)
- Output: AI system findings. Severity: CRITICAL / MAJOR / MINOR.

**ui-programmer**
- Scope: `Assets/Scripts/UI/`, UI Toolkit assets (`Assets/UI/`), and input handling.
- Mandate: UI implementation correctness only.
- Checklist:
  - [ ] UI state reflects game state accurately (no stale displays)
  - [ ] Input handling follows Unity Input System patterns
  - [ ] No hardcoded strings in UI code (localization-ready)
  - [ ] UI updates don't allocate in hot paths
  - [ ] Screen flow transitions are complete (no dead-end screens)
  - [ ] UI is decoupled from game logic (no game state owned by UI)
- Output: UI system findings. Severity: CRITICAL / MAJOR / MINOR.

**performance-analyst**
- Scope: ALL implemented `.cs` files, with focus on hot paths (combat loops,
  AI evaluation, grid pathfinding, UI updates).
- Mandate: Performance only. No code style, no design alignment, no architecture.
- Checklist:
  - [ ] No heap allocations in Update/FixedUpdate loops (boxing, LINQ, string concat)
  - [ ] No per-frame GetComponent / Find calls
  - [ ] Collections pre-sized where possible (List capacity, Dictionary capacity)
  - [ ] No unnecessary string operations in hot paths
  - [ ] Object pooling used where repeated instantiation occurs
  - [ ] Coroutine usage doesn't create GC pressure
  - [ ] ScriptableObject lookups cached, not repeated per frame
  - [ ] A* / pathfinding bounded (max iterations, early exit)
  - [ ] AI scoring evaluation bounded (no unbounded loops over all entities)
  - [ ] Memory layout concerns (struct vs class choices per ADR-009)
- Output: Per-file performance findings. Severity: CRITICAL / MAJOR / MINOR.
  Include estimated impact: HIGH (frame spikes) / MEDIUM (steady overhead) / LOW (micro-optimization).

---

### Phase 4 — Quality Gate (launch after Phase 3 completes)

**qa-tester**
- Scope: ALL test files + cross-reference against implemented code.
- Mandate: Test coverage and quality only. No code fixes, no design opinions.
- Receives: Full findings from Phases 1-3 to identify what's already flagged.
- Checklist:
  - [ ] Every public method in gameplay systems has at least one test
  - [ ] Edge cases from GDDs have corresponding test cases
  - [ ] Tests actually assert meaningful behavior (not just "doesn't throw")
  - [ ] No test files are empty or placeholder-only
  - [ ] Critical formulas (damage, capture, type chart) have boundary tests
  - [ ] Test naming follows conventions
  - [ ] Integration test coverage for system interactions
  - [ ] Missing test coverage mapped by priority
- Output: Coverage map + gap analysis. Priority: MUST-TEST / SHOULD-TEST / NICE-TO-TEST.

---

## 3. Synthesis

After all 4 phases complete, the orchestrator (you) merges findings:

### Cross-Cutting Issues
Findings raised by 2+ agents independently are **load-bearing problems**.
Deduplicate and escalate these to the top of the report.

### Severity Rollup
Count findings by severity across all agents:
- **CRITICAL**: Blocks correctness or causes runtime failures
- **MAJOR**: Violates standards, architecture, or design — will cause problems
- **MINOR**: Style, optimization opportunities, nice-to-haves

### System Health Matrix
For each implemented system, produce a row:

| System | Code Quality | Unity Patterns | Architecture | GDD Alignment | Perf | Tests | Overall |
|--------|-------------|---------------|-------------|---------------|------|-------|---------|

Grades: A (clean) / B (minor issues) / C (major issues) / F (critical issues)

---

## 4. Output Format

```markdown
# Full Project Audit Report

**Date**: [date]
**Scope**: [all|combat|creatures|core|ui]
**Agents**: 11 (4 phases)
**Systems Audited**: [count]
**Files Reviewed**: [count]

---

## Executive Summary
[3-5 sentences: overall health, biggest risks, top priority actions]

## Cross-Cutting Issues (raised by 2+ agents)
1. **[Issue]** — Flagged by: [agent1], [agent2]
   - Impact: [description]
   - Recommended action: [action]

## Severity Rollup
| Severity | Count | Breakdown |
|----------|-------|-----------|
| CRITICAL | N | [summary] |
| MAJOR    | N | [summary] |
| MINOR    | N | [summary] |

## System Health Matrix
| System | Code | Unity | Arch | Design | Perf | Tests | Overall |
|--------|------|-------|------|--------|------|-------|---------|

---

## Phase 1 — Code & Architecture

### lead-programmer
[findings]

### unity-specialist
[findings]

### technical-director
[findings]

---

## Phase 2 — Design & Vision

### game-designer
[findings]

### systems-designer
[findings]

### creative-director
[findings]

---

## Phase 3 — Domain Specialists

### gameplay-programmer
[findings]

### ai-programmer
[findings]

### ui-programmer
[findings]

### performance-analyst
[findings]

---

## Phase 4 — Quality Gate

### qa-tester
[findings]

---

## Prioritized Action Items
| # | Action | Severity | Owner (suggested agent) | Systems Affected |
|---|--------|----------|------------------------|-----------------|

## Recommended Next Steps
- [Contextual skill suggestions based on findings]

## Verdict: [HEALTHY / CONCERNS / AT RISK / CRITICAL]
```

---

## 5. Execution Notes

- **Each agent prompt must include**: the full manifest from Phase 0, their
  specific mandate and checklist, the scope filter, and any prior-phase findings
  relevant to their work.
- **Agents must NOT duplicate each other's work.** If an agent notices something
  outside their mandate, they note it in one line as "Out of scope — likely
  covered by [other-agent]" and move on.
- **Phase ordering matters.** Phase 2 needs Phase 1 results (to know what code
  exists and its quality). Phase 3 needs Phase 2 results (to know design
  alignment status). Phase 4 needs everything (to assess what's tested vs what
  should be).
- **Parallel within phases.** All agents in a phase launch simultaneously.
- **Scope filter applies everywhere.** If scope is `combat`, every agent only
  looks at combat-related files, GDDs, and tests.

## 6. Collaborative Protocol

This skill follows the collaborative design principle:

1. **Discovery first**: Build manifest, present to user
2. **Confirm scope**: "Auditing [N] files across [N] systems. Proceed?"
3. **Phase transitions**: Brief user between phases with interim findings
4. **Present full report**: Show complete audit results
5. **User decides**: Findings are advisory — user prioritizes actions
6. **Save on request**: "May I write this report to [output-path]?"

**Never** make changes based on audit findings without explicit user instruction.
The audit observes and reports — it does not fix.
