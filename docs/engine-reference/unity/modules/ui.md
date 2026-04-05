# Unity 6.3 — UI Module Reference

**Last verified:** 2026-02-13
**Knowledge Gap:** Unity 6 UI Toolkit is production-ready for runtime UI

**Gene Forge context:** Gene Forge uses **UI Toolkit exclusively** for all runtime UI.
Do NOT scaffold UGUI Canvas workflows for new UI. UI Toolkit (UXML/USS/C#) is the
required approach. Key UI screens: Battle HUD (creature stats, ability bar, turn order),
DNA Lab (creature engineering), Team Builder, Main Menu, and Results screens.
Stats displayed: HP / ATK / DEF / SPD / ACC. Type icons shown on creature cards.

---

## Overview

Unity 6 UI systems:
- **UI Toolkit** (REQUIRED for Gene Forge): Modern, performant, HTML/CSS-like
- **UGUI (Canvas)**: Not used for new Gene Forge UI
- **IMGUI**: Editor tooling only, never runtime

---

## UI Toolkit (Gene Forge Standard)

### Setup UI Document

1. Create UXML: `Assets > Create > UI Toolkit > UI Document`
2. Create USS: `Assets > Create > UI Toolkit > StyleSheet`
3. Add to scene: `GameObject > UI Toolkit > UI Document`
4. Assign UXML to `UIDocument > Source Asset`

---

### UXML (UI Structure)

```xml
<!-- BattleHUD.uxml -->
<ui:UXML xmlns:ui="UnityEngine.UIElements">
    <ui:VisualElement name="hud-root" class="hud-container">

        <!-- Active creature stat panel -->
        <ui:VisualElement name="creature-stats-panel" class="stats-panel">
            <ui:Label name="creature-name" class="creature-name-label" />
            <ui:VisualElement name="type-icon" class="type-icon" />
            <ui:VisualElement name="hp-bar-container" class="stat-bar-container">
                <ui:Label text="HP" class="stat-label" />
                <ui:VisualElement name="hp-bar" class="stat-bar hp-bar" />
                <ui:Label name="hp-value" class="stat-value" />
            </ui:VisualElement>
        </ui:VisualElement>

        <!-- Ability bar -->
        <ui:VisualElement name="ability-bar" class="ability-bar">
            <ui:Button name="ability-slot-0" class="ability-slot" />
            <ui:Button name="ability-slot-1" class="ability-slot" />
            <ui:Button name="ability-slot-2" class="ability-slot" />
            <ui:Button name="ability-slot-3" class="ability-slot" />
        </ui:VisualElement>

        <!-- Turn order strip -->
        <ui:VisualElement name="turn-order-strip" class="turn-strip" />

        <!-- End turn button -->
        <ui:Button name="end-turn-button" text="End Turn" class="end-turn-btn" />

    </ui:VisualElement>
</ui:UXML>
```

---

### USS (Styling)

```css
/* BattleHUD.uss */
.hud-container {
    flex-direction: column;
    width: 100%;
    height: 100%;
}

.stats-panel {
    position: absolute;
    bottom: 120px;
    left: 20px;
    width: 280px;
    background-color: rgba(10, 10, 20, 0.85);
    border-radius: 8px;
    padding: 12px;
}

.creature-name-label {
    font-size: 20px;
    color: white;
    -unity-font-style: bold;
}

.stat-bar-container {
    flex-direction: row;
    align-items: center;
    margin-top: 4px;
}

.stat-label {
    width: 40px;
    color: rgb(180, 180, 180);
    font-size: 13px;
}

.stat-bar {
    height: 10px;
    flex-grow: 1;
    background-color: rgb(50, 50, 50);
    border-radius: 4px;
}

.hp-bar {
    background-color: rgb(80, 200, 80);
}

.stat-value {
    width: 50px;
    color: white;
    font-size: 13px;
    -unity-text-align: upper-right;
}

.ability-bar {
    position: absolute;
    bottom: 20px;
    left: 50%;
    translate: -50% 0;
    flex-direction: row;
}

.ability-slot {
    width: 72px;
    height: 72px;
    margin: 4px;
    background-color: rgba(20, 20, 40, 0.9);
    border-radius: 6px;
    border-width: 2px;
    border-color: rgb(80, 80, 120);
}

.ability-slot:hover {
    border-color: rgb(160, 200, 255);
}

.ability-slot:disabled {
    opacity: 0.4;
}

.end-turn-btn {
    position: absolute;
    bottom: 20px;
    right: 20px;
    width: 140px;
    height: 48px;
    font-size: 16px;
    background-color: rgb(60, 100, 60);
    color: white;
    border-radius: 6px;
}

.end-turn-btn:hover {
    background-color: rgb(80, 140, 80);
}
```

---

### C# Scripting (UI Toolkit)

```csharp
using UnityEngine;
using UnityEngine.UIElements;

public class BattleHUD : MonoBehaviour {
    private VisualElement root;
    private Label creatureNameLabel;
    private VisualElement hpBar;
    private Label hpValueLabel;
    private Button[] abilitySlots = new Button[4];
    private Button endTurnButton;

    void OnEnable() {
        root = GetComponent<UIDocument>().rootVisualElement;

        creatureNameLabel = root.Q<Label>("creature-name");
        hpBar             = root.Q<VisualElement>("hp-bar");
        hpValueLabel      = root.Q<Label>("hp-value");
        endTurnButton     = root.Q<Button>("end-turn-button");

        for (int i = 0; i < 4; i++) {
            int slotIndex = i;
            abilitySlots[i] = root.Q<Button>($"ability-slot-{i}");
            abilitySlots[i].clicked += () => abilitySystem.SelectSlot(slotIndex);
        }

        endTurnButton.clicked += () => turnManager.EndPlayerTurn();
    }

    // Called by CombatManager when active creature changes
    public void RefreshCreatureDisplay(Creature creature) {
        creatureNameLabel.text = creature.DisplayName;

        float hpPercent = (float)creature.Stats.HP / creature.Stats.MaxHP;
        hpBar.style.width = new StyleLength(new Length(hpPercent * 100f, LengthUnit.Percent));
        hpValueLabel.text = $"{creature.Stats.HP}/{creature.Stats.MaxHP}";

        // Update type icon class
        root.Q<VisualElement>("type-icon").ClearClassList();
        root.Q<VisualElement>("type-icon").AddToClassList($"type-{creature.Type.ToString().ToLower()}");
    }

    public void SetAbilitiesEnabled(bool enabled) {
        foreach (var slot in abilitySlots) {
            slot.SetEnabled(enabled);
        }
        endTurnButton.SetEnabled(enabled);
    }
}
```

---

### Creature Stat Card Pattern

```csharp
// Gene Forge: build creature card dynamically (DNA Lab, Team Builder)
VisualElement CreateCreatureCard(Creature creature) {
    var card = new VisualElement();
    card.AddToClassList("creature-card");

    var nameLabel = new Label(creature.DisplayName);
    nameLabel.AddToClassList("card-name");

    // 5-stat display: HP / ATK / DEF / SPD / ACC
    var statsGrid = new VisualElement();
    statsGrid.AddToClassList("stats-grid");
    foreach (var (statName, statVal) in creature.Stats.AllStats()) {
        var row = new VisualElement();
        row.Add(new Label(statName));
        row.Add(new Label(statVal.ToString()));
        statsGrid.Add(row);
    }

    card.Add(nameLabel);
    card.Add(statsGrid);
    return card;
}
```

---

### Dynamic UI Creation (No UXML)

```csharp
void CreateUI() {
    var root = GetComponent<UIDocument>().rootVisualElement;

    var container = new VisualElement();
    container.AddToClassList("container");

    var label = new Label("Gene Forge");
    var button = new Button(() => SceneManager.LoadScene("BattleScene")) { text = "Battle" };

    container.Add(label);
    container.Add(button);
    root.Add(container);
}
```

---

### USS Flexbox Layout

```css
/* Horizontal layout */
.horizontal { flex-direction: row; }

/* Vertical layout (default) */
.vertical { flex-direction: column; }

/* Center children */
.centered {
    align-items: center;
    justify-content: center;
}

/* Spacing */
.spaced { justify-content: space-between; }
```

---

## Common Patterns

### HP / Stat Bar Update

```csharp
// Gene Forge: update any stat bar by percent
void UpdateStatBar(VisualElement bar, int current, int max) {
    float pct = Mathf.Clamp01((float)current / max);
    bar.style.width = new StyleLength(new Length(pct * 100f, LengthUnit.Percent));
}
```

### Fade In / Out Panel

```csharp
IEnumerator FadeIn(VisualElement element, float duration) {
    float elapsed = 0f;
    element.style.display = DisplayStyle.Flex;
    while (elapsed < duration) {
        elapsed += Time.deltaTime;
        element.style.opacity = Mathf.Lerp(0f, 1f, elapsed / duration);
        yield return null;
    }
}

IEnumerator FadeOut(VisualElement element, float duration) {
    float elapsed = 0f;
    while (elapsed < duration) {
        elapsed += Time.deltaTime;
        element.style.opacity = Mathf.Lerp(1f, 0f, elapsed / duration);
        yield return null;
    }
    element.style.display = DisplayStyle.None;
}
```

### Show / Hide Panel

```csharp
void ShowPanel(VisualElement panel)  => panel.style.display = DisplayStyle.Flex;
void HidePanel(VisualElement panel)  => panel.style.display = DisplayStyle.None;
```

### Damage Number Popup (World-Space to Screen)

```csharp
// Gene Forge: show floating damage number at creature screen position
void ShowDamagePopup(Vector3 worldPos, int damage, bool isCrit) {
    Vector2 screenPos = isoCamera.WorldToScreenPoint(worldPos);
    // Convert to UI Toolkit panel coords
    Vector2 uiPos = RuntimePanelUtils.ScreenToPanel(
        hudDocument.rootVisualElement.panel,
        new Vector2(screenPos.x, Screen.height - screenPos.y)
    );

    var popup = new Label(isCrit ? $"!{damage}!" : damage.ToString());
    popup.AddToClassList(isCrit ? "damage-crit" : "damage-normal");
    popup.style.position = Position.Absolute;
    popup.style.left = uiPos.x;
    popup.style.top  = uiPos.y;
    hudDocument.rootVisualElement.Add(popup);

    StartCoroutine(AnimateAndRemovePopup(popup));
}
```

---

## Avoiding UGUI

```csharp
// ❌ Do NOT use in Gene Forge for new UI
// Canvas canvas = gameObject.AddComponent<Canvas>();
// canvas.renderMode = RenderMode.ScreenSpaceOverlay;
// GameObject.Find("Canvas").GetComponentInChildren<Text>();

// ✅ Use UI Toolkit
var root = GetComponent<UIDocument>().rootVisualElement;
var label = root.Q<Label>("my-label");
```

---

## Debugging

### UI Toolkit Debugger
- `Window > UI Toolkit > Debugger` — inspect element hierarchy, styles, layout

---

## Sources
- https://docs.unity3d.com/6000.0/Documentation/Manual/UIElements.html
- https://docs.unity3d.com/Packages/com.unity.ui@2.0/manual/index.html
