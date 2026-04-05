# Game Pillars: Gene Forge

## Document Status
- **Version**: 1.0
- **Last Updated**: 2026-04-04
- **Approved By**: creative-director
- **Status**: Draft

---

## What Are Game Pillars?

Pillars are the 3-5 non-negotiable principles that define this game's identity.
Every design, art, audio, narrative, and technical decision must serve at least
one pillar. If a feature doesn't serve a pillar, it doesn't belong in the game.

**Why pillars matter**: In a typical development cycle, the team makes thousands
of small creative decisions. Pillars ensure all those decisions push in the same
direction, creating a coherent player experience rather than a collection of
disconnected features.

### What Makes a Good Pillar

A good pillar is:
- **Falsifiable**: "Fun gameplay" is not a pillar. "Combat rewards patience over
  aggression" is — it makes a testable claim about design choices.
- **Constraining**: If a pillar never forces you to say no to something, it's
  too vague. Good pillars eliminate options.
- **Cross-departmental**: A pillar that only constrains game design but says
  nothing about art, audio, or narrative is incomplete. Real pillars shape
  every discipline.
- **Memorable**: The team should be able to recite the pillars from memory.
  If they can't, the pillars are too numerous or too complex.

### Real AAA Examples

| Game | Pillars | Why They Work |
| ---- | ---- | ---- |
| **God of War (2018)** | Visceral combat; Father-son emotional journey; Continuous camera (no cuts); Norse mythology reimagined | "Continuous camera" is radical — it cut a standard cinematic tool. "Father-son journey" constrains narrative, level design, AND combat (Atreus as companion). |
| **Hades** | Fast fluid combat; Story depth through repetition; Every run teaches something new | "Story through repetition" justified the roguelike structure narratively — death IS the story. "Every run teaches" constrains level and encounter design. |
| **The Last of Us** | Story is essential, not optional; AI partners build relationships; Stealth is always an option | "AI partners build relationships" drove massive investment in companion AI — not just pathfinding, but emotional presence. |
| **Celeste** | Tough but fair; Accessibility without compromise; Story and mechanics are the same thing | "Story and mechanics are the same thing" — climbing IS the struggle, the dash IS the anxiety. Pillar prevented mechanics from being "just gameplay." |
| **Hollow Knight** | Atmosphere over explanation; Earned mastery; World tells its own story | "Atmosphere over explanation" — no tutorials, no hand-holding, the world teaches through environmental design. |
| **Dead Cells** | Every weapon is viable; Combat is a dance; Permanent death creates meaning | "Every weapon is viable" is extremely constraining — it demands constant balance work across hundreds of items. |

---

## Core Fantasy

> You are a field researcher and genetic pioneer. Every creature you capture is raw
> material for discovery. Every battle is a test of tactics and a source of new DNA.
> Your team isn't found — it's *built*. No two players will field the same roster,
> because no two players will make the same engineering decisions.

---

## Target MDA Aesthetics

| Rank | Aesthetic | How Gene Forge Delivers It | Primary Pillar |
| ---- | ---- | ---- | ---- |
| 1 | **Expression** | DNA splicing, body part equipping, visible mutations, color/pattern extraction — every creature is a personal creation that looks, sounds, and fights unlike anyone else's | Genetic Architect |
| 2 | **Discovery** | Progressive 4-tier Pokedex, hidden DNA recipes unlocked through experimentation, DNA Vaults with forbidden mods, fossil genome reconstruction, ecosystem secrets revealed through conservation | Discovery Through Play |
| 3 | **Challenge** | Height-variable isometric grid with terrain-type synergy, flanking arcs, weather zones, combo moves requiring adjacent positioning, AI trainers who adapt to your team | Tactical Grid Mastery |
| 4 | **Fantasy** | You are a field researcher and genetic pioneer — every creature captured is raw material, every battle is a source of new DNA, your team is built by your hands | Genetic Architect, Living World |
| 5 | **Narrative** | Rival trainers remember and counter your strategies, Institute rank tracks your ethics, black market DNA has story consequences, NPCs react to your scars and rank | Living World |
| 6 | **Sensation** | Visible mutations on 3D models, sound mutations layering DNA-driven audio fingerprints, procedural animation blending from equipped parts, battle scars as permanent marks | Genetic Architect (tangibility layer) |
| 7 | **Submission** | Auto-battle toggle for easy encounters, 1x/2x/4x speed modes, expedition system for idle-time play, session-friendly save points between every encounter | No primary pillar — QoL concern |
| 8 | **Fellowship** | Async PvP (post-MVP), DNA trading (post-MVP), leaderboards — deliberately lowest priority; single-player depth is the foundation | No primary pillar — post-MVP |

