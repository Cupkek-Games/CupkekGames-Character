using UnityEngine;

namespace CupkekGames.Character
{
  /// <summary>
  /// Blinks at random intervals by driving <see cref="FaceController.Blink01"/>. Knows no shape
  /// names: the controller decides which channels close and whether the current expression
  /// allows it, so the blinker runs under every face.
  /// </summary>
  [RequireComponent(typeof(FaceController))]
  public class Blinker : MonoBehaviour
  {
    [Tooltip("Seconds between blinks, picked at random in this range.")]
    [SerializeField] private Vector2 _interval = new(4f, 8f);
    [SerializeField, Min(0.001f)] private float _closeSeconds = 0.1f;
    [SerializeField, Min(0f)] private float _closedSeconds = 0.06f;
    [SerializeField, Min(0.001f)] private float _openSeconds = 0.03f;

    private enum Phase
    {
      Waiting,
      Closing,
      Closed,
      Opening,
    }

    private FaceController _face;
    private Phase _phase;
    private float _phaseEnd;

    /// <summary>Blink now (or as soon as the current blink finishes).</summary>
    public void BlinkNow()
    {
      if (_phase == Phase.Waiting) _phaseEnd = Time.time;
    }

    private void Awake()
    {
      _face = GetComponent<FaceController>();
    }

    private void OnEnable()
    {
      Wait(Time.time);
    }

    private void OnDisable()
    {
      _face.Blink01 = 0f;
    }

    private void Update()
    {
      float now = Time.time;
      switch (_phase)
      {
        case Phase.Waiting:
          if (now >= _phaseEnd) Enter(Phase.Closing, now + _closeSeconds);
          break;
        case Phase.Closing:
          _face.Blink01 = 1f - Remaining(now, _closeSeconds);
          if (now >= _phaseEnd) Enter(Phase.Closed, now + _closedSeconds);
          break;
        case Phase.Closed:
          _face.Blink01 = 1f;
          if (now >= _phaseEnd) Enter(Phase.Opening, now + _openSeconds);
          break;
        case Phase.Opening:
          _face.Blink01 = Remaining(now, _openSeconds);
          if (now >= _phaseEnd)
          {
            _face.Blink01 = 0f;
            Wait(now);
          }

          break;
      }
    }

    private float Remaining(float now, float seconds) => Mathf.Clamp01((_phaseEnd - now) / seconds);

    private void Wait(float now)
    {
      float min = Mathf.Max(0f, Mathf.Min(_interval.x, _interval.y));
      float max = Mathf.Max(_interval.x, _interval.y);
      Enter(Phase.Waiting, now + Random.Range(min, max));
    }

    private void Enter(Phase phase, float end)
    {
      _phase = phase;
      _phaseEnd = end;
    }
  }
}
