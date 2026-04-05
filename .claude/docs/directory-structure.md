# Directory Structure

```text
/
├── CLAUDE.md                        # Master configuration
├── .claude/                         # Agent definitions, skills, hooks, rules, docs
│   └── docs/
│       ├── coding-standards.md      # Code and design doc standards
│       ├── context-management.md    # Context budget and session state strategy
│       ├── coordination-rules.md    # Agent delegation and conflict resolution
│       ├── directory-structure.md   # This file — canonical project layout
│       └── technical-preferences.md # Engine, naming, performance, ADRs
│
├── Assets/                          # Unity project source (replaces src/ + assets/ in CCGS)
│   ├── Scripts/                     # Game source code
│   │   ├── Core/                    # Foundation systems (GameState, Config, Save/Load, Grid)
│   │   ├── Gameplay/                # Combat, turns, damage, capture, DNA, type chart
│   │   ├── Creatures/               # CreatureInstance, database, body parts, leveling
│   │   ├── AI/                      # Decision system, threat/aggro, encounter AI
│   │   ├── UI/                      # UI Toolkit documents, panels, combat HUD
│   │   ├── World/                   # Campaign map, ecosystem, stations, weather
│   │   └── Infrastructure/          # Settings, audio manager, VFX controller
│   ├── Resources/
│   │   └── Data/                    # ScriptableObject instances (loaded via ConfigLoader)
│   │       ├── Creatures/           # CreatureConfig ScriptableObjects
│   │       ├── Moves/               # MoveConfig ScriptableObjects
│   │       ├── BodyParts/           # BodyPartConfig ScriptableObjects
│   │       ├── Encounters/          # EncounterConfig ScriptableObjects
│   │       ├── AIPersonalities/     # AIPersonalityConfig ScriptableObjects
│   │       ├── StatusEffects/       # StatusEffectConfig ScriptableObjects
│   │       └── GameSettings.asset
│   ├── Scenes/                      # Unity scenes (Boot, MainMenu, Combat, Map, etc.)
│   ├── Prefabs/                     # Reusable prefabs (creatures, grid tiles, UI)
│   ├── Art/                         # Visual assets
│   │   ├── Models/                  # 3D creature pieces, tile meshes
│   │   ├── Materials/               # Tile materials, creature type colors
│   │   ├── Textures/                # Texture maps
│   │   └── Animations/              # Post-MVP: rigged creature animations
│   ├── Audio/                       # Sound effects and music
│   ├── VFX/                         # Particle systems and visual effects
│   ├── Shaders/                     # Custom shaders and shader graphs
│   ├── UI/                          # UI Toolkit assets (UXML, USS stylesheets)
│   └── Settings/                    # URP settings, quality profiles
│
├── design/                          # Game design documentation
│   ├── gdd/                         # 53 system GDDs + game-concept + systems-index + architecture-reference
│   ├── narrative/                   # Story, lore, dialogue, creature flavor text
│   ├── levels/                      # Combat grid layouts, zone maps, encounter areas
│   └── balance/                     # Tuning spreadsheets, stat curves, economy data
│
├── docs/                            # Technical documentation
│   ├── COLLABORATIVE-DESIGN-PRINCIPLE.md  # Question→Options→Decision→Draft→Approval
│   ├── WORKFLOW-GUIDE.md            # Session workflow patterns and best practices
│   ├── engine-reference/            # Curated engine API snapshots (version-pinned)
│   │   ├── README.md                # How to use and maintain engine references
│   │   └── unity/
│   │       ├── VERSION.md           # Pinned Unity 6 version and knowledge gap warning
│   │       ├── breaking-changes.md  # Unity 2022→6 breaking API changes
│   │       ├── current-best-practices.md  # Modern Unity 6 patterns
│   │       ├── deprecated-apis.md   # Don't-use-X → Use-Y lookup table
│   │       ├── PLUGINS.md           # Required and optional packages
│   │       ├── modules/             # Subsystem references
│   │       │   ├── animation.md
│   │       │   ├── audio.md
│   │       │   ├── input.md
│   │       │   ├── navigation.md
│   │       │   ├── networking.md
│   │       │   ├── physics.md
│   │       │   ├── rendering.md
│   │       │   └── ui.md
│   │       └── plugins/             # Package-specific references
│   │           ├── addressables.md
│   │           ├── cinemachine.md
│   │           └── dots-entities.md
│   └── examples/                    # Workflow session examples
│       ├── README.md
│       ├── session-design-crafting-system.md
│       ├── session-implement-combat-damage.md
│       ├── session-scope-crisis-decision.md
│       └── reverse-document-workflow-example.md
│
├── tests/                           # Test suites (Unity Test Framework / NUnit)
│   ├── unit/                        # Pure logic (damage calc, type chart, XP formulas)
│   ├── integration/                 # System interaction (combat flow, capture sequence)
│   ├── performance/                 # Benchmarks (A* pathfinding, mass damage calc)
│   └── playtest/                    # Playtest session logs and feedback
│
├── tools/                           # Build scripts, asset pipeline utilities
│
├── prototypes/                      # Throwaway prototypes (isolated from Assets/)
│
├── production/                      # Production management
│   ├── sprints/                     # Sprint plan documents
│   ├── milestones/                  # Milestone definitions and criteria
│   └── releases/                    # Release notes and checklists
│
├── Packages/                        # Unity package manifest (managed by Unity)
└── ProjectSettings/                 # Unity project settings (managed by Unity)
```

## Unity-Specific Notes

- **CCGS `src/` maps to `Assets/Scripts/`** — Unity requires all code under `Assets/`
- **CCGS `assets/` maps to `Assets/Art/`, `Assets/Audio/`, etc.** — Unity manages assets in `Assets/`
- **`design/`, `docs/`, `tests/`, `tools/`, `prototypes/`, `production/`** live at the project root, outside Unity's `Assets/` folder
- **ScriptableObjects** go in `Assets/Resources/Data/` for MVP (ConfigLoader uses `Resources.Load`), migrate to Addressables post-MVP