**Key observation**: The top three aesthetics (Expression, Discovery, Challenge) map 1:1 to the first three pillars. Fantasy is shared between Pillars 1 and 4. Narrative is served primarily by Pillar 4. Sensation functions as the tangibility layer that makes Pillar 1 real — without visible/audible mutation, Expression is just menu numbers.

---

## The Pillars

### Pillar 1: Genetic Architect

**One-Sentence Definition**: Every creature on the player's team should feel personally authored through deliberate DNA modification — not found, leveled, or evolved into its current state.

**Target Aesthetics Served**: Expression, Fantasy

**Design Test**: "We're debating whether to add a level-based evolution system alongside DNA splicing. This pillar says no — creature growth comes from the player's engineering decisions, not from reaching an XP threshold. Evolution would split the identity investment between player choice and automatic progression."

#### What This Means for Each Department

| Department | This Pillar Says... | Example |
| ---- | ---- | ---- |
| **Game Design** | DNA modification must offer meaningful, non-dominated choices at every tier. Stacking everything on one creature must be impossible — the player must choose an identity for each build. | Body Part System creates build diversity through slot conflicts: equipping Crystal Shell (Rock defense) conflicts with Feathered Wings (flight mobility), forcing the player to commit to a creature archetype rather than piling on every advantage. |
| **Art** | Every DNA modification must produce a visible change on the creature's 3D model. If the player can't *see* their engineering, the authorship fantasy is broken. | Color & Pattern System shifts base color with type affinity changes, extracts donor species patterns during splicing, and adds glow/emission from Aura parts or high instability. The creature's appearance is a readable history of every engineering decision. |
| **Audio** | DNA modifications must alter the creature's sonic identity. A heavily modified creature should *sound* different from its base species. | Sound Mutation System layers type-specific audio processing (fire = rumble/crackle, crystal = chime/resonance, poison = hissing/bubbling) and adds distortion at high instability. Each creature develops a unique audio fingerprint of everything the player has done to it. |
| **Narrative** | The world must acknowledge and react to the player's engineering choices. Modified creatures are not invisible to NPCs. | Battle Scar System marks creatures with permanent visible scars from near-death experiences tied to the damage source type. NPCs and rivals comment on heavily scarred creatures as veterans — the narrative recognizes the history encoded in the player's creations. |
| **Engineering** | The creature data model must support arbitrary modification stacking, serialization, and UI inspection without combinatorial explosion. | Creature Instance tracks runtime state: equipped body parts, active DNA mods, instability level, scar history, affinity bonds, and full DNA lineage tree — all serializable through Save/Load System and inspectable in the Pokedex entry. |

#### Serving This Pillar

- **DNA Alteration System** — stat boosts, perk grafts, cross-species splicing, and instability management give the player four distinct tiers of modification depth, each adding engineering complexity
- **Body Part System** — modular body slots with part blueprints that unlock moves, change type affinity, and interact with the grid (Wings ignore height cost, Roots create difficult terrain, Glands drop hazard tiles)
- **Move Customization System** — type infusion turns Flamethrower into "Frostflame" (Fire/Ice dual-type); equipping Venom Glands adds poison chance to basic attacks; move mastery through 50+ uses upgrades moves into stronger versions
- **Color & Pattern System** — type-driven color shifts, donor species pattern extraction, instability glow, and terrain-matching camouflage bonuses make modification choices visible and functional
- **Station Upgrade System** — 5-tier research station progression gates deeper DNA tools: basic stat boosts (L1) through Forbidden Mod installation and fossil resurrection (L5), pacing the player's engineering capability
- **DNA Vault System** — ancient ruins containing Forbidden Mods with extreme instability costs and unique part blueprints, rewarding exploration with engineering options unavailable anywhere else

#### Violating This Pillar

- Adding a level-based evolution system that transforms creatures automatically at XP thresholds — makes progression feel *found* rather than *authored*
- Creating "best in slot" body parts with no meaningful trade-offs — collapses build diversity into a single optimal loadout and removes the authorship decision
- Making DNA materials so abundant that every modification is trivially available — removes the resource cost that gives engineering decisions weight and consequence
- Hiding DNA modification behind a late-game unlock gate — if the core fantasy isn't accessible in the first hour, the player identifies as a trainer, not an architect
- Adding a full-reset button that removes all DNA modifications and returns a creature to base form — undermines commitment to engineering decisions and devalues the lineage tree
- Allowing players to import fully-modded creatures from external sources — bypasses the authorship journey that *is* the game

