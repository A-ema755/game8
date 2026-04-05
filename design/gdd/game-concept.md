# Gene Forge — Game Concept

*Created: 2026-04-04*
*Status: Approved*

---

## Elevator Pitch

A tactical creature-collection RPG on an isometric 3D grid where you capture, battle, and **genetically engineer** your creatures. No evolution — instead, splice DNA between species to create hybrid abilities, alter types, and visibly mutate your team. The Pokedex is a living research journal that fills in as you observe, fight, and experiment.

---

## Core Identity

| Field | Value |
|-------|-------|
| **Genre** | Tactical Creature-Collection RPG |
| **Platform** | PC (Windows), cross-platform post-MVP |
| **Target Audience** | Creature-collection fans who want more tactical depth (age 16-35) |
| **Player Count** | Single-player (async PvP post-MVP) |
| **Session Length** | 30-90 minutes |
| **Monetization** | Premium (buy-to-play) |
| **Estimated Scope** | Large (9+ months) — 53 total systems, 27 MVP |
| **Comparable Titles** | Pokemon, Shin Megami Tensei, Final Fantasy Tactics, Disgaea, XCOM |

---

## Core Fantasy

You are a field researcher and genetic pioneer. Every creature you capture is raw material for discovery. Every battle is both a test of tactics and a source of new DNA. Your team isn't found — it's *built*.

---

## Unique Hook

**You don't evolve creatures — you engineer them.** DNA splicing replaces evolution as the core growth mechanic, letting you graft abilities, alter types, and visibly mutate creatures into unique hybrids that no two players will build the same way.

**"And also" test:** "It's a tactical creature-collection RPG *and also* a genetic engineering sandbox where every creature is a custom creation."

---

## Player Experience Analysis (MDA Framework)

### Target Aesthetics

| Rank | Aesthetic | How We Deliver It |
|------|-----------|-------------------|
| 1 | **Expression** | DNA splicing, body part equipping, visible mutations — every creature is a personal creation |
| 2 | **Discovery** | Progressive Pokedex, hidden DNA recipes, DNA Vaults, fossil system, ecosystem secrets |
| 3 | **Challenge** | Tactical grid combat with terrain synergy, height advantage, weather zones, combo moves |
| 4 | **Fantasy** | You are a field researcher and genetic pioneer building the ultimate team from raw DNA |
| 5 | **Narrative** | Rival trainers that adapt to you, Institute rank progression, ethical choices with black market DNA |
| 6 | **Sensation** | Visible mutations, sound mutations, procedural animation blending, battle scars |
| 7 | **Submission** | Auto-battle toggle, speed modes, expedition mode for idle play, session-friendly save points |
| 8 | **Fellowship** | Async PvP (post-MVP), DNA trading (post-MVP), leaderboards |

### Key Dynamics (Emergent Player Behaviors)

- **Build theorycrafting** — players experiment with DNA combinations to discover powerful synergies and share "recipes"
- **Risk-reward gambling** — pushing instability higher for more power while managing unpredictability
- **Ecosystem stewardship** — balancing capture rates against conservation bonuses and rare spawns
- **Adaptive rivalry** — adjusting team composition to counter rivals who counter your previous strategies
- **Terrain manipulation** — reshaping the battlefield mid-combat to gain positional advantage

### Core Mechanics

1. **Turn-based tactical grid combat** — isometric 3D grid with height, terrain types, and weather zones
2. **DNA splicing and body part system** — graft abilities, alter types, equip parts to creature body slots
3. **Progressive creature research** — Pokedex reveals information through observation, combat, and experimentation
4. **Creature capture and party management** — Gene Traps, affinity bonds, field abilities for world traversal
5. **Campaign exploration** — habitat zones, research stations, rival encounters, environmental puzzles

---

## Player Motivation Profile

### Primary Psychological Needs (Self-Determination Theory)

| Need | How Gene Forge Satisfies It |
|------|----------------------------|
| **Autonomy** | Open DNA modification choices, multiple viable build paths, branching campaign routes, team composition freedom |
| **Competence** | Clear tactical feedback (terrain synergy, type effectiveness), progressive Pokedex mastery, Institute rank milestones |
| **Relatedness** | Rival trainer relationships that evolve, creature affinity bonds with personality quirks, NPC reactions to your rank and choices |

### Player Type Appeal (Bartle Taxonomy)

| Type | Appeal | How |
|------|--------|-----|
| **Achievers** | High | Pokedex completion, Institute ranks, DNA recipe discovery, trophy battles, arena leaderboards |
| **Explorers** | High | Hidden DNA Vaults, fossil dig sites, ecosystem secrets, progressive information reveal, environmental puzzles |
| **Socializers** | Medium | Rival trainer story arcs, NPC relationships, creature personality systems, async PvP and DNA trading (post-MVP) |
| **Killers** | Medium | Tactical combat mastery, min-max DNA builds, permadeath mode, creature arena challenge floors |

