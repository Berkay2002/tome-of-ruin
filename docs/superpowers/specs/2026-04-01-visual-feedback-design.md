# Visual Feedback System Design

**Date:** 2026-04-01
**Scope:** Hit flash, stagger visual, death effect upgrade, attack arc indicator
**Approach:** Centralized VFX controller per entity (Approach B)

## References

- Hollow Knight, Hyper Light Drifter, Death's Door, Hades — white flash on hit, color tint + shake for stagger, sprite-based attack arcs

## Components

### EnemyVisualFeedback

`[RequireComponent(typeof(SpriteRenderer))]`
`[RequireComponent(typeof(EnemyHealth))]`

Single `MonoBehaviour` on every enemy prefab. Caches `SpriteRenderer`, `EnemyHealth`, and the original `Material` reference on `Awake`. Subscribes to `OnHealthChanged`, `OnStagger`, `OnDeath`.

**Hit Flash (OnHealthChanged):**
- Handler signature: `(float currentHealth, float maxHealth)` — ignores both params, just triggers flash
- Swap sprite material to shared `WhiteFlash` material for `flashDuration` (default 0.1s), then restore cached original material
- Coroutine-based; new hit stops existing flash coroutine and restarts
- Skip if enemy is already dead (track `_isDead` bool)

**Stagger Visual (OnStagger):**
- `OnStagger` is parameterless — read stagger duration from `EnemyHealth.data.staggerDuration`
- Tint sprite to muted yellow `Color(1f, 0.9f, 0.4f)` for stagger duration
- Rapid positional shake: oscillate transform +/- `shakeIntensity` (default 0.05) units at ~30Hz
- Cache original local position on stagger start, restore on stagger end
- Hit flash takes priority: flash swaps material (overriding tint visually), stagger tint resumes when flash ends

**Death Effect (OnDeath):**
- Set `_isDead = true`, cancel any active flash or stagger coroutines
- Flash white for `deathFlashDuration` (default 0.15s)
- Fade alpha from 1 to 0 over `deathFadeDuration` (default 0.5s)
- Destroy gameObject when fade completes

**Effect priority:** Death > Hit Flash > Stagger Tint. Death cancels all others. Hit flash briefly overrides stagger tint via material swap.

**Inspector-exposed fields:**
```
[Header("Hit Flash")]
public float flashDuration = 0.1f;
public Material whiteFlashMaterial;

[Header("Stagger")]
public Color staggerTintColor = new Color(1f, 0.9f, 0.4f);
public float shakeIntensity = 0.05f;

[Header("Death")]
public float deathFlashDuration = 0.15f;
public float deathFadeDuration = 0.5f;
```

**Note:** Multiple `OnHealthChanged` subscribers coexist safely — `HealthBarUI` already subscribes to the same event and will continue working.

### EnemyVisualFeedback on Boss

The Boss prefab uses `BossController` (not `EnemyStateMachine`), but it still has `EnemyHealth` — so `EnemyVisualFeedback` works via the same events. Differences:

- **Hit flash and stagger:** Work identically (same `EnemyHealth` events)
- **Death:** `EnemyVisualFeedback` handles the visual fade + destroy. `BossController.OnBossDeath()` currently just disables itself — update it to also set `_rb.velocity = Vector2.zero` (already does this) but **remove `enabled = false`** since the visual feedback component now owns the destroy lifecycle. `BossController.OnBossDeath()` should only stop movement/attacks; `EnemyVisualFeedback` handles the visual death and eventual `Destroy(gameObject)`.

### AttackVisualFeedback

`MonoBehaviour` on the Player prefab. Loosely coupled — `PlayerCombat` calls it via `GetComponent<AttackVisualFeedback>()` with a null check, so the system works even if the component is missing.

- Holds a `[SerializeField]` reference to a child `SpriteRenderer` ("SwingArc"), disabled by default
- `ShowSwing(Vector2 direction, float duration)` called by `PlayerCombat` when attack starts
- Enables swing sprite, rotates to face attack direction, positions at attack point offset
- Scales sprite to match `PlayerCombat.attackRange` (passed as parameter or read via `GetComponent`). Note: range lives on `PlayerCombat`, not on `AttackData`
- Disables sprite after attack duration via coroutine

