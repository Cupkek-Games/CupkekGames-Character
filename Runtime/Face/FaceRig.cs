using System.Collections.Generic;
using UnityEngine;

namespace CupkekGames.Character
{
  /// <summary>
  /// The face's channels, bound by name across every mesh that carries them. A face split
  /// over many meshes (face, brows, lashes, teeth, tongue) answers to one channel name, and
  /// blendshape indices are looked up here at build time, so a re-export that reorders
  /// shapes changes nothing. A shape named <c>X_mid</c> is bound as the mid-path correction
  /// of channel <c>X</c> and is driven from X's weight, never directly.
  /// </summary>
  public sealed class FaceRig
  {
    public const string MidSuffix = "_mid";

    private const float WriteEpsilon = 0.001f;

    internal readonly struct Binding
    {
      public readonly SkinnedMeshRenderer Renderer;
      public readonly int Index;

      public Binding(SkinnedMeshRenderer renderer, int index)
      {
        Renderer = renderer;
        Index = index;
      }
    }

    internal sealed class Channel
    {
      public readonly string Name;
      public readonly List<Binding> Targets = new();
      public readonly List<Binding> Mids = new();

      // Last values this rig wrote; NaN = never written, so the first write always lands.
      public float Written = float.NaN;
      public float MidWritten = float.NaN;

      public Channel(string name) => Name = name;
    }

    private readonly List<SkinnedMeshRenderer> _renderers = new();
    private readonly Dictionary<string, Channel> _channels = new();
    private readonly List<string> _orphanMids = new();

    public IReadOnlyList<SkinnedMeshRenderer> Renderers => _renderers;
    public IEnumerable<string> ChannelNames => _channels.Keys;

    /// <summary><c>X_mid</c> shapes with no <c>X</c> on any mesh: an export bug.</summary>
    public IReadOnlyList<string> OrphanMidKeys => _orphanMids;

    public static FaceRig Build(IEnumerable<SkinnedMeshRenderer> renderers)
    {
      var rig = new FaceRig();
      var mids = new List<(string channel, Binding binding)>();

      foreach (SkinnedMeshRenderer renderer in renderers)
      {
        if (renderer == null || renderer.sharedMesh == null || renderer.sharedMesh.blendShapeCount == 0) continue;

        rig._renderers.Add(renderer);
        Mesh mesh = renderer.sharedMesh;
        for (int i = 0; i < mesh.blendShapeCount; i++)
        {
          string shape = mesh.GetBlendShapeName(i);
          if (shape.EndsWith(MidSuffix) && shape.Length > MidSuffix.Length)
          {
            mids.Add((shape.Substring(0, shape.Length - MidSuffix.Length), new Binding(renderer, i)));
            continue;
          }

          rig.GetOrAdd(shape).Targets.Add(new Binding(renderer, i));
        }
      }

      foreach ((string channel, Binding binding) in mids)
      {
        if (rig._channels.TryGetValue(channel, out Channel owner))
        {
          owner.Mids.Add(binding);
        }
        else if (!rig._orphanMids.Contains(channel + MidSuffix))
        {
          rig._orphanMids.Add(channel + MidSuffix);
        }
      }

      return rig;
    }

    public bool HasChannel(string name) => !string.IsNullOrEmpty(name) && _channels.ContainsKey(name);

    public bool HasMid(string name) => _channels.TryGetValue(name ?? string.Empty, out Channel channel) && channel.Mids.Count > 0;

    /// <summary>
    /// Weight of the <c>X_mid</c> correction for channel weight <paramref name="weight"/>:
    /// a triangle that is 0 at both ends of the path and 100 at its midpoint.
    /// </summary>
    public static float MidWeight(float weight)
    {
      float t = Mathf.Clamp01(weight / 100f);
      return 100f * (1f - Mathf.Abs(2f * t - 1f));
    }

    /// <summary>Reads the channel's current weight from its first mesh (0 if the rig lacks it).</summary>
    public float Read(string name)
    {
      if (!_channels.TryGetValue(name ?? string.Empty, out Channel channel) || channel.Targets.Count == 0) return 0f;
      Binding first = channel.Targets[0];
      return first.Renderer != null ? first.Renderer.GetBlendShapeWeight(first.Index) : 0f;
    }

    /// <summary>Forget the last written values, so the next write lands even if unchanged.</summary>
    public void Invalidate()
    {
      foreach (Channel channel in _channels.Values)
      {
        channel.Written = float.NaN;
        channel.MidWritten = float.NaN;
      }
    }

    internal bool TryGetChannel(string name, out Channel channel)
    {
      channel = null;
      return !string.IsNullOrEmpty(name) && _channels.TryGetValue(name, out channel);
    }

    /// <summary>Write a weight to every mesh carrying the channel, and its mid correction.</summary>
    internal void Write(Channel channel, float weight)
    {
      if (float.IsNaN(channel.Written) || Mathf.Abs(channel.Written - weight) > WriteEpsilon)
      {
        foreach (Binding target in channel.Targets)
        {
          if (target.Renderer != null) target.Renderer.SetBlendShapeWeight(target.Index, weight);
        }

        channel.Written = weight;
      }

      if (channel.Mids.Count == 0) return;

      float mid = MidWeight(weight);
      if (!float.IsNaN(channel.MidWritten) && Mathf.Abs(channel.MidWritten - mid) <= WriteEpsilon) return;

      foreach (Binding target in channel.Mids)
      {
        if (target.Renderer != null) target.Renderer.SetBlendShapeWeight(target.Index, mid);
      }

      channel.MidWritten = mid;
    }

    private Channel GetOrAdd(string name)
    {
      if (!_channels.TryGetValue(name, out Channel channel))
      {
        channel = new Channel(name);
        _channels.Add(name, channel);
      }

      return channel;
    }
  }
}
