# CupkekGames Character

Humanoid character primitives shared across CupkekGames games.

## What's inside

**Runtime** (`CupkekGames.Character.asmdef`)

- `HumonoidCharacter` — root MonoBehaviour for a humanoid; binds a body controller, blend-shape controller, and accessory slots.
- `ModelCharacterSO` — ScriptableObject wrapper around an addressable prefab + name/avatar/description; consumed by `CharacterDefinition` on `UnitDefinitionSO`.
- `CharacterDefinition` — `IUnitFeatureDefinition` adding a `ModelCharacterSO` reference to any `UnitDefinitionSO`.
- `BlendShapeController` + `BlendShapeDatabase` + expression SOs — runtime blend-shape blending.
- `CharacterVisualAccessories` — equipment slot manager that swaps active accessory GameObjects per role with auto-reset support.

**Editor** (`CupkekGames.Character.Editor.asmdef`)

- `BlendShapeControllerEditor` — custom inspector for the blend-shape controller.

## Dependencies

Asmdef references resolve via the CupkekGames scoped registry: `services`, `units`, `keyvaluedatabases`, `fadeables`, `addressableassets`, `pool`, `transforms`, `data`, `combat` (+ Unity built-ins). Bring your own copy via the registry.
