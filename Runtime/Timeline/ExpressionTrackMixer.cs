using UnityEngine;
using UnityEngine.Playables;

namespace CupkekGames.Character.Timeline
{
  public class ExpressionTrackMixer : PlayableBehaviour
  {
    private int lastApplied = -1;

    public override void ProcessFrame(Playable playable, FrameData info, object playerData)
    {
      BlendShapeController blendShapeController = playerData as BlendShapeController;

      if (!blendShapeController)
      {
        return;
      }

      int inputCount = playable.GetInputCount();
      for (int i = 0; i < inputCount; i++)
      {
        float inputWeight = playable.GetInputWeight(i);
        if (inputWeight > 0 && i != lastApplied)
        {
          ScriptPlayable<ExpressionBehaviour> inputPlayable = (ScriptPlayable<ExpressionBehaviour>)playable.GetInput(i);

          ExpressionBehaviour input = inputPlayable.GetBehaviour();

          blendShapeController.TargetSO = input.TargetExpression;

          if (input.BlendDuration != 0 || Application.isPlaying)
          {
            blendShapeController.BlendToTarget(input.BlendDuration);
          }
          else
          {
            blendShapeController.ApplyValuesFromTarget();
          }

          lastApplied = i;
          break;
        }
      }
    }
  }
}
