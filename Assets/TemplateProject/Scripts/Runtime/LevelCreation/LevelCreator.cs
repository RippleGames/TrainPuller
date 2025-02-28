#if UNITY_EDITOR
using System.Collections.Generic;
using TemplateProject.Scripts.Data;
using TemplateProject.Scripts.Runtime.Models;
using TemplateProject.Scripts.Utilities;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace TemplateProject.Scripts.Runtime.LevelCreation
{
    public class LevelCreator : MonoBehaviour
    {
        [Header("Cached References")] [SerializeField]
        private GameObject gridBasePrefab;

        [SerializeField] private GameObject stickmanPrefab;
        [SerializeField] private GameObject goalPrefab;
        [SerializeField] private GameColors gameColors;
        [SerializeField] private AddressablePrefabSaver prefabSaver;
        [SerializeField] private AddressablePrefabLoader prefabLoader;
        [SerializeField] private TestConfig testConfig;
        private GameObject _currentParentObject;
        private GameObject _loadedLevel;

        [Header("Level Settings")] [HideInInspector]
        public int gridWidth;

        [HideInInspector] public int gridHeight;
        [HideInInspector] public int levelIndex;
        [SerializeField] public List<LevelGoal> levelGoals;
        [SerializeField] private int levelTime;
        [SerializeField] private LevelData.GridColorType colorTypes;
        public bool isSecretStickman;
        public bool isReservedStickman;

        [Header("Constant Variables")] [SerializeField]
        private float spaceModifier;

        private LevelData _levelData;

        public void GenerateLevel()
        {
            if (_levelData != null)
                return;

            _levelData = LevelSaveSystem.LoadLevel(levelIndex);
            if (_levelData != null) return;
            _levelData = new LevelData(gridWidth, gridHeight);
        }

        public void GridButtonAction(int x, int y)
        {
            _levelData.SetCellSettings(x, y, colorTypes, isSecretStickman, isReservedStickman);
        }

        public void GridRemoveButtonAction(int x, int y)
        {
            _levelData.RemoveCellSettings(x, y);
        }

        public void SaveLevel()
        {
            prefabSaver.SaveAndAssignPrefab(_currentParentObject, levelIndex);
            EditorUtility.SetDirty(prefabSaver);
            LevelSaveSystem.SaveLevel(_levelData, levelIndex);
        }

        public void LoadLevel()
        {
            _levelData = LevelSaveSystem.LoadLevel(levelIndex);
            if (_levelData == null) return;

            gridWidth = _levelData.width;
            gridHeight = _levelData.height;
            var prefabName = $"Level_{levelIndex}";
            
            _loadedLevel = GameObject.FindGameObjectWithTag("LevelParent");
            if (_loadedLevel)
            {
                DestroyImmediate(_loadedLevel);
            }
            var prevList = GameObject.FindGameObjectsWithTag("LevelParent");
            foreach (var oldLevels in prevList)
            {
                if (oldLevels)
                {
                    DestroyImmediate(oldLevels);
                }
            }

            if (_currentParentObject)
            {
                DestroyImmediate(_currentParentObject);
            }

            _loadedLevel = prefabLoader.ManualPrefabLoader(prefabName,
                (level) =>
                {
                    var container = level.GetComponent<LevelContainer>();
                    levelGoals = container.GetLevelGoals();
                    levelTime = container.GetLevelTime();
                    _loadedLevel = level;
                });
        }

        public void ResetLevel()
        {
            RemoveLevel();
            levelGoals.Clear();
            _levelData = new LevelData(gridWidth, gridHeight);
            prefabSaver.RemovePrefabFromAddressablesAndDelete(levelIndex);
        }


        private void RemoveLevel()
        {
            LevelSaveSystem.RemoveLevel(levelIndex);
        }

        public LevelData GetLevelData() => _levelData;

        public void SpawnGrid()
        {
            var gridBases = new GridBase[gridWidth, gridHeight];
            var oldParentObject = GameObject.FindGameObjectWithTag("LevelParent");
            if (oldParentObject)
            {
                DestroyImmediate(oldParentObject);
            }

            var newParentObject = new GameObject("Level_" + levelIndex);
            var levelContainer = newParentObject.AddComponent<LevelContainer>();
            newParentObject.transform.tag = "LevelParent";

            var gridParentObject = new GameObject("GridParent");
            gridParentObject.transform.SetParent(newParentObject.transform);

            for (var y = 0; y < _levelData.height; y++)
            {
                for (var x = 0; x < _levelData.width; x++)
                {
                    var cell = _levelData.GetGridCell(x, y);

                    var pos = transform.position + GridSpaceToWorldSpace(x, y);

                    var gridBaseObj = PrefabUtility.InstantiatePrefab(gridBasePrefab.gameObject) as GameObject;
                    if (!gridBaseObj) continue;
                    gridBaseObj.transform.SetParent(gridParentObject.transform);
                    gridBaseObj.transform.position = pos;
                    gridBaseObj.transform.localEulerAngles = new Vector3(0f, 180f, 0);

                    var gridBaseScript = gridBaseObj.GetComponent<GridBase>();
                    gridBases[x, y] = gridBaseScript;
                    if (cell.stackData.stickmanColorType is LevelData.GridColorType.None
                        or LevelData.GridColorType.Close)
                    {
                        gridBaseScript.Init(null, cell.stackData.stickmanColorType is LevelData.GridColorType.Close,
                            x,
                            y);
                        continue;
                    }

                    var stickman = PrefabUtility.InstantiatePrefab(stickmanPrefab.gameObject) as GameObject;
                    if (!stickman) continue;
                    stickman.transform.SetParent(gridBaseObj.transform);
                    stickman.transform.localPosition = Vector3.zero;
                    stickman.transform.localEulerAngles = Vector3.zero;
                    var stickmanScript = stickman.GetComponent<Stickman>();
                    stickmanScript.Init(cell.stackData.stickmanColorType, cell.stackData.isSecret,
                        cell.stackData.isReserved, gridBaseScript);

                    gridBaseScript = gridBaseObj.GetComponent<GridBase>();
                    gridBaseScript.Init(stickmanScript, false, cell.x, cell.y);
                }
            }

            var currentGoals = SpawnLevelGoals(newParentObject.transform);

            levelContainer.Init(gridWidth, gridHeight, levelTime, gridBases, currentGoals, levelGoals);
            EditorUtility.SetDirty(levelContainer);
            _currentParentObject = newParentObject;
        }

        private List<GoalScript> SpawnLevelGoals(Transform levelParent)
        {
            var goals = new List<GoalScript>();
            var goalParent = new GameObject("GoalParent");
            goalParent.transform.SetParent(levelParent);
            goalParent.transform.position = new Vector3(0, 0.60848f, -7.5f);
            var x = 5.75f;
            foreach (var levelGoal in levelGoals)
            {
                var goal = PrefabUtility.InstantiatePrefab(goalPrefab) as GameObject;
                if (!goal) continue;
                goal.transform.SetParent(goalParent.transform);
                goal.transform.localPosition = new Vector3(x, 0, 0);
                x += 5.75f;
                var goalScript = goal.GetComponent<GoalScript>();
                goalScript.Init(levelGoal.colorType, levelGoal.reservedCount);
                goals.Add(goalScript);
            }

            return goals;
        }

        private Vector3 GridSpaceToWorldSpace(int x, int y)
        {
            return new Vector3(x * spaceModifier * 0.9f - (gridWidth * spaceModifier * 0.9f / 2) + 0.575f,
                0, y * spaceModifier * 0.9f - 2f);
        }

        public GameColors GetGameColors()
        {
            return gameColors;
        }

        public void TestLevel()
        {
            testConfig.testLevelIndex = levelIndex;
            EditorUtility.SetDirty(this);
            EditorSceneManager.SaveOpenScenes();
            EditorSceneManager.OpenScene("Assets/TemplateProject/Scenes/TestScene.unity");
            EditorApplication.isPlaying = true;
        }
    }

    [System.Serializable]
    public class LevelGoal
    {
        public LevelData.GridColorType colorType;
        public int reservedCount;
    }
}
#endif