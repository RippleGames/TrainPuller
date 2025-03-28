using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text.RegularExpressions;
using UnityEngine;
using ElephantSDK;

public class ABManager : MonoBehaviour
{
    public static ABManager Instance;

    public List<int> levelsList = new List<int>();
    [SerializeField] public List<Levels> groupedLevels = new List<Levels>();

    public string levelsStr;

    public int levelLoopStartLevel;
    public bool isShowDebugMenu;
    public bool isTutorialOn;
    public bool isRemoveAdsOn;
    public bool isRestartButtonOn;
    public float raycastRadius;
    public float loadingSceneDelayTime;
    public int commonLuckFactor;
    public int specificLuckFactor;
    public float popDelayTimeOnMultipleMatches;
    public float rotateTutorialWaitTime;

    private void Awake()
    {
        MakeSingleton();
        RefreshData();
        Application.targetFrameRate = 60;
        // Input.multiTouchEnabled = false;
    }

    private void MakeSingleton()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
        }
        else
        {
            Destroy(gameObject);
        }
    }

    public void RefreshData()
    {
    //     var remoteConfig = RemoteConfig.GetInstance();
    //     
    //     isRemoveAdsOn = remoteConfig.GetBool("isRemoveAdsOn", isRemoveAdsOn);
    //     isTutorialOn = remoteConfig.GetBool("isTutorialOn", isTutorialOn);
    //     isShowDebugMenu = remoteConfig.GetBool("isShowDebugMenu", isShowDebugMenu);
    //     loadingSceneDelayTime = remoteConfig.GetFloat("loadingSceneDelayTime", loadingSceneDelayTime);
    //     isRestartButtonOn = remoteConfig.GetBool("isRestartButtonOn", isRestartButtonOn);
    //     raycastRadius = remoteConfig.GetFloat("raycastRadius", raycastRadius);
    //     commonLuckFactor = remoteConfig.GetInt("commonLuckFactor", commonLuckFactor);
    //     specificLuckFactor = remoteConfig.GetInt("specificLuckFactor", specificLuckFactor);
    //     popDelayTimeOnMultipleMatches =
    //         remoteConfig.GetFloat("popDelayTimeOnMultipleMatches", popDelayTimeOnMultipleMatches);
    //     rotateTutorialWaitTime =
    //         remoteConfig.GetFloat("rotateTutorialWaitTime", rotateTutorialWaitTime);
    //     
    //     string[] levels = remoteConfig.Get("levels", this.levelsStr).Split(new char[] { ',' });
    //     
    //     
    //     // Updated Regex pattern to include an optional group of digits after the letters
    //     Regex pattern = new Regex(@"(\d+(?:_\d+)*)([a-z]*)(\d*)");
    //     
    //     for (int i = 0; i < levels.Length; i++)
    //     {
    //         string[] levelsSplit = levels[i].Split(new char[] { '_' });
    //     
    //         Levels levelsInstance = new Levels();
    //         List<int> levelValues = new List<int>();
    //         for (int h = 0; h < levelsSplit.Length; h++)
    //         {
    //             Match match = pattern.Match(levelsSplit[h]);
    //     
    //             int levelNumber = Convert.ToInt16(match.Groups[1].Value, new CultureInfo("en-US"));
    //     
    //             // Capture and store the difficulty strings if they exist
    //             // string difficulty = match.Groups[2].Value;
    //     
    //             levelsList.Add(levelNumber);
    //             levelValues.Add(levelNumber);
    //         }
    //     
    //         levelsInstance.levels = levelValues;
    //         groupedLevels.Add(levelsInstance);
    //     }
    //     
    //     levelLoopStartLevel = remoteConfig.GetInt("levelLoopStartLevel", levelLoopStartLevel);
    }

    [System.Serializable]
    public class Levels
    {
        [SerializeField] public List<int> levels = new List<int>();
    }
}