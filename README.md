# CupkekGames Character

Humanoid character primitives shared across CupkekGames games.

## What's inside

**Runtime** (`CupkekGames.Character.asmdef`)

- `HumonoidCharacter` — root MonoBehaviour for a humanoid; binds the animation controller, the face, eye movement, and emotes. `PlayExpression(key, hold)` resolves keys through `FaceExpressions`.
- `ModelCharacterSO` — ScriptableObject wrapper around an addressable prefab + name/avatar/description; consumed by `CharacterDefinition` on `UnitDefinitionSO`.
- `CharacterDefinition` — `IUnitFeatureDefinition` adding a `ModelCharacterSO` reference to any `UnitDefinitionSO`.
- `FaceController` — composes the face each frame from layers (expression crossfade with rest/timed play, blink, `X_mid` corrections) over a `FaceRig` that binds every child mesh with shape keys by name.
- `FaceExpressionSO` (sparse weights, absent = 0, blink policy) + `FaceExpressionCatalog` / `FaceExpressions` resolver; `EmoteVFXDatabase` for optional emote VFX.
- `Blinker` (drives `FaceController.Blink01`) and `EyeAim` (parallel, clamped procedural eye aim).
- `CharacterVisualAccessories` — equipment slot manager that swaps active accessory GameObjects per role with auto-reset support.

**Editor** (`CupkekGames.Character.Editor.asmdef`)

- `FaceControllerEditor` — preview expressions, blink, inspect the rig, validate.
- `FaceRigValidator` — checks a character against the face contract (mesh/bone name clashes, avatar, non-zero rest weights, orphan `_mid` keys, blink channels, wiring, expression coverage).

## Dependencies

Asmdef references resolve via the CupkekGames scoped registry: `services`, `units`, `keyvaluedatabases`, `fadeables`, `addressableassets`, `pool`, `transforms`, `data`, `combat` (+ Unity built-ins). Bring your own copy via the registry.