**Updated method signature:** `ShowSwing(Vector2 direction, float duration, float range)`

**Integration with PlayerCombat:**
- In `TryAttack()`, after `DealDamage()`: get `AttackVisualFeedback` component, null-check, call `ShowSwing(_controller.FacingDirection, _attackAnimTimer, attackRange)`
- No changes to hit detection, damage, or combo logic

## Assets

### WhiteFlash Material (`Assets/Materials/WhiteFlash.mat`)
- URP 2D project — use **Shader Graph** or the built-in `Sprites/Default` shader
- Approach: create a simple Shader Graph asset (`Assets/Shaders/WhiteFlash.shadergraph`) that samples the sprite texture alpha but outputs solid white for RGB. This preserves the sprite silhouette while flashing pure white.
- Alternative (simpler): use `Sprites/Default` with `MaterialPropertyBlock` to override `_Color` to white — but this tints rather than replaces, so dark sprites won't flash fully white. Shader Graph is preferred.
- The material is a shared asset — all enemies reference the same instance. Original material is cached per-instance in `Awake`.

### Slash Arc Sprite (`Assets/Sprites/Effects/SlashArc.png`)
- 64x64 PNG with transparency
- White slash arc, 90-degree curved swing trail, semi-transparent edges fading to nothing
- AI generation prompt: "A single white slash arc effect sprite, 90-degree curved swing trail, semi-transparent edges fading to nothing, black background, pixel art style, 64x64, top-down 2D game perspective, no other elements"
- Post-processing: remove black background, save as PNG with alpha

## Prefab Changes

- **Enemy prefabs** (Hollow, Knight, Wraith, Caster): add `EnemyVisualFeedback` component, assign `WhiteFlash` material
- **Boss prefab**: add `EnemyVisualFeedback` component, assign `WhiteFlash` material
- **Player prefab**: add child GameObject "SwingArc" with `SpriteRenderer` (disabled), add `AttackVisualFeedback` component with reference to SwingArc renderer

## Code Changes

### Remove from EnemyStateMachine
- Remove `DeathFade()` coroutine — death visuals now owned by `EnemyVisualFeedback`
- In `SetState(EnemyState.Dead)`: keep collider disable, keep `_rb.velocity = Vector2.zero`, remove `StartCoroutine(DeathFade())` call

### Update BossController
- `OnBossDeath()`: keep `_rb.velocity = Vector2.zero`, remove `enabled = false`. Death visual + destroy handled by `EnemyVisualFeedback`.

### Add to PlayerCombat
- In `TryAttack()`: call `GetComponent<AttackVisualFeedback>()?.ShowSwing(_controller.FacingDirection, _attackAnimTimer, attackRange)`

### Generator Updates
- `PrefabGenerator`: add `EnemyVisualFeedback` to all enemy prefabs (including Boss), add `AttackVisualFeedback` + SwingArc child to Player prefab, assign `WhiteFlash` material reference
- Create `WhiteFlash` Shader Graph + material in a material/shader generator step

## Testing

### PlayMode Tests (all visual feedback tests require coroutines)
- `EnemyVisualFeedback`: hit flash triggers on damage (material swaps to white, reverts after duration)
- `EnemyVisualFeedback`: rapid hits restart flash timer (material stays white, doesn't flicker)
- `EnemyVisualFeedback`: death cancels active flash and stagger, plays flash-then-fade sequence, destroys gameObject
- `EnemyVisualFeedback`: stagger applies tint color and restores original after duration
- `AttackVisualFeedback`: swing arc appears in correct direction, scales with range, disables after duration

### EditMode Tests (state logic only, no coroutines)
- Verify `EnemyVisualFeedback` caches original material on Awake
- Verify `AttackVisualFeedback` swing sprite starts disabled
