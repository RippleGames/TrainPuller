using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Cinemachine;
using TemplateProject.Scripts.Runtime.Managers;
using TrainPuller.Scripts.Data;
using TrainPuller.Scripts.Runtime.Managers;
using UnityEngine;
using UnityEngine.AddressableAssets;
using UnityEngine.ResourceManagement.AsyncOperations;

namespace TemplateProject.Scripts.Utilities
{
    public class AddressablePrefabLoader : MonoBehaviour
    {
        [Header("Cached References")] 
        [SerializeField] private GameplayManager gameplayManager;
        [SerializeField] private InteractionManager interactionManager;
        [SerializeField] private GridManager gridManager;
        [SerializeField] private TimeManager timeManager;
        [SerializeField] private CinemachineVirtualCamera virtualCamera;
        private GameObject loadedPrefabInstance;

        [Header("Variables")] public string label = "Level";

#if UNITY_EDITOR
        [Header("Editor")] public Action<GameObject> callbackAction;
        public AsyncOperationHandle<GameObject>? currentHandle;
#endif

        private async void Start()
        {
            await Addressables.InitializeAsync().Task;
            var prefabAddress = $"Level_{LevelManager.instance.GetLevelIndex()}";
            LoadPrefab(prefabAddress);
            AssignLevelCount(label);

        }
        
        private void AssignLevelCount(string label)
        {
            Addressables.LoadResourceLocationsAsync(label).Completed += OnLocationsLoaded;
        }

        void OnLocationsLoaded(AsyncOperationHandle<IList<UnityEngine.ResourceManagement.ResourceLocations.IResourceLocation>> handle)
        {
            if (handle.Status == AsyncOperationStatus.Succeeded)
            {
                
                LevelManager.instance.SetTotalLevelCount(handle.Result.Count);
            }
            else
            {
                Debug.LogError("Addressables yüklenirken hata oluştu.");
            }
        }

        private void LoadPrefab(string prefabAddress)
        {
            Addressables.LoadAssetAsync<GameObject>(prefabAddress).Completed += OnPrefabLoaded;
        }

        private void OnPrefabLoaded(AsyncOperationHandle<GameObject> handle)
        {
            if (handle.Status == AsyncOperationStatus.Succeeded)
            {
                loadedPrefabInstance = Instantiate(handle.Result);
                if (loadedPrefabInstance.TryGetComponent(out LevelContainer levelContainer))
                {
                    levelContainer.InitializeVariables(interactionManager, gameplayManager, gridManager, timeManager, virtualCamera);
                }
                LevelManager.instance.SetLevelTMP(UIManager.instance.GetLevelTMP(), UIManager.instance.GetStartLevelTMP());
                HandleTransitions();

                Debug.Log($"Loaded and instantiated prefab: {handle.Result.name}");
            }
            else
            {
                Debug.LogError($"Failed to load prefab from Addressables group: {label}");
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
                loadedPrefabInstance = Instantiate(handle.Result);
                callbackAction?.Invoke(loadedPrefabInstance);
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
            //DOVirtual.DelayedCall(Random.Range(0f, 0.5f), () =>
            //{
            //    UIManager.instance.OpenTransition(null);
            //    DOVirtual.DelayedCall(Random.Range(0.5f, 1f), () =>
            //    {
          
            //    });
            //});


            UIManager.instance.CloseLoadingScreen();
            UIManager.instance.CloseTransition(() =>
            {
                TimeManager.instance.SetTimerTMP(UIManager.instance.GetTimerTMP(),
                    UIManager.instance.GetStartLevelTimeTMP());
                LevelManager.instance.SetLevelTMP(UIManager.instance.GetLevelTMP(), UIManager.instance.GetStartLevelTMP());
                // UIManager.instance.EnableSettingsButton();
                // UIManager.instance.OpenStartScreen();
            });
        }

        private void OnDestroy()
        {
            if (loadedPrefabInstance)
            {
                Addressables.ReleaseInstance(loadedPrefabInstance);
            }
        }

#if UNITY_EDITOR

        public GameObject ManualPrefabLoader(string prefabAddress, Action<GameObject> callback)
        {
            callbackAction = callback;
            LoadPrefabEditor(prefabAddress);
            return loadedPrefabInstance;
        }

        private void OnDisable()
        {
            if (loadedPrefabInstance)
            {
                Addressables.ReleaseInstance(loadedPrefabInstance);
            }
        }
#endif
    }
}