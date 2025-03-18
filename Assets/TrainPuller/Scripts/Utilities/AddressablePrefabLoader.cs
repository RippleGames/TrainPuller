using System;
using System.Linq;
using DG.Tweening;
using TemplateProject.Scripts.Data;
using TemplateProject.Scripts.Runtime.Managers;
using TrainPuller.Scripts.Runtime.Managers;
using UnityEditor.AddressableAssets;
using UnityEngine;
using UnityEngine.AddressableAssets;
using UnityEngine.ResourceManagement.AsyncOperations;
using Random = UnityEngine.Random;

namespace TrainPuller.Scripts.Utilities
{
    public class AddressablePrefabLoader : MonoBehaviour
    {
        [Header("Cached References")] 
        [SerializeField] private InteractionManager interactionManager;
        [SerializeField] private GameplayManager gameplayManager;
        [SerializeField] private GridManager gridManager;
        [SerializeField] private TimeManager timeManager;
        private GameObject _loadedPrefabInstance;

        [Header("Variables")] public string levelGroupName = "LevelsGroup";

#if UNITY_EDITOR
        [Header("Editor")] public Action<GameObject> callbackAction;
        public AsyncOperationHandle<GameObject>? currentHandle;
#endif

        private void Start()
        {
            var prefabAddress = $"Level_{LevelManager.instance.GetLevelIndex()}";
            LoadPrefab(prefabAddress);
            AssignLevelCount();
        }

        private void AssignLevelCount()
        {
            var count =
                AddressableAssetSettingsDefaultObject.Settings.groups.FirstOrDefault(g => g.Name == levelGroupName)!
                    .entries.Count;
            LevelManager.instance.SetTotalLevelCount(count);
        }

        private void LoadPrefab(string prefabAddress)
        {
            Addressables.LoadAssetAsync<GameObject>(prefabAddress).Completed += OnPrefabLoaded;
        }

        private void OnPrefabLoaded(AsyncOperationHandle<GameObject> handle)
        {
            if (handle.Status == AsyncOperationStatus.Succeeded)
            {
                _loadedPrefabInstance = Instantiate(handle.Result);
                if (_loadedPrefabInstance.TryGetComponent(out LevelContainer levelContainer))
                {
                    levelContainer.InitializeVariables(interactionManager,gameplayManager, gridManager, timeManager);
                }

                HandleTransitions();

                Debug.Log($"Loaded and instantiated prefab: {handle.Result.name}");
            }
            else
            {
                Debug.LogError($"Failed to load prefab from Addressables group: {levelGroupName}");
            }
        }
#if UNITY_EDITOR
        private void LoadPrefabEditor(string prefabAddress)
        {
            if (currentHandle.HasValue && currentHandle.Value.IsValid())
            {
                currentHandle.Value.Completed -= OnPrefabLoadedEditor;
                currentHandle.Value.Release();
                Addressables.Release(currentHandle.Value);
            }

            var handle = Addressables.LoadAssetAsync<GameObject>(prefabAddress);
            handle.Completed += OnPrefabLoadedEditor;
            currentHandle = handle;
            
        }

        private void OnPrefabLoadedEditor(AsyncOperationHandle<GameObject> handle)
        {
            if (handle.Status == AsyncOperationStatus.Succeeded)
            {
                _loadedPrefabInstance = Instantiate(handle.Result);
                callbackAction?.Invoke(_loadedPrefabInstance);
                Debug.Log($"Loaded and instantiated prefab: {handle.Result.name}");
                handle.Release();
            }
            else
            {
                Debug.LogError($"Failed to load prefab from Addressables group.");
            }
        }

#endif

        private void HandleTransitions()
        {
            DOVirtual.DelayedCall(Random.Range(0f, 0.5f), () =>
            {
                UIManager.instance.OpenTransition(null);
                DOVirtual.DelayedCall(Random.Range(0.5f, 1f), () =>
                {
                    UIManager.instance.CloseLoadingScreen();
                    UIManager.instance.CloseTransition(() =>
                    {
                        TimeManager.instance.SetTimerTMP(UIManager.instance.GetTimerTMP(),
                            UIManager.instance.GetStartLevelTimeTMP());
                        LevelManager.instance.SetLevelTMP(UIManager.instance.GetLevelTMP(),UIManager.instance.GetStartLevelTMP());
                        UIManager.instance.EnableSettingsButton();
                        UIManager.instance.OpenStartScreen();
                    });
                });
            });
        }

        private void OnDestroy()
        {
            if (_loadedPrefabInstance)
            {
                Addressables.ReleaseInstance(_loadedPrefabInstance);
            }
        }

#if UNITY_EDITOR

        public GameObject ManualPrefabLoader(string prefabAddress, Action<GameObject> callback)
        {
            callbackAction = callback;
            LoadPrefabEditor(prefabAddress);
            return _loadedPrefabInstance;
        }

        private void OnDisable()
        {
            if (_loadedPrefabInstance)
            {
                Addressables.ReleaseInstance(_loadedPrefabInstance);
            }
        }
#endif
    }
}