---

### Pillar 2: Tactical Grid Mastery

**One-Sentence Definition**: Battles are won through positioning, terrain exploitation, and creature synergy on the isometric grid — never through raw stat advantages alone.

**Target Aesthetics Served**: Challenge

**Design Test**: "We're debating whether to add an auto-resolve button for trivially easy encounters. This pillar says no — if an encounter doesn't reward positioning, it shouldn't exist. Cut it or redesign it. The grid must always matter."

#### What This Means for Each Department

| Department | This Pillar Says... | Example |
| ---- | ---- | ---- |
| **Game Design** | Every combat encounter must present meaningful positioning decisions. Flat stat-checks are forbidden as the primary resolution mechanic. | Grid/Tile System's height-variable tiles (levels 0-4) create elevation-based damage bonuses and line-of-sight blocking. Damage & Health System stacks height bonus, terrain synergy multiplier, and flanking arc bonus on top of type effectiveness — spatial awareness multiplies damage output. |
| **Art** | The grid must be readable at a glance — tile types, height differences, synergy zones, and creature positions must be instantly parseable without hovering. | Terrain System uses distinct material colors per tile type (lava glow for fire, blue sheen for water, green canopy for grass) and stepped height shading so the player can identify synergy opportunities from the isometric camera without zooming. |
| **Audio** | Combat audio must communicate tactical state changes — gaining or losing positional advantage should be audible. | Audio System's dynamic combat music shifts intensity when the player achieves flanking position, claims high ground, or places a creature on a synergy tile. The soundtrack reinforces that positioning matters. |
| **Narrative** | Rival trainers must demonstrate tactical intelligence, not just bring bigger numbers. The world should treat grid mastery as a respected skill. | Rival Trainer System makes rivals adapt both team composition AND positioning strategy to counter the player's most-used tactics. A rival who got flanked last time opens the next fight with a defensive terrain-control formation. |
| **Engineering** | Grid pathfinding and combat resolution must be performant enough for real-time tactical feedback — move ranges, attack zones, and synergy highlights computed within frame budget. | Grid/Tile System's A* pathfinding with height cost must calculate and display reachable tiles, attack ranges, and synergy highlights within the 16.67ms frame budget. AI Decision System's scoring-based evaluation must resolve without visible delay. |

#### Serving This Pillar

- **Grid/Tile System** — isometric 3D grid with height 0-4, flanking arcs, line-of-sight calculations, and A* pathfinding creates a rich tactical space where every tile matters
- **Terrain System** — tile types interact with creature types (fire creature on lava = power boost, water on water = healing), making positioning a type-awareness exercise layered on top of spatial reasoning
- **Terrain Alteration System** — select creatures can scorch, freeze, flood, grow, electrify, or corrode tiles mid-battle, reshaping the tactical landscape as the fight evolves
- **Combo Move System** — adjacent creatures trigger fusion attacks costing both creatures' turns, requiring precise positioning to access the strongest moves in the game
- **Threat/Aggro System** — wild creatures target based on threat scoring (damage dealt, proximity, low HP), making formation and creature placement a survival tool, not just an offensive choice
- **Damage & Health System** — height bonus, terrain synergy multipliers, and flanking bonuses stack multiplicatively with type effectiveness, rewarding players who read the grid

#### Violating This Pillar

- Adding auto-targeting that selects the "optimal" target for the player — removes the core positioning decision from every attack
- Creating creatures so fast they reach any tile in one turn — makes starting position and movement planning irrelevant
- Making type effectiveness so dominant that grid position doesn't matter — flattens tactical depth into a rock-paper-scissors pre-battle check
- Adding a free "undo move" button that lets players reposition without commitment — removes the consequence that makes positioning decisions meaningful
- Designing flat, open combat maps with no terrain variation — eliminates height advantage, cover opportunities, and synergy tile mechanics
- Allowing all ranged attacks to hit any tile on the grid regardless of line-of-sight — makes positioning for cover and elevation pointless

---

### Pillar 3: Discovery Through Play

**One-Sentence Definition**: Information about creatures, DNA interactions, and the world is earned through observation, experimentation, and risk — never presented upfront in menus or tutorials.

**Target Aesthetics Served**: Discovery

**Design Test**: "We're debating whether to display a creature's full stat block and DNA compatibility when first encountered. This pillar says no — the Pokedex reveals progressively across four research tiers, and each tier is unlocked by playing, not by reading."