### Flow State Design

| Aspect | Design Approach |
|--------|----------------|
| **Onboarding curve** | First habitat zone (Verdant Basin) introduces one system at a time: capture, combat, then DNA mods. Pokedex guides discovery. |
| **Difficulty scaling** | Rival trainers adapt to your team composition. Trophy battles are optional superbosses. Institute rank gates harder zones. |
| **Feedback clarity** | Catch Predictor UI shows probability. Terrain synergy highlights on the grid. DNA instability meter is always visible. Risk displayed before committing mods. |
| **Recovery from failure** | Failed captures don't consume traps (MVP). Save between any encounter. Permadeath is optional. No permanent loss in normal mode. |

---

## Core Loop

### Moment-to-Moment (~30s)

Turn-based tactical combat on the isometric grid. Position creatures on synergy terrain, exploit height advantage, trigger combo moves, manage weather zones. Each turn is a tactical decision.

### Short-Term (5-15m)

Encounter wild creatures or trainers. Capture targets with Gene Traps. Win battles to earn DNA materials. Return to a research station to modify creatures with new parts and splices.

### Session-Level (30-90m)

Explore a habitat zone across multiple encounters. Visit research stations to heal, modify creatures, and review Pokedex progress. Culminates in a rival battle or trophy creature encounter.

### Long-Term Progression (Across Sessions)

Fill the Pokedex through progressive research tiers. Discover DNA recipes and forbidden mods in ancient vaults. Build an ultimate team of custom-engineered creatures. Rise through Institute ranks. Challenge the creature arena and final trophy gauntlet.

### Summary Table

| Scale | Duration | Activity |
|-------|----------|----------|
| Moment-to-moment | ~30s | Turn-based tactical combat on isometric grid |
| Short-term | 5-15m | Encounter, Capture/Win, Earn DNA Materials, Modify Creatures |
| Session-level | 30-90m | Explore habitat zone, multiple battles, research station, boss/rival |
| Long-term | Across sessions | Fill Pokedex, discover DNA recipes, build ultimate team, challenge trophy battles |

### Retention Hooks

| Hook Type | Implementation |
|-----------|---------------|
| **Curiosity** | Progressive Pokedex reveals, hidden DNA Vaults, fossil discoveries, ecosystem secrets, "what happens if I splice X with Y?" |
| **Investment** | Creature affinity bonds, battle scars as prestige marks, DNA lineage trees, Institute rank progression |
| **Social** | Rival trainers that remember and adapt, NPC reactions to your choices, async PvP and leaderboards (post-MVP) |
| **Mastery** | Terrain manipulation tactics, DNA recipe optimization, instability risk management, creature arena challenge floors |

---

## Game Pillars

### 1. Genetic Architect
Your creatures are your creations. DNA alteration is the core expression mechanic, not evolution.

**Design test:** "Does this feature give the player more ways to customize their creatures through DNA?" If no, deprioritize.

### 2. Tactical Grid Mastery
Positioning, terrain, type synergy, and creature combos win battles — not just stat checks.

**Design test:** "Does this feature reward spatial thinking and positioning on the grid?" If no, deprioritize.

### 3. Discovery Through Play
The Pokedex reveals information progressively. You learn by fighting, capturing, and experimenting — not by reading menus.

**Design test:** "Does this feature encourage the player to explore, experiment, or observe to gain knowledge?" If no, deprioritize.

### 4. Living World
Habitats, weather, day/night, and rival trainers that adapt to you create a world that feels reactive.

**Design test:** "Does this feature make the world feel like it exists and responds independently of the player?" If no, deprioritize.

### Anti-Pillars (What This Game Is NOT)

- **Not a grind treadmill** — progression comes from smart DNA choices and tactical play, not repeating the same battles for XP
- **Not a hand-holding tutorial** — the Pokedex rewards curiosity and experimentation, not following quest markers
- **Not a numbers-only RPG** — visible mutations, battle scars, and procedural animations make your choices tangible, not just stat sheets
- **Not a competitive esport** — single-player tactical depth first; async PvP is a post-MVP bonus, not the core experience
- **Not a gacha/live-service** — premium buy-to-play with complete content; no loot boxes, no battle passes, no FOMO mechanics

---

## Inspiration and References

### Game References

