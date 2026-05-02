using System;
using CupkekGames.Data;
using CupkekGames.Units;
using UnityEngine;

namespace CupkekGames.Character
{
    [Serializable]
    public class CharacterDefinition : IUnitFeatureDefinition
    {
        [SerializeField] private ModelCharacterSO _model;

        public ModelCharacterSO Model { get => _model; set => _model = value; }

        public IFeature CloneFeature() => this; // SO reference, no mutable state
    }
}