#### What This Means for Each Department

| Department | This Pillar Says... | Example |
| ---- | ---- | ---- |
| **Game Design** | Information systems must gate knowledge behind player action. Every piece of data the player receives should feel earned, not gifted. | Pokedex System's 4-tier research progression (Silhouette → Basic Profile → Full Profile → Research Complete) requires fighting, capturing, and experimenting to advance. Each tier unlocks gameplay-relevant data — DNA compatibility and move pools only appear at tier 3. |
| **Art** | Visual design must embed discoverable information — creature appearances, terrain patterns, and environmental cues should reward attentive observation. | Color & Pattern System ties creature base color to primary type affinity. An observant player can infer a wild creature's type from its color before the Pokedex confirms it — visual literacy becomes a gameplay skill. |
| **Audio** | Sound design should carry discoverable gameplay information. Players who listen should gain an advantage over players who don't. | Sound Mutation System gives each creature a DNA-driven audio fingerprint. An experienced player can identify a creature's DNA modifications by ear before seeing its stat sheet — recognizing fire-crackle undertones or crystal-chime resonance. |
| **Narrative** | Story and lore are uncovered through play actions, not exposition dumps. Narrative rewards engagement depth. | Pokedex System unlocks lore entries, personality quirks, and flavor text only at Full Profile tier (captured + multiple battles). The creature's story is earned through relationship, not handed out on first sighting. |
| **Engineering** | Systems must support progressive information disclosure and serve tiered data to the UI based on earned research tier. | Creature Database serves data filtered by the player's current Pokedex tier for that species. Combat UI, Creature Forge, and Party Management screens all respect the tier gate — no system leaks information the player hasn't earned. |

#### Serving This Pillar

- **Pokedex System** — 4-tier progressive discovery where each tier unlocks meaningful gameplay information (DNA compatibility at tier 3, all recipes at tier 4) and narrative lore, creating a research journal that fills through play
- **DNA Alteration System** — DNA recipes are discovered through experimentation, not listed in a crafting menu; specific mod combinations unlock rare synergies that reward systematic testing
- **DNA Vault System** — hidden ancient ruins require environmental puzzle-solving to access, containing Forbidden Mods and unique part blueprints not available through any other path
- **Living Ecosystem** — predator/prey behaviors, migration patterns, and conservation dynamics are observable in the world but never explained in a tutorial; players who watch and track learn the rules
- **Capture System** — Catch Predictor UI shows probability but doesn't reveal the underlying formula variables; players learn through experience which factors (HP %, status effects, trap type, terrain) improve success
- **Fossil System** — fossilized DNA in dig sites and ancient labs must be excavated and reconstructed with incomplete genomes; the player discovers what the creature was through the act of rebuilding it

#### Violating This Pillar

- Adding a bestiary or wiki that shows all creature data from game start — destroys the progressive discovery arc that gives the Pokedex its purpose
- Showing DNA recipe lists before the player has discovered them through experimentation — removes the "what happens if I splice X with Y?" curiosity that drives the engineering loop
- Placing map markers on every encounter, treasure, vault, and secret — converts exploration into waypoint-following and kills the reward for attentive navigation
- Creating an extended tutorial that explains all DNA modification tiers upfront in the first zone — front-loads information instead of letting players discover system depth at their own pace
- Displaying the full type effectiveness chart in-game from the start — removes the early-game learning-through-combat that makes type discovery exciting
- Having NPCs explicitly tell the player where to find rare creatures or hidden DNA recipes — replaces earned knowledge with handed-out answers

---

### Pillar 4: Living World

**One-Sentence Definition**: The game world operates by its own ecological and social rules, and it visibly reacts to player behavior — habitats deplete, ecosystems shift, and rivals adapt whether the player is watching or not.

**Target Aesthetics Served**: Fantasy, Narrative

**Design Test**: "We're debating whether to add a creature respawn system that silently resets encounter pools between sessions. This pillar says no — if a species is over-captured, the ecosystem must deplete visibly. Predators move in. Rare spawns vanish. The world has consequences."

#### What This Means for Each Department