| Reference | What We Take | What We Do Differently | Why It Matters |
|-----------|-------------|----------------------|----------------|
| **Pokemon** | Creature collection, type chart, progressive Pokedex, creature bonds | Replace evolution with DNA splicing; tactical grid combat instead of 1v1 turns; visible mutations instead of static models | Foundation creature-collection loop that millions understand |
| **Shin Megami Tensei** | Demon fusion, dark tone, strategic depth, press-turn combat | DNA splicing is additive (no consuming creatures); instability adds risk/reward; body part system for granular control | Proves creature fusion can be a core loop, not just a side feature |
| **Disgaea** | Isometric grid tactics, extreme customization depth, post-game content | More grounded power curve; DNA mods instead of class changes; creature arena instead of Item World | Shows tactical RPGs can support deep customization without losing accessibility |
| **Final Fantasy Tactics** | Height-based terrain advantage, job/ability system, positioning matters | Real-time weather zones instead of global effects; creature-based instead of human units; terrain alteration mid-combat | Gold standard for "positioning matters" in tactical RPGs |
| **XCOM** | Grid-based tactical combat, cover system, squad customization, permadeath mode | Creature-based instead of soldiers; DNA mods instead of equipment; capture instead of kill; terrain synergy instead of just cover | Modern proof that grid tactics + permadeath + attachment = emotional investment |

### Non-Game Inspirations

| Reference | What We Take | Why It Matters |
|-----------|-------------|----------------|
| **Jurassic Park / Jurassic World** | Genetic engineering as spectacle and hubris; "life finds a way" unpredictability | DNA instability system — pushing too far has consequences. The fantasy of playing god with genetics. |
| **The Island of Doctor Moreau** | Body horror of hybrid creatures; ethical boundaries of modification | Visible mutations, forbidden mods, black market DNA ethics system |
| **Real-world genetics / CRISPR** | Gene splicing terminology, inheritance, mutation risk | Grounded DNA system language; lineage trees; the idea that modifications have cascading effects |

---

## Target Player Profile

| Attribute | Description |
|-----------|-------------|
| **Age range** | 16-35 |
| **Gaming experience** | Intermediate to experienced — familiar with tactical RPGs or creature collectors |
| **Time availability** | 30-90 minute sessions, 3-5 times per week |
| **Platform preference** | PC (keyboard + mouse primary), controller support |
| **Current games** | Pokemon, Shin Megami Tensei, Disgaea, Final Fantasy Tactics, XCOM, Fire Emblem, Monster Hunter |
| **What they're looking for** | Deeper tactical combat than Pokemon; more creature customization than any existing game; the satisfaction of building something unique |
| **What would turn them away** | Excessive grind without meaningful choices; pay-to-win mechanics; shallow combat that resolves through stat checks alone; overly complex UI that obscures the fun |

---

## Technical Considerations

| Aspect | Details |
|--------|---------|
| **Recommended Engine** | Unity 6 (6000.x) with URP |
| **Language** | C# |
| **Key Technical Challenges** | Procedural animation blending for body parts; visible mutation rendering on 3D models; DNA instability effects; ecosystem simulation persistence; height-variable isometric grid |
| **Art Style** | Isometric 3D, simple 3D pieces (MVP) evolving to full 3D models with visible DNA mutations (post-MVP) |
| **Art Pipeline Complexity** | Medium — modular body part meshes that snap to creature archetypes; color/pattern system via material properties; scar overlay textures |
| **Audio Needs** | Creature vocalizations with DNA-based audio processing (pitch, filter, layering); tactical combat SFX; ambient habitat soundscapes; music per zone |
| **Networking** | None for MVP; async PvP requires party upload/download and AI battle simulation (post-MVP) |
| **Content Volume** | 53 designed systems (27 MVP); 20-30 creatures MVP (100+ full vision); 40-60 moves MVP; 5 habitat zones; 5 research station tiers |
| **Procedural Systems** | DNA mutation visuals, animation blending, sound mutations, ecosystem behavior, egg trait randomization, fossil genome gaps |
| **Rendering** | Universal Render Pipeline (URP) — 3D Renderer |
| **Perspective** | Isometric 3D, fixed 45° camera, height-variable grid |

---

## What Makes Combat Unique

### Terrain-Type Synergy
The grid isn't just space — it amplifies your creatures. Fire creatures power up on lava tiles. Water creatures heal on water. Grass creatures gain cover in forest. Positioning a creature on its synergy terrain is as important as type matchups.

### Combo Moves
Two creatures adjacent on the grid can trigger a **fusion attack** that costs both their turns. Combo moves are unlocked by creature affinity (bond level) and type pairing. A fire + ice combo might create "Steam Blast" — area damage that ignores terrain cover.

