# Engine Reference Documentation

This directory contains curated, version-pinned documentation snapshots for Unity 6 URP,
the engine used in **Gene Forge** (tactical creature-collection RPG, isometric 3D grid combat,
DNA-based creature engineering). These files exist because **LLM knowledge has a cutoff date**
and game engines update frequently.

## Why This Exists

Claude's training data has a knowledge cutoff (currently May 2025). Game engines like Unity
ship updates that introduce breaking API changes, new features, and deprecated patterns. Without
these reference files, agents will suggest outdated code.

## Gene Forge Tech Stack

- **Engine**: Unity 6 URP (Universal Render Pipeline)
- **UI**: UI Toolkit (not UGUI)
- **Input**: Input System package (`com.unity.inputsystem`)
- **Camera**: Cinemachine
- **Tiles/Grid**: ProBuilder for tile geometry
- **Stats**: 5-stat model — HP / ATK / DEF / SPD / ACC
- **Types**: 8 creature types (type matchup chart in GDD)
- **Perspective**: Isometric 3D grid combat

## Structure

```
unity/
├── VERSION.md              # Pinned version, verification date, knowledge gap window
├── breaking-changes.md     # API changes between versions, organized by risk level
├── deprecated-apis.md      # "Don't use X → Use Y" lookup tables
├── current-best-practices.md  # New practices not in model training data
└── modules/                # Per-subsystem quick references (~150 lines max each)
    ├── animation.md
    ├── audio.md
    ├── input.md
    ├── navigation.md
    ├── networking.md
    ├── physics.md
    ├── rendering.md
    └── ui.md
```

## How Agents Use These Files

Engine-specialist agents are instructed to:

1. Read `VERSION.md` to confirm the current engine version
2. Check `deprecated-apis.md` before suggesting any engine API
3. Consult `breaking-changes.md` for version-specific concerns
4. Read relevant `modules/*.md` for subsystem-specific work

## Gene Forge Agent Notes

- **UI work**: Always use UI Toolkit (UXML/USS/C#). Never scaffold UGUI Canvas workflows.
- **Input work**: Always use the Input System package. Never use `Input.GetKey` / legacy Input.
- **Rendering**: Gene Forge uses URP. Never suggest HDRP or Built-in pipeline shaders.
- **Grid/Isometric**: Tile positions use isometric projection. World positions require iso math.
- **Creature stats**: HP / ATK / DEF / SPD / ACC — reference these names exactly in code.
- **Camera**: Use Cinemachine virtual cameras. Do not manually script camera transforms.

## Maintenance

### When to Update

- After upgrading the engine version
- When the LLM model is updated (new knowledge cutoff)
- After running `/refresh-docs` (if available)
- When you discover an API the model gets wrong

### How to Update

1. Update `VERSION.md` with the new engine version and date
2. Add new entries to `breaking-changes.md` for the version transition
3. Move newly deprecated APIs into `deprecated-apis.md`
4. Update `current-best-practices.md` with new patterns
5. Update relevant `modules/*.md` with API changes
6. Set "Last verified" dates on all modified files

### Quality Rules

- Every file must have a "Last verified: YYYY-MM-DD" date
- Keep module files under 150 lines (context budget)
- Include code examples showing correct/incorrect patterns
- Link to official documentation URLs for verification
- Only document things that differ from the model's training data
