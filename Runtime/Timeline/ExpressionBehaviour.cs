using UnityEngine;
using UnityEngine.Playables;

namespace CupkekGames.Character.Timeline
{
    public class ExpressionBehaviour : PlayableBehaviour
    {
        public BlendShapeListSO TargetExpression;
        public float BlendDuration;

        public override void ProcessFrame(Playable playable, FrameData info, object playerData)
        {
            BlendShapeController blendShapeController = playerData as BlendShapeController;

            blendShapeController.TargetSO = TargetExpression;

            if (BlendDuration != 0 && Application.isPlaying)
            {
                blendShapeController.BlendToTarget(BlendDuration);
            }
            else
            {
                blendShapeController.ApplyValuesFromTarget();
            }
        }
    }
}
