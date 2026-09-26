using UnityEngine;
using UnityEngine.Playables;

namespace CupkekGames.Character.Timeline
{
    public class ExpressionBehaviour : PlayableBehaviour
    {
        public FaceExpressionSO TargetExpression;
        public float BlendDuration;

        public override void ProcessFrame(Playable playable, FrameData info, object playerData)
        {
            FaceController face = playerData as FaceController;
            if (face == null || TargetExpression == null || face.Current == TargetExpression)
            {
                return;
            }

            if (Application.isPlaying && BlendDuration > 0f)
            {
                face.Play(TargetExpression, 0f, BlendDuration);
            }
            else
            {
                face.Snap(TargetExpression);
            }
        }
    }
}