### Weather Zones
Weather isn't global — it's per-tile-region. A rainstorm covers the left side of the grid while the right stays sunny. Weather shifts type effectiveness, terrain behavior, and triggers weather-dependent abilities.

### Height Advantage
Creatures on higher ground deal bonus damage and gain range. Pushing enemies off ledges (via knockback abilities) deals fall damage.

---

## DNA Alteration System (Core Hook)

### Splicing
Combine DNA from two different species. A fire creature can gain an ice creature's cold resistance perk. The donor creature isn't consumed — you extract DNA materials from captures and battle rewards.

### Visible Mutations
DNA changes alter the creature's 3D model: extra spikes, color shifts, glowing markings, crystalline growths, shadow aura. Your most modified creatures look unmistakably *yours*.

### Body Part System
Creatures have body slots (Head, Back, Arms, Tail, Legs, etc.) based on their archetype (Bipedal, Quadruped, Serpentine, Avian, Amorphous). DNA splicing unlocks **part blueprints** — wings, horns, claws, shells, fangs, auras, glands — that can be equipped to slots. Parts aren't cosmetic:
- **Parts unlock moves** — can't learn Aerial Dive without wings
- **Parts change type affinity** — Crystal Shell adds Rock resistance
- **Parts interact with the grid** — Wings ignore height cost, Roots create difficult terrain, Glands drop hazard tiles
- **Parts conflict** — heavy Carapace + Feathered Wings = weight conflict
- **Part synergy sets** — 3+ same-type parts grant a set bonus (e.g., "Inferno Body")
- **Parts level up** through use and can upgrade into stronger versions
- **Signature parts** are stronger on their original species

### Color & Pattern System
Creature appearance is functional, not just cosmetic:
- **Base color** shifts with primary type — alter type affinity and the color follows
- **Patterns** (stripes, spots, veins, crystalline) are extracted from donor species during DNA splicing
- **Glow/Emission** comes from Aura parts or high instability
- **Scar marks** — permanent marks from failed DNA splices or near-death battles, cannot be removed
- **Camouflage** — creatures whose color matches their terrain tile get +Evasion

### Creature Personality DNA
Behavioral traits alter how a creature acts in and out of combat. One personality trait equipped at a time:
- **Aggressive** — +10% damage, attacks first in speed ties, may act independently
- **Cautious** — +10% evasion, auto-retreats at low HP
- **Loyal** — -instability gain, combo moves cost less, never disobeys
- **Feral** — +20% damage, +instability, may attack allies at high instability
- **Curious** — +XP gain, reveals Pokedex info faster
- **Territorial** — +Defense when holding position for 2+ turns

Personality visually shows: Aggressive = red-tinted eyes, Cautious = flattened posture, Feral = glitch particles.

### Procedural Animation Blending
Animations blend based on equipped parts rather than being fixed per species:
- Wings = wing-flap idle, aerial move animations
- Heavy shell = slowed walk cycle, weight bounce
- Extra limbs = additional swing in attack animations
- Tail weapon = tail whip idle, counter-attack animation
- Small/large size mods = faster/slower animation playback
No two heavily-modded creatures animate the same way.

### Sound Mutations
Each creature has a base vocalization that DNA mods alter:
- Fire DNA adds rumble/crackle undertone
- Crystal parts add chime/resonance
- Poison DNA adds hissing/bubbling
- High instability adds distortion/static
- The creature's "voice" becomes an audio fingerprint of everything you've done to it

### Mutation Risk
Aggressive mods have a chance to cause side effects. High-risk splices can give powerful perks WITH a stat debuff. Risk is displayed before committing.

### DNA Instability
Each creature has a stability meter (0-100). Each mod adds instability. High instability = more powerful but unpredictable (chance to disobey, random stat swings in battle, or rare "breakthrough" bonus effects). Managing instability is a strategic choice.

### DNA Lineage Tree
Track every modification on every creature. Discover "recipes" — specific mod combinations that unlock rare synergies. The lineage tree is shareable (post-MVP: DNA trading between players).

---

## Creature Collection

### Progressive Pokedex
Information reveals gradually:
1. **Silhouette** — seen in the wild but not fought
2. **Basic profile** — fought once (type, base stats visible)
3. **Full profile** — captured (move pool, habitats, DNA compatibility revealed)
4. **Research complete** — battled 10+ times, all DNA recipes discovered

### Creature Affinity
Bond level grows from battling together. Higher affinity unlocks:
- Unique combo moves with other high-affinity party members
- Affinity perks (passive bonuses)
- Lore entries and personality quirks in the Pokedex
- Reduced DNA instability from modifications

### Nesting Sites
Find eggs in the world with randomized DNA traits. Hatched creatures start with innate perks that can't be removed — making nest finds valuable even if you already own that species.

