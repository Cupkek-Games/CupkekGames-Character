# CupkekGames Character

Humanoid character primitives shared across CupkekGames games.

## What's inside

**Runtime** (`CupkekGames.Character.asmdef`)

- `HumonoidCharacter` — root MonoBehaviour for a humanoid; binds the animation controller, the face and the gaze.
- `ModelCharacterSO` — ScriptableObject wrapper around an addressable prefab + name/avatar/description; consumed by `CharacterDefinition` on `UnitDefinitionSO`.
- `CharacterDefinition` — `IUnitFeatureDefinition` adding a `ModelCharacterSO` reference to any `UnitDefinitionSO`.
- `FaceController` — composes the face each frame from layers (expression crossfade with rest/timed play, blink, `X_mid` corrections) over a `FaceRig` that binds every child mesh with shape keys by name, plus channels other components add (`AddChannel`): a blush is a channel like any shape key, so expressions list it and it fades with them.
- `FaceFrame` — the face's own axes (from the eyes) in eye gaps, live: what anything placed on a face is measured in.
- `FaceOverlay` + `FaceOverlayLookSO` — the face's tints as channels (`blush`, `blushNose`, `blushLines`, `gloom`, `tears`), drawn as a second pass over the skin in face space: no mask texture, any face with eye bones.
- `FaceExpressionSO` (sparse weights, absent = 0, blink policy) + `FaceExpressionCatalog` / `FaceExpressions` resolver.
- `Blinker` (drives `FaceController.Blink01`, now and then a double blink) and `EyeAim` (parallel, clamped procedural eye aim).
- `Gaze` — where she looks and the life in her eyes: a look someone asked for, else who acts nearby, else her own saccade glances; a wide jump blinks; turns the head through `HeadLook` when a look asks for it.
- `AttentionSO` — who acts, as an asset (no statics): a system raises a cue on it (`Raise`) and every `Gaze` holding it may glance at the strongest nearby; a system can mute it while it runs (a fight).
- `HeadLook` — turns the head and part of the neck toward a point or a moving target after animation, on top of any clip (a seated pose that looks down can still lift its face); clamped, fading in and out, the face measured in the rest pose so any humanoid rig works.
- `CharacterVisualAccessories` — equipment slot manager that swaps active accessory GameObjects per role with auto-reset support.

**Editor** (`CupkekGames.Character.Editor.asmdef`)

- `FaceControllerEditor` — preview expressions, blink, inspect the rig, validate.
- `FaceRigValidator` — checks a character against the face contract (mesh/bone name clashes, avatar, non-zero rest weights, orphan `_mid` keys, blink channels, wiring, expression coverage).

## Dependencies

Asmdef references resolve via the CupkekGames scoped registry: `services`, `units`, `keyvaluedatabases`, `fadeables`, `addressableassets`, `pool`, `transforms`, `data`, `combat` (+ Unity built-ins). Bring your own copy via the registry.