| Department | This Pillar Says... | Example |
| ---- | ---- | ---- |
| **Game Design** | World systems must simulate behavior that persists across sessions and reacts to cumulative player actions. The world is not a static backdrop. | Living Ecosystem tracks predator/prey populations, migration cycles, and conservation scoring per zone. Over-capturing a prey species causes predators to shift zones and rare spawns to vanish. Conservation bonuses unlock exclusive rare creature appearances. |
| **Art** | Environments must communicate ecological state visually — a thriving zone and a depleted zone cannot look identical. | Campaign Map should reflect ecosystem health through visual density: lush vegetation, visible creature silhouettes, and ambient particle effects in healthy zones; sparse terrain, fewer ambient creatures, and muted colors in depleted ones. |
| **Audio** | Environmental audio must reflect world state. The player should hear ecosystem health before checking any menu. | Audio System's ambient habitat soundscapes thin out in depleted zones (fewer creature calls, less insect activity, wind more prominent) and intensify in thriving ones (layered creature vocalizations, rustling, water sounds). Silence signals consequences. |
| **Narrative** | NPCs and rivals must reference the player's impact on the world. No canned dialogue that ignores player history. | Institute Rank System feeds the player's ethics score (conservation, black market use, Pokedex research) into NPC dialogue. Station staff address you differently per rank. Rivals mock or respect your conservation record. Black market use triggers specific confrontation dialogue. |
| **Engineering** | World state must persist across sessions, propagate changes across interconnected systems, and remain serializable within save file size budgets. | Save/Load System must serialize full ecosystem state (population counts per species per zone, migration phase, conservation score, rival adaptation state) alongside creature and player data. The world the player returns to must be the world they left. |

#### Serving This Pillar

- **Living Ecosystem** — predator/prey dynamics, herd migration cycles, conservation scoring, and over-capture population consequences create a world that tracks and responds to cumulative player behavior
- **Rival Trainer System** — rivals adapt team composition and strategy to counter the player's most-used creatures and tactics, evolving across encounters as characters with memory
- **Campaign Map** — 5 habitat zones (Verdant Basin, Ember Peaks, Frozen Reach, Storm Coast, Shadow Depths) with distinct terrain, weather, and creature populations that function as interconnected ecosystems
- **Encounter System** — encounter types and creature composition vary by zone ecosystem state and player progression, not random number generation alone
- **Institute Rank System** — ethics-based ranking reacts to conservation effort, Pokedex research depth, and black market transactions; rank gates content, changes NPC behavior, and unlocks grant funding
- **Weather System** — per-tile-region weather zones that shift type effectiveness, alter terrain behavior, and trigger weather-dependent abilities, making the environment an active participant in combat

#### Violating This Pillar

- Resetting encounter pools silently between sessions — makes the world feel like a vending machine that restocks overnight, destroying persistence
- Making rival trainers use fixed teams that never adapt to the player — removes the sense that another intelligence inhabits the world and pays attention
- Creating zones with no interconnected ecology — each area as an isolated set piece with no relationship to adjacent zones or the player's capture history
- Having NPCs deliver identical dialogue regardless of player actions, rank, or ethical choices — breaks the illusion that the world registered what the player did
- Making weather purely cosmetic with no gameplay effects on combat, terrain, or creature behavior — wastes a major "the world has rules" signal on decoration
- Allowing unlimited capture with no population consequences — contradicts the foundational premise that this is a real ecosystem the player is studying, not a warehouse of spawners

---

## Anti-Pillars (What This Game Is NOT)

Anti-pillars are equally important as pillars — they prevent scope creep and
keep the vision focused. Every "no" protects the "yes."

Great anti-pillars are things the team might actually want to do. "NOT a racing
game" is obvious and useless. "NOT an open-world game" is useful if the genre
could plausibly support it.

- **NOT a grind treadmill**: Progression comes from smart DNA choices and tactical positioning, not from repeating the same battles for XP. Grinding contradicts Pillar 1 (Genetic Architect) by making growth automatic rather than authored, and Pillar 3 (Discovery) by replacing experimentation with repetition.

- **NOT a hand-holding tutorial**: Extended tutorials, quest markers to follow, and pop-up explanations undermine Pillar 3 (Discovery Through Play). The Pokedex, the grid, and the DNA system teach through use — the player earns understanding by doing, not by being told.

- **NOT a numbers-only RPG**: Pillar 1 (Genetic Architect) demands that every DNA modification produce visible, audible, tangible results on the creature — color shifts, part attachments, vocal changes, animation blending. If engineering choices exist only as stat deltas in a menu, the authorship fantasy collapses.

- **NOT a competitive esport**: The game prioritizes single-player tactical depth and creature expression (Pillars 1 and 2). Designing for competitive balance would constrain DNA modification freedom, flatten creature diversity into homogeneous optimal builds, and force Pillar 1 to yield to meta optimization.