### Shiny / Variant System
Rare color/model variants with unique innate DNA traits. Variants have a distinct visual marker and are prized for their exclusive starting perks.

---

## World Structure

### Habitat Zones
The campaign map is divided into biomes, each with unique creatures, terrain types, and weather patterns:

| Zone | Terrain | Weather | Creature Types |
|------|---------|---------|----------------|
| Verdant Basin | Forest, grass, water | Rain, sun | Grass, Water, Bug |
| Ember Peaks | Lava, rock, high ground | Heat haze, ash storms | Fire, Rock, Dragon |
| Frozen Reach | Ice, snow, cliffs | Blizzard, aurora | Ice, Ghost, Steel |
| Storm Coast | Water, sand, high wind | Thunderstorms, fog | Electric, Water, Flying |
| Shadow Depths | Dark caves, crystal, fungus | Permanent dark, bioluminescence | Dark, Poison, Psychic |

### Day/Night Cycle
- Affects creature spawns (nocturnal creatures only appear at night)
- Shifts type effectiveness (Dark moves +10% at night, Light/Fairy +10% during day)
- Visual lighting changes on the isometric grid

### Research Stations
Safe zones between battles where you:
- Perform DNA alterations
- Heal your party
- Store/swap creatures
- Review Pokedex progress
- Craft DNA materials from raw components

### Rival Trainers
Recurring enemies that **adapt to your team**. If you rely on fire creatures, your rival builds a water-heavy counter team next time. They have names, personalities, and story arcs.

---

## Encounter Types

| Type | Description | Reward |
|------|-------------|--------|
| **Wild** | 1-3 wild creatures on the grid. Can capture. | Capture opportunity + DNA materials |
| **Trainer** | Rival or NPC trainer battle. Cannot capture. | DNA materials + currency + story progression |
| **Nest** | Find and protect a nest from wild predators. | Creature egg with innate DNA traits |
| **Trophy** | Optional superboss creature. Extremely tough. | Unique DNA recipe + rare materials + Pokedex entry |
| **Horde** | Large wave of weak creatures. | Bulk DNA materials + XP |

---

## Party System

- **Active party:** 4-6 creatures (configurable per difficulty)
- **Storage:** Unlimited creature storage at research stations
- **Field abilities:** Each creature has an out-of-combat ability:
  - Fire creature: clear forest/ice obstacles on campaign map
  - Water creature: cross water terrain
  - Electric creature: power up devices, open electric doors
  - Flying creature: scout ahead (reveal hidden encounters)
  - Psychic creature: sense rare creatures nearby

---

## Capture System

