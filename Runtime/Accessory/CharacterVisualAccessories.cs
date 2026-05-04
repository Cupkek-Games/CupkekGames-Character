using UnityEngine;
using System.Collections.Generic;
using CupkekGames.TimeSystem;
using System.Threading;

namespace CupkekGames.Character
{
  public class CharacterVisualAccessories : MonoBehaviour
  {
    [SerializeField] private List<CharacterVisualAccessory> _equipments;
    public List<CharacterVisualAccessory> PlayerEquipments => _equipments;

    private CountdownTimeContext _resetCountdown;

    private void Awake()
    {
      EnableEquipment(VisualAccessoryRoleKinds.DEFAULT);
    }

    private void OnDestroy()
    {
      _resetCountdown?.Dispose();
    }

    public void EnableEquipments()
    {
      EnableEquipment(VisualAccessoryRoleKinds.DEFAULT);
    }

    public void DisableEquipments()
    {
      foreach (CharacterVisualAccessory accessory in _equipments)
      {
        accessory.GameObject.SetActive(false);
      }
    }

    public void EnableEquipment(string role)
    {
      foreach (CharacterVisualAccessory accessory in _equipments)
      {
        if (accessory.Role == role)
        {
          accessory.GameObject.SetActive(true);
        }
        else
        {
          accessory.GameObject.SetActive(false);
        }
      }
    }

    /// <summary>
    /// Enable equipment with auto-reset to DEFAULT after duration.
    /// If called again before duration ends, the timer resets.
    /// </summary>
    public void EnableEquipmentWithAutoReset(string role, float duration)
    {
      _resetCountdown?.Dispose();
      EnableEquipment(role);
      StartResetCountdown(duration);
    }

    /// <summary>
    /// Disable all equipment with auto-reset to DEFAULT after duration.
    /// If called again before duration ends, the timer resets.
    /// </summary>
    public void DisableEquipmentsWithAutoReset(float duration)
    {
      _resetCountdown?.Dispose();
      DisableEquipments();
      StartResetCountdown(duration);
    }

    private void StartResetCountdown(float duration)
    {
      TimeContext timeContext = TimeManager.Instance?.Global;
      if (timeContext == null)
      {
        return;
      }

      _resetCountdown = new CountdownTimeContext(timeContext, duration, 0, 0.1f, CancellationToken.None);
      _resetCountdown.OnComplete += OnResetComplete;
      _resetCountdown.Start();
    }

    private void OnResetComplete()
    {
      EnableEquipment(VisualAccessoryRoleKinds.DEFAULT);
      _resetCountdown?.Dispose();
      _resetCountdown = null;
    }
  }
}
