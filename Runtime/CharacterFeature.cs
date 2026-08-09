using CupkekGames.Animations;
using CupkekGames.Units;
using UnityEngine;

namespace CupkekGames.Character
{
    /// <summary>
    /// Bundles all 3D character visual sub-systems for a <see cref="Unit"/>.
    /// Wraps: model spawn, animation engine, blend shapes, eye movement, accessories.
    /// These sub-systems always co-occur — if a unit has a 3D model, it has all of these.
    /// </summary>
    public class CharacterFeature : IUnitFeature
    {
        // Resolved references (set after spawn)
        private HumonoidCharacter _character;
        private UnitView _unitView;

        /// <summary>The spawned HumonoidCharacter (null until spawned).</summary>
        public HumonoidCharacter Character => _character;

        /// <summary>The UnitView on the spawned GameObject (null until spawned).</summary>
        public UnitView UnitView => _unitView;

        // Convenience accessors (delegate to HumonoidCharacter)
        public IAnimationEngine AnimationEngine => _character?.AnimationEngine;
        public IAnimationStateController AnimationController => _character?.AnimationController;
        public BlendShapeController BlendShapeController => _character?.BlendShapeController;
        public EyeMovement EyeMovement => _character?.EyeMovement;

        public void OnInitialize(Unit unit)
        {
            // Character is spawned externally (by pool manager or scene placement).
            // This feature is attached after spawn and wired via SetCharacter().
        }

        // Scene references never clone: the copy starts unbound and is rebound at
        // the next spawn via SetCharacter().
        public IUnitFeature CloneFeature() => new CharacterFeature();

        public void OnDispose(Unit unit)
        {
            _character = null;
            _unitView = null;
        }

        /// <summary>
        /// Wire the spawned 3D character to this feature.
        /// Called after the GameObject is instantiated/pooled.
        /// </summary>
        public void SetCharacter(GameObject spawnedObject)
        {
            if (spawnedObject == null) return;

            _character = spawnedObject.GetComponentInChildren<HumonoidCharacter>();
            _unitView = spawnedObject.GetComponent<UnitView>();

            if (_character == null)
                Debug.LogWarning($"[CharacterFeature] No HumonoidCharacter found on {spawnedObject.name}");
        }

        /// <summary>
        /// Wire from an existing HumonoidCharacter reference.
        /// </summary>
        public void SetCharacter(HumonoidCharacter character)
        {
            _character = character;
            if (character != null)
                _unitView = character.GetComponentInParent<UnitView>();
        }
    }
}