- **Gene Traps** (this world's version of Pokeballs)
- Catch rate formula based on: target HP %, type of trap, status effects, creature rarity
- **Catch Predictor UI:** shows probability before throwing
- Failed captures don't consume the trap (MVP simplification) OR consume trap (post-MVP challenge)
- Some creatures require special conditions (weather, time, terrain) to be capturable

---

## Progression

### XP & Leveling
- Creatures gain XP from battles
- Leveling increases stats via growth curves (per species)
- New moves learned at level thresholds
- No evolution — DNA alteration is the growth vector

### Currency: Research Points (RP)
- Earned from battles, Pokedex entries, DNA discoveries
- Spent at research stations for: DNA materials, gene traps, healing, crafting

### Campaign Progression
- Linear-ish with branching paths (like campaign map from old design)
- Each habitat zone ends with a rival battle or trophy creature
- Story unfolds through rival encounters + research station NPCs
- Endgame: fill Pokedex, challenge final trophy gauntlet

---

## Quality of Life

- **Auto-battle toggle** — AI plays your team for easier encounters
- **Speed modes** — 1x / 2x / 4x combat animation speed
- **Team presets** — save party + DNA loadouts for quick swapping
- **Catch predictor** — probability display before capture attempts
- **Move reminder** — re-learn any previously known move at research station

---

## Terrain Alteration (Select Creatures Only)

Not all creatures can alter the battlefield — it's a rare trait tied to specific species and parts. Creatures with terrain-altering abilities can:
- **Scorch tiles** (fire) — missed fire attacks or fire abilities turn tiles to lava
- **Freeze tiles** (ice) — ice moves freeze water tiles, create slippery terrain
- **Grow terrain** (grass/plant) — vine walls that block movement, overgrowth that provides cover
- **Flood tiles** (water) — create water tiles in low areas
- **Electrify tiles** (electric) — charged tiles that damage creatures stepping on them
- **Corrode tiles** (poison) — toxic puddles that apply status effects

Weather alteration is even rarer — only certain powerful species or DNA-modded creatures can shift weather zones during combat (create local rain, sandstorm, fog patches).

---

## Threat / Aggro System

Wild creatures target based on a threat score:
- **Damage dealers** generate high threat
- **Closest creature** draws aggro from territorial enemies
- **Low HP creatures** attract predator-type enemies
- Certain moves manipulate aggro: **Taunt** (force target you), **Stealth** (reduce threat to near zero), **Provoke** (redirect all aggro to an ally)
- Trainer battles use AI personality instead of aggro

---

## Living Ecosystem

Wild creatures have behavior on the campaign map — they're not just random encounters:
- **Predators** actively hunt prey species in their zone
- **Herds** migrate between areas on a cycle
- **Over-capture consequences** — deplete a species and predators move in, rare prey disappears, ecosystem rebalances
- **Conservation bonus** — maintaining ecosystem health in a zone unlocks rare creature spawns and research grants
- The ecosystem state persists in your save

---

## Environmental Puzzles

Creature field abilities solve map puzzles to unlock hidden content:
- Freeze a waterfall = bridge to a hidden nesting site
- Burn through a thorny wall = shortcut to a DNA vault
- Electric-shock a dead generator = open a sealed ancient lab
- Grow vines across a chasm = reach an isolated habitat
- Creatures must be in your active party to use their field ability

---

## DNA Vaults (Ancient Ruins)

Pre-collapse research stations hidden in each habitat zone:
- Contain **Forbidden Mods** — experimental DNA that was abandoned for being too unstable
- Forbidden Mods are extremely powerful parts/alterations with high instability costs
- Each vault has a guardian (trophy-level creature protecting the data)
- Vault DNA recipes are unique — can't be found anywhere else
- Vaults require environmental puzzle-solving to access

---

## Fossil System

Discover fossilized DNA in specific zones (dig sites, cave walls, ancient labs):
- Resurrect **extinct creatures** with incomplete genomes
- Fossil creatures have random stat gaps (some stats permanently lower) but unique **ancient parts** unavailable on living species
- Ancient parts have no type — they're neutral but powerful
- Rare fossils require special extraction at upgraded research stations
- Fossil creatures have their own Pokedex section: "Paleo Archive"

---

## Creature Calls

Learn to mimic creature vocalizations from Pokedex research:
- **Lure calls** — attract a specific species on the campaign map
- **Scare calls** — repel certain species, avoid unwanted encounters
- **Trigger calls** — cause environmental reactions (a dragon's call clears rockslides, a whale's call raises water levels)
- Calls unlock as you reach "Full Profile" research tier on that species
- Wrong call in the wrong zone may attract aggressive creatures

---

## Black Market DNA

An underground trader operating outside the Institute's ethics rules:
- Offers **stolen legendary DNA**, unstable experimental parts, combat stimulants (+stats for one battle, permanent +instability)
- Using black market DNA has **story consequences** — rival trainers comment on it, certain NPCs refuse help, Institute reputation decreases
- Black market inventory rotates, prices are high, some items are one-of-a-kind
- Accessible after discovering a hidden contact in each habitat zone

---

## Research Station Upgrades

Stations improve as you invest Research Points:

| Level | Cost | Unlocks |
|-------|------|---------|
| **1 (Field Lab)** | Default | Basic DNA mods (stat boosts), healing, party swap |
| **2 (Gene Lab)** | 500 RP | Part grafting, basic color mods |
| **3 (Splice Lab)** | 1500 RP | Cross-species DNA splicing, pattern extraction |
| **4 (Mutation Lab)** | 3000 RP | Personality modification, instability management |
| **5 (Apex Lab)** | 5000 RP | Forbidden Mod installation, fossil resurrection, advanced crafting |

Upgrades apply to ALL research stations once purchased.

---

## Creature Arena

Endgame battle tower with escalating challenge:
- **Themed floors** with rule modifiers: no items, double speed, one creature only, type-restricted, no DNA mods
- **Floor bosses** every 5 levels with unique AI and exclusive part drops
- **Leaderboard** — floors cleared, fastest times
- Rewards: exclusive parts, cosmetic variants, rare DNA materials, achievement titles
- Arena resets weekly with new modifier combinations (post-MVP)

---

## Expedition Mode

Send idle creatures (not in active party) on autonomous missions:
- Expeditions take real-time hours (1-8h depending on difficulty)
- Creatures return with: DNA materials, eggs, discovered creatures, map intel, rare fossils
- Success rate based on: creature level, type match to expedition zone, affinity level
- Higher-affinity creatures bring back better rewards
- Can send up to 3 expeditions simultaneously
- Expedition log shows a text summary of what your creature encountered

---

## Institute Rank

The Institute (your employer/sponsor) tracks your performance:

| Rank | Requirement | Perks |
|------|------------|-------|
| **Intern** | Start | Basic station access, standard gene traps |
| **Field Agent** | 10 Pokedex entries | Uncommon gene traps, +10% RP from battles |
| **Researcher** | 30 entries + 5 DNA recipes | Station Level 3 access, exclusive zone permits |
| **Senior Researcher** | 60 entries + conservation score > 50 | Rare gene traps, grant funding (500 RP/session), rival intel |
| **Lead Scientist** | 100 entries + all zones explored | Station Level 5, legendary trap access, Forbidden Mod clearance |

- **Ethics matter** — black market use drops your rank. Conservation and Pokedex completion raise it.
- **Rank gates content** — higher ranks unlock restricted zones, better traps, and Institute-only DNA recipes.
- **NPCs react** — station staff address you differently per rank. Rivals respect or mock your rank.

---

## Move Customization via DNA

Moves aren't fixed — DNA mods can alter them:
- **Type infusion** — graft Ice DNA onto a fire creature = Flamethrower becomes "Frostflame" (Fire/Ice dual-type)
- **Part-based moves** — equip Venom Glands = basic attack gains poison chance. Equip Wings = unlock aerial variants of existing moves.
- **Instability effects** — creatures at 50+ instability gain random bonus effects on moves (extra crit chance, AoE splash, status proc). Powerful but unpredictable.
- **Move mastery** — use a move 50+ times and it can upgrade into a stronger version with reduced PP. Player choice to upgrade or keep the original.

Move customization is displayed in the move details panel: base move + active DNA modifications + instability effects.

---

## Battle Scars

Near-death experiences leave permanent visible marks:
- Triggered when a creature survives at <10% HP in combat
- Each scar is unique — position, shape, and type tied to the damage source (burn scar from fire, frost scar from ice, claw marks from physical)
- Scars are **permanent** — cannot be removed. They're prestige, not punishment.
- Heavily scarred creatures are recognized as veterans by NPCs and rivals
- Scars visible in: battle, Creature Forge, Pokedex entry, async PvP
- A creature's scar history is listed in its Pokedex profile

---

## Permadeath Mode (Optional)

An official Nuzlocke-style difficulty mode:
- **Fainted = dead** — creature is permanently removed from your party
- **First encounter only** — can only attempt capture on the first creature encountered per area
- **Memorial Wall** — dead creatures displayed at the Research Station with their full history: level, DNA mods, parts, scars, battles fought, cause of death
- **Posthumous DNA extraction** — one final DNA extraction from a fallen creature (unique to permadeath mode). Their legacy lives on in your other creatures.
- **Memorial Pokedex section** — "In Memoriam" page tracking all lost creatures across all permadeath runs
- **Unlockable** — available after completing the first habitat zone in normal mode

---

## Post-MVP Features

- **Async PvP** — upload party, others fight your AI-controlled team
- **DNA Trading** — trade DNA modifications/materials between players
- **Leaderboards** — Pokedex completion %, most DNA mods discovered
- **Creature Photography** — snapshot mechanic for Pokedex entries (bonus RP for good shots)
- **Breeding** — combine two creatures for offspring with blended DNA traits

---

## Risks and Open Questions

### Design Risks

| Risk | Severity | Mitigation |
|------|----------|------------|
| DNA system too complex for onboarding | High | Progressive unlock — stat boosts first, then parts, then splicing. Verdant Basin teaches one layer at a time. |
| Instability mechanic feels punishing rather than exciting | Medium | Tune breakthrough bonus rate to reward risk-takers. Clear UI communication of risk vs. reward before committing. |
| Creature balance impossible with modular body parts | High | Part conflict system limits stacking. Signature parts incentivize staying close to species identity. Instability penalizes over-modification. |
| Progressive Pokedex feels grindy | Low | Research tiers unlock meaningful gameplay (DNA recipes, calls), not just lore. Each tier feels like a reward. |

### Technical Risks

| Risk | Severity | Mitigation |
|------|----------|------------|
| Procedural animation blending looks janky | High | MVP uses simple 3D pieces with minimal animation. Full animation blending is post-MVP after the system is proven. |
| Visible mutation rendering performance | Medium | LOD system for mutations. Limit simultaneous visible mutations on screen. Shader-based color/pattern changes are cheap. |
| Ecosystem simulation complexity | Medium | Simple predator-prey state machine for MVP. Full migration and population dynamics are post-MVP. |
| Height-variable isometric grid pathfinding | Medium | A* with height cost is well-understood. Limit grid sizes to keep pathfinding fast. |

### Market Risks

| Risk | Severity | Mitigation |
|------|----------|------------|
| "It's just another Pokemon clone" perception | High | Lead marketing with DNA engineering — the unique hook. Show visible mutations and custom creatures, not just battles. |
| Tactical RPG audience and creature-collection audience don't overlap | Medium | Both audiences value customization and strategy. The DNA system bridges the gap. Pokemon fans want more depth; XCOM fans want more personality. |
| Premium pricing in a genre dominated by free-to-play | Low | No gacha, no loot boxes, no FOMO. Position as the "anti-mobile" creature game. Content-complete at launch. |

### Scope Risks

| Risk | Severity | Mitigation |
|------|----------|------------|
| 53 systems is too many for any team size | High | Only 27 systems in MVP. Strict scope tiers. Cut aggressively if behind schedule. |
| Art pipeline for modular creatures is expensive | High | Simple 3D pieces for MVP. Modular mesh system designed for efficiency. Full 3D models only post-MVP after revenue. |
| Content volume (100+ creatures, 5 zones) | Medium | MVP is 1 zone, 20-30 creatures. Each zone is self-contained and can ship incrementally. |

### Open Questions

- [ ] How many creature archetypes (Bipedal, Quadruped, etc.) are needed for MVP vs. full game?
- [ ] Should instability have a hard cap, or can creatures theoretically reach 100 and become fully uncontrollable?
- [ ] How does the type chart scale? 6-8 types for MVP — is that enough for interesting DNA splicing?
- [ ] What is the right ratio of wild encounters to trainer battles per zone?
- [ ] Should research station upgrades be per-station or global? (Currently global — is that too generous?)
- [ ] How do we handle save file size with full ecosystem state + creature lineage trees?
- [ ] What is the monetization strategy for post-launch content? DLC zones? Cosmetic packs?

---

## MVP Definition

### Core Hypothesis

Players will find DNA splicing more engaging than traditional evolution because it offers creative expression, meaningful risk/reward decisions, and visible results that make every creature feel personally crafted.

### Required for MVP

- 1 habitat zone (Verdant Basin) with full encounter variety
- 20-30 creatures across 3-4 types
- 40-60 moves with type chart (6-8 types)
- Turn-based tactical grid combat (terrain synergy, height advantage)
- Basic DNA alteration (stat boosts, perk grafts — no visible mutations yet)
- Body part system (equip parts to slots, parts unlock moves)
- Gene Trap capture system with Catch Predictor UI
- Progressive Pokedex (4-tier research)
- Research station (Level 1-3 upgrades)
- 8-10 encounters + 1 rival trainer + 1 trophy battle
- Party system (4 creatures active)
- Save/load system
- Core UI (combat HUD, Pokedex, creature management, DNA modification)

### Explicitly NOT in MVP

- No visible mutations on 3D models (shader color changes only)
- No day/night cycle
- No weather zones
- No async PvP or multiplayer of any kind
- No creature arena
- No expedition mode
- No fossil system
- No creature calls
- No black market DNA
- No breeding
- No permadeath mode
- No procedural animation blending
- No sound mutations

### Scope Tiers

| Tier | Content | Features | Timeline |
|------|---------|----------|----------|
| **MVP** | 1 zone, 20-30 creatures, 40-60 moves, 6-8 types | Grid combat, basic DNA mods, capture, Pokedex, 1 rival, 1 trophy | Months 1-6 |
| **Vertical Slice** | MVP + visible mutations, weather zones | Body part visuals, instability effects, combo moves | Months 6-9 |
| **Alpha** | 3 zones, 60+ creatures, 12+ types | Day/night, ecosystem, fossil system, creature arena, station L4-5 | Months 9-14 |
| **Full Vision** | 5 zones, 100+ creatures, all 53 systems | Async PvP, breeding, expedition, permadeath, creature calls, black market | Months 14-20+ |

---

## Next Steps

- [ ] Finalize creature archetype list and body slot definitions for MVP species
- [ ] Design type chart (6-8 types) with effectiveness matrix and DNA interaction rules
- [ ] Create Verdant Basin zone layout with encounter placement and terrain map
- [ ] Define 20-30 MVP creature stat blocks, move pools, and DNA compatibility
- [ ] Prototype isometric grid combat in Unity 6 (movement, attack, terrain synergy)
- [ ] Build DNA modification UI prototype (stat boosts + part equipping)
- [ ] Design first rival trainer personality, team composition, and adaptation logic
- [ ] Write creature lore and Pokedex entries for MVP species
- [ ] Establish art pipeline for simple 3D creature pieces and modular parts
- [ ] Set up Unity project structure matching the directory spec
- [ ] Create sprint plan for MVP Month 1 deliverables
