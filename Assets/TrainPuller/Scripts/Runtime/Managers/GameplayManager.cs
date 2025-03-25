using System;
using System.Collections.Generic;
using DG.Tweening;
using TemplateProject.Scripts.Data;
using TemplateProject.Scripts.Data.Config;
using TemplateProject.Scripts.Runtime.Managers;
using TrainPuller.Scripts.Runtime.Models;

#if UNITY_EDITOR
using UnityEditor;
using UnityEditor.SceneManagement;
#endif
using UnityEngine;

namespace TrainPuller.Scripts.Runtime.Managers
{
    [DefaultExecutionOrder(-1)]
    public class GameplayManager : MonoBehaviour
    {
        public static GameplayManager instance;

        [Header("Cached References")] [SerializeField]
        private List<TrainMovement> allTrains;

        [SerializeField] private List<TrainMovement> competedTrains;
        [SerializeField] private GameConfig gameConfig;
        [SerializeField] private UIManager uiManager;

        [Header("Parameters")] [AudioClipName] public string levelFailSound;
        [AudioClipName] public string levelCompleteSound;

        [Header("Game Flags")] [SerializeField]
        private bool isAudioOn;

        [SerializeField] private bool isVibrationOn;
        private bool _initialSettingsSet = true;
        private bool _isInitialBusArrived;

        [Header("Actions")] public Action onBusChangeDone;
        public Action onGameLost;

#if UNITY_EDITOR

        private void OnEnable()
        {
            if (!LevelManager.instance) return;
            if (!LevelManager.instance.isTestScene) return;
            Application.logMessageReceived += HandleLog;
        }

        private void OnDisable()
        {
            if (!LevelManager.instance) return;
            if (!LevelManager.instance.isTestScene) return;
            Application.logMessageReceived -= HandleLog;
        }

        void HandleLog(string logString, string stackTrace, LogType type)
        {
            if (type == LogType.Error)
            {
                if (!LevelManager.instance.isTestScene) return;
                EditorApplication.isPlaying = false;
                EditorApplication.playModeStateChanged += GoToLevelCreator;
            }
        }
#endif

        private void Awake()
        {
            InitializeSingleton();
            HandleGameConfig();
        }

        private void InitializeSingleton()
        {
            if (instance) return;
            instance = this;
        }

        private void HandleGameConfig()
        {
            isAudioOn = gameConfig.isAudioOn == 1;
            isVibrationOn = gameConfig.isVibrationOn == 1;
            uiManager.HandleSwitches(isAudioOn, isVibrationOn);
            _initialSettingsSet = false;
        }


        public void RemoveTrain(TrainMovement trainMovement)
        {
            allTrains.Remove(trainMovement);
            competedTrains.Add(trainMovement);

            if (allTrains.Count <= 0)
            {
                WinGame();
            }
        }

        public bool GetVibration()
        {
            return isVibrationOn;
        }

        public bool GetAudio()
        {
            return isAudioOn;
        }

        public void ToggleVibration()
        {
            if (_initialSettingsSet) return;
            isVibrationOn = !isVibrationOn;
            SaveConfig();
        }

        public void ToggleAudio()
        {
            if (_initialSettingsSet) return;
            isAudioOn = !isAudioOn;
            AudioListener.volume = isAudioOn ? 1 : 0;
            SaveConfig();
        }

        private void SaveConfig()
        {
            gameConfig.Save(isAudioOn ? 1 : 0, isVibrationOn ? 1 : 0);
        }

        private void WinGame()
        {
            LevelManager.instance.isGamePlayable = false;
            uiManager.LevelCompleteEvents();
            TimeManager.instance.PauseTimer();

            if (VibrationManager.instance)
            {
                VibrationManager.instance.Win();
            }

            if (AudioManager.instance)
            {
                AudioManager.instance.PlaySound(levelCompleteSound);
            }

            DOVirtual.DelayedCall(3f, () =>
            {
#if UNITY_EDITOR
                if (LevelManager.instance.isTestScene)
                {
                    EditorApplication.isPlaying = false;
                    EditorApplication.playModeStateChanged += GoToLevelCreator;
                    return;
                }
#endif
                LevelManager.instance.LevelIncrease();
            });
        }

        public void LoseGame(bool isTimeLose)
        {
            if (!LevelManager.instance.isGamePlayable || LevelManager.instance.isLevelFailed) return;
            LevelManager.instance.isGamePlayable = false;
            LevelManager.instance.isLevelFailed = true;

            onGameLost?.Invoke();
            TimeManager.instance.PauseTimer();
#if UNITY_EDITOR
            if (LevelManager.instance.isTestScene)
            {
                EditorApplication.isPlaying = false;
                EditorApplication.playModeStateChanged += GoToLevelCreator;
                return;
            }
#endif

            DOVirtual.DelayedCall(1f, () =>
            {
                if (VibrationManager.instance)
                {
                    VibrationManager.instance.Fail();
                }

                if (AudioManager.instance)
                {
                    AudioManager.instance.PlaySound(levelFailSound);
                }

                if (isTimeLose)
                {
                    uiManager.SetTimeLost();
                }

                uiManager.OpenLoseScreen();
            });
        }
#if UNITY_EDITOR

        private void GoToLevelCreator(PlayModeStateChange obj)
        {
            if (obj == PlayModeStateChange.EnteredEditMode)
            {
                EditorSceneManager.OpenScene("Assets/TemplateProject/Scenes/LevelCreator.unity");
                EditorApplication.playModeStateChanged -= GoToLevelCreator;
            }
        }
#endif

        public void SetTrains(List<TrainMovement> trains)
        {
            allTrains = trains;
        }
    }
}