- **NOT a gacha / live-service**: Premium, content-complete at launch. FOMO mechanics and loot boxes compromise Pillar 3 (discoveries are earned through play, not purchased) and Pillar 4 (the world has persistent ecological rules, not ones rewritten by seasonal monetization events).

---

## Pillar Conflict Resolution

When two pillars conflict (and they will), use this priority order. The ranking
reflects which aspects of the experience are most essential to the core fantasy.

| Priority | Pillar | Rationale |
| ---- | ---- | ---- |
| 1 | **Genetic Architect** | This is the identity pillar — the "and also" that separates Gene Forge from every other creature-collection RPG. Without it, the game is a competent tactics game with a Pokemon skin. Every other pillar exists to give DNA engineering a stage, a context, and a reason to matter. When any other pillar threatens to constrain modification freedom, the question is: "Can we protect grid integrity / discovery pacing / world reactivity without reducing the player's authorship space?" If yes, find that solution. If no, Genetic Architect wins. |
| 2 | **Tactical Grid Mastery** | This is the engagement pillar — the core loop's moment-to-moment heartbeat. DNA engineering without a satisfying tactical arena is a spreadsheet. No creature build should trivialize the grid. When Pillar 1 and Pillar 2 conflict (e.g., a DNA combination creates a creature that ignores positioning), the resolution is to make powerful builds *require more tactical skill to pilot*, not to remove the build option. Instability costs, part conflicts, and terrain dependency are the balancing mechanisms — they serve both pillars simultaneously. |
| 3 | **Discovery Through Play** | This is the retention pillar — the reason players return session after session. It can be dialed in intensity (more or fewer Pokedex tiers, more or fewer hidden recipes) without losing the game's identity. When it conflicts with Pillar 1 (player can't find the DNA recipe they want and feels blocked), add more discovery paths (NPC hints, experimentation bonuses, Pokedex research rewards) rather than exposing a recipe list. Preserve the *earning*, adjust the *friction*. |
| 4 | **Living World** | This is the immersion pillar — the most scope-expensive and the first to simplify under production pressure. A fully reactive ecosystem with migration, conservation scoring, and adaptive rivals is the full vision. Under scope pressure: ecosystem reduces to state machines, weather becomes static per zone, rival adaptation simplifies to type-counter logic. The world must still *feel* reactive even if the simulation underneath is simpler. Perception of life matters more than simulation fidelity. |

**Resolution Process**:
1. Identify which pillars are in tension
2. Consult the priority ranking above
3. If the lower-priority pillar can be served partially without compromising the higher-priority one, do so — most conflicts have a "serve both" solution
4. If not, the higher-priority pillar wins
5. Document the decision and rationale in the relevant design document
6. If the conflict is fundamental (two pillars are irreconcilable), escalate to the creative-director to consider revising the pillars themselves

---

## Player Motivation Alignment

| Need | Which Pillar Serves It | How |
| ---- | ---- | ---- |
| **Autonomy** (meaningful choice, player agency) | **Genetic Architect** (primary), Tactical Grid Mastery (secondary) | DNA modification offers dozens of viable build paths per creature with no single correct answer. Grid combat supports multiple tactical approaches — aggressive push, terrain control, flanking, creature synergy. Campaign map offers branching routes. The player authors their team, their tactics, and their path. |
| **Competence** (mastery, skill growth, clear feedback) | **Tactical Grid Mastery** (primary), Discovery Through Play (secondary) | Grid combat provides immediate readable feedback: terrain synergy highlights, type effectiveness callouts, height bonus indicators, damage numbers. The Pokedex progression gives concrete mastery milestones across four tiers. Institute rank gates content by demonstrated competence. The player always knows why they won or lost. |
| **Relatedness** (connection, belonging, emotional bond) | **Living World** (primary), Genetic Architect (secondary) | Rival trainers evolve as characters across encounters and adapt to the player's choices. Creature affinity bonds develop through shared battles, unlocking personality quirks and combo moves. NPCs react to Institute rank, battle scars, and ethical choices. The player's engineered creatures become emotional anchors — battle scars and DNA lineage trees encode shared history. Relatedness is the weakest need served because the game is single-player with no real-time social systems at MVP. |

**Gap check**: All three SDT needs are served by at least one pillar. Relatedness is the thinnest — it depends on NPC relationships and creature attachment rather than human social connection. This is a known trade-off of the single-player-first anti-pillar. The creature affinity system and rival trainer arcs are the primary mitigation: they create parasocial bonds that substitute for fellowship. Post-MVP async PvP and DNA trading would strengthen Relatedness significantly.

---

## Emotional Arc

