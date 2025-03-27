using DG.Tweening;
using ElephantSDK;
using TemplateProject.Scripts.Data;
using TemplateProject.Scripts.Runtime.Managers;
using TMPro;
using TrainPuller.Scripts.Runtime.Models;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace TrainPuller.Scripts.Runtime.Managers
{
    [DefaultExecutionOrder(-1)]
    public class LevelManager : MonoBehaviour
    {
        public static LevelManager instance;

        [Header("CachedReferences")] [SerializeField]
        private TestConfig testConfig;

        [Header("Flags")] public bool isGamePlayable;
        public bool isLevelFailed;
        public bool isTestScene;

        [Header("Parameters")] [SerializeField]
        private int levelIndex;

        [SerializeField] private int totalLevelCount;
        [SerializeField] private int totalPlayedLevelCount;


        private void Awake()
        {
            HandleFPS();
            MakeSingleton();
            HandleSaveData();
        }

        private void HandleFPS()
        {
            if (Application.targetFrameRate != 120)
            {
                Application.targetFrameRate = 120;
            }

            DOTween.SetTweensCapacity(500, 50);
        }

        private void MakeSingleton()
        {
            if (instance == null)
            {
                instance = this;
            }
            else
            {
                Destroy(gameObject);
            }
        }

        private void HandleSaveData()
        {
            if (!isTestScene)
            {
                FetchPlayerPrefs();
            }
            else
            {
                levelIndex = testConfig.testLevelIndex;
                totalPlayedLevelCount = 0;
            }
        }

        private void FetchPlayerPrefs()
        {
            
            levelIndex = PlayerPrefs.GetInt("CurrentLevel", 0);
            totalPlayedLevelCount = PlayerPrefs.GetInt("TotalPlayedLevel", 1);
            Elephant.LevelStarted(levelIndex);
        }

        public void LevelIncrease()
        {
            Elephant.LevelCompleted(levelIndex);
            IncreaseLevelIndex();
            LoadLevel();
        }

        private void LoadLevel()
        {
            var asyncOperation = SceneManager.LoadSceneAsync(SceneManager.GetActiveScene().buildIndex);
            asyncOperation.allowSceneActivation = false;
            UIManager.instance.OpenTransition(() => { asyncOperation.allowSceneActivation = true; });
        }

        private void IncreaseLevelIndex()
        {
            levelIndex++;
            totalPlayedLevelCount++;
            if (levelIndex >= totalLevelCount)
            {
                levelIndex = 11;
            }

            PlayerPrefs.SetInt("CurrentLevel", levelIndex);
            PlayerPrefs.SetInt("TotalPlayedLevel", totalPlayedLevelCount);
        }

        public void DecreaseLevel()
        {
            levelIndex--;
            totalPlayedLevelCount--;
            if (levelIndex <= 0)
            {
                levelIndex = 0;
            }

            if (totalPlayedLevelCount <= 0)
            {
                totalPlayedLevelCount = 0;
            }

            PlayerPrefs.SetInt("CurrentLevel", levelIndex);
            PlayerPrefs.SetInt("TotalPlayedLevel", totalPlayedLevelCount);
            RestartLevel();
        }

        public void RestartLevel()
        {
            LoadLevel();
        }

        public int GetLevelIndex()
        {
            return levelIndex;
        }

        public void SetTotalLevelCount(int levelCount)
        {
            totalLevelCount = levelCount;
        }

        public void SetLevelTMP(TextMeshProUGUI levelTMP, TextMeshProUGUI startLevelTMP)
        {
            levelTMP.text = "Level " + totalPlayedLevelCount;
            startLevelTMP.text = "LEVEL " + totalPlayedLevelCount;
            UIManager.instance.OpenLevelText();
        }
    }
}