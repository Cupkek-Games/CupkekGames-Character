using UnityEngine;
using UnityEngine.Playables;

namespace CupkekGames.Character.Timeline
{
    public class ExpressionClip : PlayableAsset
    {
        public BlendShapeListSO TargetExpression;
        public float BlendDuration;

        public override Playable CreatePlayable(PlayableGraph graph, GameObject owner)
        {
            var playable = ScriptPlayable<ExpressionBehaviour>.Create(graph);

            ExpressionBehaviour expressionBehaviour = playable.GetBehaviour();
            expressionBehaviour.TargetExpression = TargetExpression;
            expressionBehaviour.BlendDuration = BlendDuration;

            return playable;
        }
    }
}
