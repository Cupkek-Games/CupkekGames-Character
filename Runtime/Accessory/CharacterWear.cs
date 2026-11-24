using System;
using System.Collections.Generic;
using UnityEngine;
using CupkekGames.Holds;

namespace CupkekGames.Character
{
  /// <summary>
  /// What a character takes off, and when (a hat at camp, shoes in bed). Per character: named
  /// removable <b>parts</b> (each the objects it shows and hides) and <b>occasions</b> (each the parts
  /// that come off during it). The game says when an occasion starts and ends
  /// (<see cref="Enter"/>); a part can also come off by itself (<see cref="TakeOff"/>, a cutscene).
  /// <para>
  /// Counted holds, never a flag: a part is off while at least one hold on it is on and back once
  /// the last is let go, so a camp and a cutscene overlapping never put the hat back early. An
  /// occasion this character has no rule for is a real state (most keep everything on): it changes
  /// nothing. A part's objects must not also be <see cref="CharacterVisualAccessories"/> props: two
  /// owners would fight.
  /// </para>
  /// </summary>
  public class CharacterWear : MonoBehaviour
  {
    [Serializable]
    public class Part
    {
      [Tooltip("The part's name, as occasions list it (Hat).")]
      public string Name;
      [Tooltip("What shows while it is worn and hides while it is off (the hat's mesh).")]
      public List<GameObject> Objects = new List<GameObject>();
    }

    [Serializable]
    public class Occasion
    {
      [Tooltip("The occasion's name, as the game says it (Camp).")]
      public string Name;
      [Tooltip("The parts that come off during it.")]
      public List<string> Parts = new List<string>();
    }

    [SerializeField] private List<Part> _parts = new List<Part>();
    [SerializeField] private List<Occasion> _occasions = new List<Occasion>();

    private readonly Dictionary<string, HoldSet> _off = new Dictionary<string, HoldSet>();
    private readonly Dictionary<string, Part> _byName = new Dictionary<string, Part>();

    /// <summary>A part came off or went back on.</summary>
    public event Action Changed;

    public IReadOnlyList<Part> Parts => _parts;
    public IReadOnlyList<Occasion> Occasions => _occasions;

    private void Awake()
    {
      string problem = Problem();
      if (problem != null) throw new InvalidOperationException($"[CharacterWear] '{name}': {problem}");

      foreach (Part part in _parts)
      {
        HoldSet off = new HoldSet();
        string partName = part.Name;
        off.Changed += () => Apply(partName);
        _off.Add(part.Name, off);
        _byName.Add(part.Name, part);
      }
    }

    private void OnValidate()
    {
      string problem = Problem();
      if (problem != null) Debug.LogError($"[CharacterWear] '{name}': {problem}", this);
    }

    /// <summary>
    /// <paramref name="occasion"/> starts for this character: its parts come off until the handle
    /// is disposed. An occasion with no rule here returns a handle that does nothing.
    /// </summary>
    public IDisposable Enter(string occasion)
    {
      Occasion rule = _occasions.Find(o => o.Name == occasion);
      if (rule == null || rule.Parts.Count == 0) return new All(null);

      List<IDisposable> holds = new List<IDisposable>(rule.Parts.Count);
      foreach (string part in rule.Parts) holds.Add(TakeOff(part));
      return new All(holds);
    }

    /// <summary>Takes <paramref name="part"/> off until the handle is disposed. Throws for a part this character does not have.</summary>
    public IDisposable TakeOff(string part)
    {
      if (!_off.TryGetValue(part, out HoldSet off))
        throw new ArgumentException($"[CharacterWear] '{name}' has no part '{part}'.", nameof(part));
      return off.Hold();
    }

    /// <summary>Whether <paramref name="part"/> is on now. Throws for a part this character does not have.</summary>
    public bool IsWorn(string part)
    {
      if (!_off.TryGetValue(part, out HoldSet off))
        throw new ArgumentException($"[CharacterWear] '{name}' has no part '{part}'.", nameof(part));
      return !off.IsHeld;
    }

    // A part's objects follow its holds; a handle let go after the character went does nothing.
    private void Apply(string part)
    {
      if (this == null) return;
      bool worn = !_off[part].IsHeld;
      foreach (GameObject item in _byName[part].Objects)
      {
        if (item != null) item.SetActive(worn);
      }
      Changed?.Invoke();
    }

    // An authoring error, or null: a nameless or doubled part, a part with no objects, an occasion
    // naming a part this character lacks, a part object that is also a held prop.
    private string Problem()
    {
      HashSet<string> names = new HashSet<string>();
      foreach (Part part in _parts)
      {
        if (string.IsNullOrWhiteSpace(part.Name)) return "a part has no name.";
        if (!names.Add(part.Name)) return $"two parts are named '{part.Name}'.";
        if (part.Objects == null || part.Objects.Count == 0 || part.Objects.Exists(o => o == null))
          return $"part '{part.Name}' has no objects, or a missing one.";
      }

      foreach (Occasion occasion in _occasions)
      {
        if (string.IsNullOrWhiteSpace(occasion.Name)) return "an occasion has no name.";
        foreach (string part in occasion.Parts)
        {
          if (!names.Contains(part)) return $"occasion '{occasion.Name}' takes off '{part}', which is not a part.";
        }
      }

      CharacterVisualAccessories props = GetComponentInChildren<CharacterVisualAccessories>(true);
      if (props == null || props.PlayerEquipments == null) return null;
      foreach (Part part in _parts)
      {
        foreach (GameObject item in part.Objects)
        {
          if (props.PlayerEquipments.Exists(p => p.GameObject == item))
            return $"part '{part.Name}' lists '{item.name}', which is also a held prop (CharacterVisualAccessories): it would have two owners.";
        }
      }

      return null;
    }

    // Several holds let go together, once; none for an occasion with no rule here.
    private sealed class All : IDisposable
    {
      private List<IDisposable> _holds;

      public All(List<IDisposable> holds) => _holds = holds;

      public void Dispose()
      {
        List<IDisposable> holds = _holds;
        _holds = null;
        if (holds == null) return;
        foreach (IDisposable hold in holds) hold.Dispose();
      }
    }
  }
}