### Session Emotional Arc

| Phase | Duration | Target Emotion | Pillar(s) Driving It | Mechanics Delivering It |
| ---- | ---- | ---- | ---- | ---- |
| Opening | 0-5 min | Anticipation, planning | Genetic Architect | Review creatures at research station. Apply DNA modifications earned last session. Equip new body parts. Set party composition. The player begins every session as the architect, surveying and improving their creations. |
| Rising | 5-25 min | Tension, focus, flow | Tactical Grid Mastery, Discovery Through Play | Explore habitat zone through 3-5 grid encounters. Each fight demands positioning decisions on new terrain. Between combats: new Pokedex silhouettes spotted, ecosystem behavior observed, zone paths scouted. Discovery beats punctuate tactical tension. |
| Climax | 25-40 min | Triumph, exhilaration, risk | Tactical Grid Mastery, Genetic Architect | Rival trainer battle or trophy creature encounter. The player's DNA builds and tactical skill are tested together at peak difficulty. High-instability creatures may breakthrough or disobey — risk and reward peak simultaneously. |
| Resolution | 40-50 min | Satisfaction, reflection | Genetic Architect, Discovery Through Play | Return to research station. Spend earned DNA materials on new modifications. Review Pokedex progress — new tiers unlocked, new lore revealed. Observe ecosystem changes from the session's captures. The player sees concrete results from the session's work. |
| Hook | End of session | Curiosity, unfinished business | Discovery Through Play, Living World | New creature silhouettes spotted but not yet fought. A DNA recipe partially discovered — one more splice to test. Ecosystem shifted visibly — a new migration pattern emerging. Rival hinted at a counter-strategy. The player leaves with threads to pull next time. |

### Long-Term Emotional Progression

**Early game (Verdant Basin)** — *Wonder and learning.* First captures, first DNA mods, first grid victories. The Pokedex is mostly silhouettes. DNA modification is limited to basic stat boosts at a Level 1 station. The player is an Intern, awed by the ecosystem's complexity, learning one system at a time. The world feels vast and full of secrets. Primary emotions: curiosity, discovery, small victories.

**Mid game (Zones 2-3)** — *Confidence and ambition.* DNA builds growing complex — cross-species splicing unlocked, body parts meaningfully reshaping creature capabilities. Rivals are a genuine tactical threat who remember and adapt. The player starts pushing instability higher, gambling on breakthroughs for power. Pokedex filling in with earned knowledge. Institute rank rising to Researcher. Primary emotions: pride in builds, tension in combat, the thrill of risk-taking.

**Late game (Zones 4-5)** — *Mastery and ethical tension.* Forbidden Mods available from DNA Vaults. Black market DNA tempting with powerful shortcuts that carry rank penalties. Trophy creatures demanding peak tactical execution. Conservation-vs-power trade-offs crystallize — over-capture has visible ecosystem consequences. The player's choices have reshaped the world. Primary emotions: moral weight, hard-won mastery, consequences made tangible.

**Endgame (Arena + completion)** — *Pride and legacy.* The creature roster is uniquely the player's creation — no other player has this team, these scars, this lineage history. Creature Arena's themed floors and rule modifiers test every build and tactic. Full Pokedex completion is a research achievement, not a checklist. The player isn't just a researcher anymore — they're a pioneer whose engineering and choices reshaped an ecosystem. Primary emotions: ownership, legacy, the desire to show others what you built.

---

## Reference Games

| Reference | What We Take From It | What We Do Differently | Which Pillar It Validates |
| ---- | ---- | ---- | ---- |
| **Pokemon** | Creature collection loop, type effectiveness chart, progressive Pokedex, creature bonds | Replace evolution with DNA splicing; tactical grid combat instead of 1v1 turns; visible mutations instead of static models; Pokedex earned through research, not just capture | Genetic Architect, Discovery Through Play |
| **Shin Megami Tensei** | Demon fusion as a core loop, dark tone, strategic depth, press-turn combat | DNA splicing is additive (donor not consumed); instability adds risk/reward absent from fusion; body part system gives granular control vs. SMT's wholesale fusion | Genetic Architect |
| **Final Fantasy Tactics** | Height-based terrain advantage, job/ability system, positioning as primary tactical axis | Real-time weather zones instead of global effects; creature-based instead of human units; terrain alteration mid-combat adds dynamic grid reshaping FFT lacks | Tactical Grid Mastery |
| **XCOM** | Grid-based tactical combat, cover system, squad customization, permadeath as emotional amplifier | Creature-based instead of soldiers; DNA mods instead of equipment; capture instead of kill; terrain synergy instead of pure cover — the grid rewards affinity, not just protection | Tactical Grid Mastery, Living World |
| **Hollow Knight** | Atmosphere over explanation, world teaches through design, earned mastery through play | Applied to information systems rather than platforming — Pokedex, DNA recipes, and ecosystem rules are learned by doing, not told. The world is the tutorial. | Discovery Through Play |

