#if UNITY_ADDRESSABLES
using UnityEngine.AddressableAssets;
using CupkekGames.AddressableAssets;
using CupkekGames.SceneManagement;
using CupkekGames.Sequencer;
using CupkekGames.Services;
using CupkekGames.Settings;
using CupkekGames.GameSave;
#endif
using UnityEngine;
using Cysharp.Threading.Tasks;

namespace CupkekGames.Character
{
  [CreateAssetMenu(fileName = "ModelCharacterSO", menuName = "CupkekGames/Character/ModelCharacterSO")]
  public class ModelCharacterSO : ScriptableObject
  {
    [SerializeField] private string _name = "Name";
    [SerializeField] private string _description = "Description";
    [SerializeField] private Sprite _avatar;

#if UNITY_ADDRESSABLES
    [SerializeField] private AssetReference _modelPrefab;
    public AssetReference ModelPrefab => _modelPrefab;
    public bool HasModel => _modelPrefab != null && _modelPrefab.RuntimeKeyIsValid();
#else
    [SerializeField] private GameObject _modelPrefab;
    public GameObject ModelPrefab => _modelPrefab;
    public bool HasModel => _modelPrefab != null;
#endif

    public string Name => _name;
    public string Description => _description;
    public Sprite Avatar => _avatar;

#if UNITY_ADDRESSABLES
    public async UniTask<GameObject> SpawnUniqueAsync(Vector3 position, Quaternion rotation)
    {
      return await AddressableAssetManager.InstantiateAsync(_modelPrefab, position, rotation);
    }

    public GameObject SpawnUnique(Vector3 position, Quaternion rotation)
    {
      return AddressableAssetManager.InstantiateSync(_modelPrefab, position, rotation);
    }

    public void DestroyAllThenRelease()
    {
      AddressableAssetManager.DestroyAllThenRelease(_modelPrefab);
    }
#else
    public async UniTask<GameObject> SpawnUniqueAsync(Vector3 position, Quaternion rotation)
    {
      await UniTask.CompletedTask;
      return Object.Instantiate(_modelPrefab, position, rotation);
    }

    public GameObject SpawnUnique(Vector3 position, Quaternion rotation)
    {
      return Object.Instantiate(_modelPrefab, position, rotation);
    }

    public void DestroyAllThenRelease()
    {
      // No-op for direct references: objects are destroyed via Object.Destroy
    }
#endif
  }
}