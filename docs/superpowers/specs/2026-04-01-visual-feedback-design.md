# Visual Feedback System Design

**Date:** 2026-04-01
**Scope:** Hit flash, stagger visual, death effect upgrade, attack arc indicator
**Approach:** Centralized VFX controller per entity (Approach B)

## References

- Hollow Knight, Hyper Light Drifter, Death's Door, Hades — white flash on hit, color tint + shake for stagger, sprite-based attack arcs

## Components

### EnemyVisualFeedback

Single `MonoBehaviour` on every enemy prefab. Grabs `SpriteRenderer` and `EnemyHealth` on `Awake`, subscribes to `OnHealthChanged`, `OnStagger`, `OnDeath`.

**Hit Flash (OnHealthChanged):**
- Swap sprite material to shared `WhiteFlash` material for 0.1s, then restore original
- Coroutine-based; new hit restarts the timer
- Skip if enemy is already dead

**Stagger Visual (OnStagger):**
- Tint sprite to muted yellow `Color(1f, 0.9f, 0.4f)` for stagger duration
- Rapid positional shake: oscillate transform +/-0.05 units at ~30Hz
- Restore original color and position on stagger end
- Hit flash takes priority: flash interrupts stagger tint briefly, stagger tint resumes after flash ends

**Death Effect (OnDeath):**
- Flash white for 0.15s (slightly longer than hit flash)
- Fade alpha from 1 to 0 over 0.5s
- Destroy gameObject when fade completes
- Cancels any active hit flash or stagger visual

**Effect priority:** Death > Hit Flash > Stagger Tint. Death cancels all others. Hit flash briefly overrides stagger tint.

### AttackVisualFeedback

`MonoBehaviour` on the Player prefab.

- Holds reference to a child `SpriteRenderer` ("SwingArc"), disabled by default
- `ShowSwing(Vector2 direction, float duration)` called by `PlayerCombat` when attack starts
- Enables swing sprite, rotates to face attack direction, positions at attack point offset
- Scales sprite to match attack's `range` from `AttackData`
- Disables sprite after attack duration

**Integration with PlayerCombat:**
- One line added where attack starts: call `ShowSwing()` with direction and attack timer duration
- No changes to hit detection, damage, or combo logic

## Assets

### WhiteFlash Material (`Assets/Materials/WhiteFlash.mat`)
- Unlit sprite shader that outputs solid white while preserving the sprite's alpha shape
- Shared by all enemies — single asset reference

### Slash Arc Sprite (`Assets/Sprites/Effects/SlashArc.png`)
- 64x64 PNG with transparency
- White slash arc, 90-degree curved swing trail, semi-transparent edges fading to nothing
- AI generation prompt: "A single white slash arc effect sprite, 90-degree curved swing trail, semi-transparent edges fading to nothing, black background, pixel art style, 64x64, top-down 2D game perspective, no other elements"
- Post-processing: remove black background, save as PNG with alpha

## Prefab Changes

- **Enemy prefabs** (Hollow, Knight, Wraith, Caster, Boss): add `EnemyVisualFeedback` component
- **Player prefab**: add child GameObject "SwingArc" with `SpriteRenderer` (disabled), add `AttackVisualFeedback` component referencing SwingArc

## Code Changes

### Remove from EnemyStateMachine
- Remove `DeathFade()` coroutine — death visuals now owned by `EnemyVisualFeedback`
- Dead state still disables collider and sets state, but no longer handles visual fade or destroy

### Add to PlayerCombat
- One call to `AttackVisualFeedback.ShowSwing()` at attack start

### Generator Updates
- `PrefabGenerator`: add `EnemyVisualFeedback` to enemy prefabs, add `AttackVisualFeedback` + SwingArc child to Player prefab
- Create `WhiteFlash` material asset in generator or dedicated material generator

## Testing

### EditMode Tests
- `EnemyVisualFeedback`: flash resets on repeated hits, death cancels other effects, stagger tint applies and restores

### PlayMode Tests
- Hit flash triggers on enemy damage
- Stagger shows tint + shake for correct duration
- Death plays flash then fade sequence, destroys gameObject
- Attack swing arc appears in correct direction, scales with range, disables after duration
