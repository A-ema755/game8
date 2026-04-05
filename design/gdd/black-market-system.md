# Black Market System

## 1. Overview

The Black Market System introduces an underground trader per habitat zone who offers ethically questionable DNA and combat stimulants. Black Market inventory includes stolen legendary DNA, experimental unstable parts, and temporary combat buffs. Purchases reduce Institute Rank and have story consequences — rivals acknowledge black market use, NPCs refuse services, and the story reacts. The system presents a tempting moral choice: power now versus future penalties.

## 2. Player Fantasy

Finding the hidden black market trader feels like discovering a forbidden secret. Buying stolen legendary DNA is exhilarating and risky. The story consequences (rank penalties, NPC reactions) create genuine stakes for the choice. A player might use black market strategically to overcome a tough opponent, then regret the rank loss when locked out of future content.

## 3. Detailed Rules

### 3.1 Black Market Locations & Traders

One trader per zone, hidden:
- **Verdant Basin:** Shady merchant in overgrown ruins
- **Ember Peaks:** Underground lava trader
- **Frozen Reach:** Icy cave black market
- **Storm Coast:** Smuggler's dock
- **Shadow Depths:** Deep cave trader

Traders are discovered passively (random encounter) or actively (player exploration).

### 3.2 BlackMarketInventory

```csharp
[System.Serializable]
public class BlackMarketStock
{
    public string traderId;
    public List<BlackMarketItem> currentInventory;
    public int inventoryRefreshTurns = 10;  // Rotates every 10 zone clears
    
    [System.Serializable]
    public class BlackMarketItem
    {
        public string itemId;
        public string displayName;
        public string description;
        public DnaRarity rarity;
        public int rpCost;
        public bool isLegendary;
        public bool isOneOfAKind;
        public InstituteRankPenalty penalty;  // -1 to -2 ranks
    }
}

[System.Serializable]
public class InstituteRankPenalty
{
    public int penaltyMagnitude;  // How many ranks lost (-1, -2)
    public string npcReactionString;  // "I know what you did..."
}
```

### 3.3 Inventory Categories

| Category | Example Items | Cost | Penalty |
|----------|----------------|------|---------|
| Stolen DNA | Legendary Fire DNA, rare Psychic perk | 400-600 RP | -1 rank |
| Experimental Mods | Unstable parts (high instability) | 500-700 RP | -1 rank |
| Combat Stimulants | +30% ATK for 1 battle, +50% SPD for 3 turns | 200-400 RP | -1 rank per use |
| One-of-a-Kind | Unique forbidden recipes | 1000 RP | -2 ranks |

### 3.4 Purchase Flow

```csharp
public void PurchaseBlackMarketItem(BlackMarketItem item, Player player)
{
    if (player.RP < item.rpCost)
    {
        Message("Insufficient RP");
        return;
    }
    
    player.RP -= item.rpCost;
    
    if (item.isOneOfAKind)
        currentInventory.Remove(item);  // One-time only
    
    // Apply rank penalty
    player.InstituteRank -= item.penalty.penaltyMagnitude;
    
    // Add DNA to inventory
    player.DNAInventory.Add(item);
    
    // Story consequence
    OnBlackMarketPurchase?.Invoke(item);
}
```

### 3.5 Story Consequences

**Rank Loss Effects:**
- Rank drops below requirement for Station Level 3 or 5 → features locked
- Rivals acknowledge: "Went dark market on me, huh?"
- Certain NPCs at research stations refuse dialogue: "I can't trust you anymore."
- High-rank exclusive encounters become unavailable
- Rank can be recovered by Pokedex completion and avoiding further black market use (+1 rank per 20 entries)

## 4. Formulas

### Rank Penalty per Purchase

```
rankPenalty = item.rarity switch:
  Legendary => -1 or -2 (one-of-a-kind),
  Rare => -1,
  Uncommon => 0 (cost only)

finalRank = Max(0, currentRank - rankPenalty)
```

### Inventory Rotation

```
refreshedEvery = 10 zone clears OR 30 minutes session time
newItems = randomSelect(allBlackMarketItems, 3-5)
```

### Combat Stimulant Duration

```
buffDuration = 1 battle (stimulant consumed)
OR 3 turns (temporary speed buff)
```

## 5. Edge Cases

| Scenario | Resolution |
|----------|-----------|
| Player reaches rank 0 (lowest) from black market use | Rank stays at 0; no further penalties; cannot recover easily |
| One-of-a-kind item purchased; player loads old save | Item is restored; no duplication (per-save inventory) |
| Player buys combat stimulant, then doesn't use it in battle | Stimulant remains in inventory for next battle |
| Rank drops below Level 3 requirement mid-session | Station features lock immediately; player cannot purchase upgrades |
| NPC refuses dialogue after rank loss; player completes Pokedex to recover | Dialogue unlocks again once rank requirement met |
| Black market trader not yet discovered in zone | Trader can still be found by exploration or random encounter |

## 6. Dependencies

| System | Relationship |
|--------|-------------|
| Institute Rank System | Rank penalties applied on purchase |
| DNA Alteration System | Black market DNA installed as modifications |
| Combat System | Combat stimulants affect turn-based combat |
| Pokedex System | Pokedex completion recovers lost rank |
| Campaign Map | Black market trader locations |
| NPC/Dialogue System | Story reactions to black market purchases |
| Save/Load System | Persists rank penalties and purchases |

## 7. Tuning Knobs

| Parameter | Default | Notes |
|-----------|---------|-------|
| `blackMarketRPCostMultiplier` | 1.5–2.0 | Costs are 1.5–2x normal DNA |
| `rankPenaltyRare` | -1 | Rank lost per Rare purchase |
| `rankPenaltyLegendary` | -2 | Rank lost per Legendary/one-of-a-kind |
| `inventoryRefreshZoneClearCount` | 10 | Zone clears before stock rotates |
| `combatStimulantDuration` | 1 battle | How long stimulant buff lasts |
| `rankRecoveryPerPokedexEntry` | 0.05 | Recover 1 rank per 20 entries |
| `oneOfAKindItemCount` | 1–2 | Unique items per trader per stock |

## 8. Acceptance Criteria

- [ ] Black market traders exist in each zone and can be discovered.
- [ ] Inventory displays 3-5 items per stock rotation.
- [ ] Purchasing reduces Institute Rank by correct amount.
- [ ] One-of-a-kind items disappear after purchase.
- [ ] Stolen DNA can be applied as modifications at research stations.
- [ ] Combat stimulants provide stat boosts for 1 battle or 3 turns.
- [ ] Rank loss locks Station Level 3 and 5 features.
- [ ] NPC dialogue changes to acknowledge black market use.
- [ ] Rivals comment on black market purchases.
- [ ] Rank can be recovered through Pokedex completion.
- [ ] Inventory rotates every 10 zone clears.
- [ ] Black market transactions persist through save/load.