**Non-game inspirations**:

- **Jurassic Park / Jurassic World** — Genetic engineering as spectacle and hubris. "Life finds a way" maps directly to the instability system: push too far and your creation becomes unpredictable. The fantasy of playing god with DNA, and the tension of consequences. *(Validates: Genetic Architect, Living World)*
- **The Island of Doctor Moreau** — The body horror of hybrid creatures and the ethical boundaries of modification. Informs visible mutations, forbidden mods, black market DNA ethics, and the question of how far is too far. *(Validates: Genetic Architect)*
- **Real-world genetics / CRISPR** — Gene splicing terminology, inheritance patterns, mutation risk, cascading effects. Grounds the DNA system in recognizable science language. The idea that modifications have downstream consequences the modifier can't fully predict. *(Validates: Discovery Through Play, Genetic Architect)*

---

## Pillar Validation Checklist

- [x] **Count**: 4 pillars — within the 3-5 range
- [x] **Falsifiable**: Each pillar makes a testable claim. "Personally authored through DNA" is false if mods are trivial or meaningless. "Won through positioning" is false if stat-checks dominate combat. "Earned through observation" is false if information is handed out upfront. "Operates by its own ecological rules" is false if the world is static between sessions.
- [x] **Constraining**: Each pillar forced "no" to a plausible feature in its design test — level-based evolution (P1), auto-resolve (P2), full creature data on first encounter (P3), silent encounter respawning (P4). Each Violating section names 6 specific tropes the project could drift toward.
- [x] **Cross-departmental**: Each pillar has non-trivial implications for all 5 departments (Game Design, Art, Audio, Narrative, Engineering) with concrete examples citing real project systems. Verified in department tables.
- [x] **Design-tested**: Each pillar has a concrete design test that resolves a real decision with a specific outcome, not a vague guideline.
- [x] **Anti-pillars defined**: 5 anti-pillars (grind treadmill, hand-holding tutorial, numbers-only RPG, competitive esport, gacha/live-service), all representing directions the project could plausibly drift toward given genre conventions.
- [x] **Priority-ranked**: Clear 1-4 ranking (Genetic Architect > Tactical Grid Mastery > Discovery Through Play > Living World) with conflict-resolution rationale explaining why each rank holds its position and how conflicts between adjacent priorities are resolved.
- [x] **MDA-aligned**: Top 3 target aesthetics (Expression, Discovery, Challenge) map directly to Pillars 1, 3, and 2 respectively. Fantasy (rank 4) is shared between Pillars 1 and 4. No aesthetic orphan in the top 4.
- [x] **SDT coverage**: Autonomy served strongly by Pillar 1 (DNA build freedom). Competence served strongly by Pillar 2 (readable tactical feedback). Relatedness served moderately by Pillar 4 (rival relationships, creature bonds, NPC reactions). All three needs covered; Relatedness is the thinnest, which is a known trade-off of the single-player-first anti-pillar.
- [x] **Memorable**: Four short names a team can recite: Genetic Architect, Tactical Grid Mastery, Discovery Through Play, Living World.
- [x] **Core fantasy served**: "Your team isn't found — it's *built*" → P1. "Every battle is a test of tactics" → P2. "Every creature captured is raw material for discovery" → P3. "You are a field researcher in a living world" → P4. Every pillar traces directly to the core fantasy statement.

---

## Next Steps

- [ ] Get pillar document approval from creative-director (this review)
- [ ] Distribute to all department leads (game-designer, art-director, audio-director, narrative-director, technical-director) for sign-off
- [ ] Create Sprint 1 design tests: apply each pillar to the first real implementation decisions in the Foundation Layer (Data Configuration Pipeline, Grid/Tile System, Game State Manager, Type Chart System)
- [ ] Schedule first pillar review after 2 weeks of active development — validate that implementation decisions are passing the design tests
- [ ] Cross-reference pillars in game-concept.md to ensure consistency between the concept document and this pillar document

---

*This document is the creative north star. It lives in `design/gdd/game-pillarsx.md`
and is referenced by every design, art, audio, and narrative document in the project.
Review quarterly or after major milestone pivots.*
