using System;
using System.Collections.Generic;
using System.Threading;
using UnityEngine;
using CupkekGames.Holds;
using CupkekGames.Services;
using CupkekGames.TimeSystem;

namespace CupkekGames.Character
{
  /// <summary>
  /// The props a character holds (weapons, a pickaxe, a briefcase), each tagged with a role. One
  /// role's props show at a time.
  /// <para>
  /// What shows is decided, not last-written: a <b>resting</b> role, set by one owner
  /// (<see cref="EnableEquipment"/>, <see cref="EnableEquipments"/>, <see cref="DisableEquipments"/>),
  /// and counted holds over it from anyone: <see cref="Hide"/> hides every prop, <see cref="Show"/>
  /// shows a role's props instead. While any hide hold is on, nothing shows; else the newest show
  /// hold's role; else the resting role. Letting a hold go never puts back a prop another system
  /// put away (a cheer's hide ending used to bring back a briefcase the station had stowed).
  /// <see cref="HideFor"/> and <see cref="ShowFor"/> take a hold and let it go after a time.
  /// </para>
  /// </summary>
  public class CharacterVisualAccessories : MonoBehaviour
  {
    [SerializeField] private List<CharacterVisualAccessory> _equipments;
    public List<CharacterVisualAccessory> PlayerEquipments => _equipments;

    // The resting role; null: every prop put away.
    private string _resting = VisualAccessoryRoleKinds.DEFAULT;
    private readonly HoldSet _hidden = new HoldSet();
    // The show holds on, oldest first: the newest one's role shows.
    private readonly List<ShowHold> _shown = new List<ShowHold>();
    // Timed holds still running, let go when the character goes.
    private readonly List<CountdownTimeContext> _timers = new List<CountdownTimeContext>();
    private TimeManager _time;

    /// <summary>The role whose props show now; null when none does.</summary>
    public string Showing { get; private set; }

    private void Awake()
    {
      _hidden.Changed += Apply;
      Apply();
    }

    private void OnDestroy()
    {
      _hidden.Changed -= Apply;
      foreach (CountdownTimeContext timer in _timers) timer.Dispose();
      _timers.Clear();
    }

    // ── The resting role (one owner) ────────────────────────────────────────

    /// <summary>At rest the DEFAULT role's props show.</summary>
    public void EnableEquipments() => EnableEquipment(VisualAccessoryRoleKinds.DEFAULT);

    /// <summary>At rest every prop is put away.</summary>
    public void DisableEquipments()
    {
      _resting = null;
      Apply();
    }

    /// <summary>At rest <paramref name="role"/>'s props show.</summary>
    public void EnableEquipment(string role)
    {
      _resting = role;
      Apply();
    }

    // ── Holds over it (anyone) ──────────────────────────────────────────────

    /// <summary>Hides every prop until the handle is disposed (and every other hide hold is let go).</summary>
    public IDisposable Hide() => _hidden.Hold();

    /// <summary>Shows <paramref name="role"/>'s props instead of the resting role's until the handle is disposed.</summary>
    public IDisposable Show(string role)
    {
      ShowHold hold = new ShowHold(this, role);
      _shown.Add(hold);
      Apply();
      return hold;
    }

    /// <summary>Hides every prop for <paramref name="seconds"/> of game time, then lets go.</summary>
    public void HideFor(float seconds) => LetGoAfter(Hide(), seconds);

    /// <summary>Shows <paramref name="role"/>'s props for <paramref name="seconds"/> of game time, then lets go.</summary>
    public void ShowFor(string role, float seconds) => LetGoAfter(Show(role), seconds);

    private void LetGoAfter(IDisposable hold, float seconds)
    {
      if (_time == null)
      {
        // Only an edit-mode preview runs without a TimeManager; there the hold is let go at once.
        _time = ServiceLocator.Get<TimeManager>(silent: !Application.isPlaying);
        if (_time == null)
        {
          hold.Dispose();
          return;
        }
      }

      CountdownTimeContext timer = new CountdownTimeContext(_time.Global, seconds, 0, 0.1f, CancellationToken.None);
      timer.OnComplete += () =>
      {
        hold.Dispose();
        _timers.Remove(timer);
        timer.Dispose();
      };
      _timers.Add(timer);
      timer.Start();
    }

    private void Release(ShowHold hold)
    {
      if (!_shown.Remove(hold)) return;
      Apply();
    }

    // The props of the role that wins (see the class summary) show, every other prop hides.
    private void Apply()
    {
      if (this == null) return;
      string role = _hidden.IsHeld ? null : _shown.Count > 0 ? _shown[_shown.Count - 1].Role : _resting;
      Showing = role;
      if (_equipments == null) return;
      foreach (CharacterVisualAccessory accessory in _equipments)
      {
        if (accessory.GameObject != null) accessory.GameObject.SetActive(role != null && accessory.Role == role);
      }
    }

    private sealed class ShowHold : IDisposable
    {
      private CharacterVisualAccessories _owner;

      public ShowHold(CharacterVisualAccessories owner, string role)
      {
        _owner = owner;
        Role = role;
      }

      public string Role { get; }

      public void Dispose()
      {
        CharacterVisualAccessories owner = _owner;
        _owner = null;
        if (owner != null) owner.Release(this);
      }
    }
  }
}
