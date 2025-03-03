#if UNITY_EDITOR
using System.Collections.Generic;
using FluffyUnderware.Curvy;
using FluffyUnderware.Curvy.Generator;
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
        [SerializeField] private RoadSplineGenerator splineGenerator;

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
        private CurvySpline _curvyGenerator;

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
            var spline = newParentObject.AddComponent<CurvySpline>();
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
                    if (cell.stackData.gridColorType is LevelData.GridColorType.None
                        or LevelData.GridColorType.Close)
                    {
                        gridBaseScript.Init(null, cell.stackData.gridColorType is LevelData.GridColorType.Close,
                            x,
                            y);
                        continue;
                    }

                    // var stickman = PrefabUtility.InstantiatePrefab(stickmanPrefab.gameObject) as GameObject;
                    // if (!stickman) continue;
                    // stickman.transform.SetParent(gridBaseObj.transform);
                    // stickman.transform.localPosition = Vector3.zero;
                    // stickman.transform.localEulerAngles = Vector3.zero;
                    // var stickmanScript = stickman.GetComponent<Stickman>();
                    // stickmanScript.Init(cell.stackData.gridColorType, cell.stackData.isSecret,
                    //     cell.stackData.isReserved, gridBaseScript);

                    gridBaseScript = gridBaseObj.GetComponent<GridBase>();
                    // gridBaseScript.Init(stickmanScript, false, cell.x, cell.y);
                    gridBaseScript.Init(null, false, cell.x, cell.y);
                }
            }

            HandleAdjacentSet(gridBases);
            splineGenerator.GenerateSplines(gridBases,spaceModifier);
            //AddSplinePoints(spline, roadPositions);

            var currentGoals = SpawnLevelGoals(newParentObject.transform);

            levelContainer.Init(gridWidth, gridHeight, levelTime, gridBases, currentGoals, levelGoals);
            EditorUtility.SetDirty(levelContainer);
            _currentParentObject = newParentObject;
        }

        private List<Vector2> FindClosedPath(GridBase[,] gridBases)
        {
            List<Vector2> roadPath = new List<Vector2>();
            HashSet<Vector2Int> visited = new HashSet<Vector2Int>();
            Vector2Int start = FindStartPoint(gridBases);
            if (start == Vector2Int.one * -1) return roadPath; // No valid start

            Queue<Vector2Int> queue = new Queue<Vector2Int>();
            queue.Enqueue(start);
            Dictionary<Vector2Int, Vector2Int> parentMap = new Dictionary<Vector2Int, Vector2Int>();
            parentMap[start] = start;

            while (queue.Count > 0)
            {
                Vector2Int current = queue.Dequeue();
                if (visited.Contains(current)) continue;
            
                visited.Add(current);
                roadPath.Add((Vector2)current);

                foreach (Vector2Int neighbor in GetValidNeighbors(current, visited,gridBases))
                {
                    if (!parentMap.ContainsKey(neighbor))
                    {
                        parentMap[neighbor] = current;
                        queue.Enqueue(neighbor);
                    }
                }
            }
            return roadPath;
        }

        List<Vector2Int> GetOrderedNeighbors(Vector2Int pos, HashSet<Vector2Int> visited, GridBase[,] gridBases)
        {
            List<Vector2Int> neighbors = new List<Vector2Int>();
            Vector2Int[] directions = { Vector2Int.left, Vector2Int.down, Vector2Int.right, Vector2Int.up };

            foreach (var dir in directions)
            {
                Vector2Int next = pos + dir;
                if (IsValid(next, gridBases) && gridBases[next.x, next.y].isTrail && !visited.Contains(next) &&
                    AreDirectlyConnected(pos, next, gridBases))
                {
                    neighbors.Add(next);
                }
            }

            return neighbors;
        }


        Vector2Int FindStartPoint(GridBase[,] gridBases)
        {
            for (var y = 0; y < gridBases.GetLength(1); y++)
            for (var x = gridBases.GetLength(0) - 1; x >= 0; x--) // Start from top-right
                if (gridBases[x, y].isTrail)
                    return new Vector2Int(x, y);
            return Vector2Int.one * -1; // No road found
        }


        private Vector2Int GetNextRoad(Vector2Int current, Vector2Int previous, HashSet<Vector2Int> visited,
            GridBase[,] gridBases)
        {
            Vector2Int[] directions =
                { Vector2Int.left, Vector2Int.down, Vector2Int.right, Vector2Int.up }; // Right-hand rule traversal

            foreach (var dir in directions)
            {
                var next = current + dir;
                if (IsValid(next, gridBases) && gridBases[next.x, next.y].isTrail && next != previous &&
                    !visited.Contains(next))
                    return next;
            }

            return Vector2Int.one * -1;
        }

        List<Vector2Int> GetValidNeighbors(Vector2Int pos, HashSet<Vector2Int> visited, GridBase[,] gridBases)
        {
            List<Vector2Int> neighbors = new List<Vector2Int>();
            Vector2Int[] directions = { Vector2Int.left, Vector2Int.down, Vector2Int.right, Vector2Int.up };

            foreach (var dir in directions)
            {
                Vector2Int next = pos + dir;
                if (IsValid(next,gridBases) && gridBases[next.x, next.y].isTrail && !visited.Contains(next) && AreDirectlyConnected(pos, next,gridBases))
                {
                    neighbors.Add(next);
                }
            }
            return neighbors;
        }

        private bool IsValid(Vector2Int pos, GridBase[,] gridBases) => pos.x >= 0 && pos.y >= 0 &&
                                                                       pos.x < gridBases.GetLength(0) &&
                                                                       pos.y < gridBases.GetLength(1);

        bool AreDirectlyConnected(Vector2Int a, Vector2Int b, GridBase[,] gridBases)
        {
            // Ensure roads are directly connected in BOTH directions
            if (Mathf.Abs(a.x - b.x) + Mathf.Abs(a.y - b.y) != 1)
                return false;

            // Prevent indirect connections (e.g., skipping diagonally or linking distant points)
            return gridBases[a.x, a.y].connections.Contains(b) && gridBases[b.x, b.y].connections.Contains(a);
        }

        private void HandleSplineCreation(GridBase[,] gridBases)
        {
            for (var y = 0; y < _levelData.height; y++)
            {
                for (var x = 0; x < _levelData.width; x++)
                {
                    var cell = _levelData.GetGridCell(x, y);
                    var gridBaseScript = gridBases[x, y];
                    if (cell.stackData.gridColorType == LevelData.GridColorType.Trail)
                    {
                    }
                }
            }
        }

        private void AddSplinePoints(CurvySpline spline, List<Vector2> splinePoints)
        {
            foreach (var splinePoint in splinePoints)
            {
                spline.Add(transform.position + GridSpaceToWorldSpace((int)splinePoint.x, (int)splinePoint.y));
            }
        }


        private void HandleAdjacentSet(GridBase[,] gridBases)
        {
            for (var y = 0; y < _levelData.height; y++)
            {
                for (var x = 0; x < _levelData.width; x++)
                {
                    var cell = _levelData.GetGridCell(x, y);
                    var gridBaseScript = gridBases[x, y];
                    if (cell.stackData.gridColorType != LevelData.GridColorType.Trail) continue;
                    gridBaseScript.isTrail = true;
                    CheckForAdjacentCells(gridBases, _levelData, gridBaseScript);
                }
            }
        }

        private void CheckForAdjacentCells(GridBase[,] gridBases, LevelData levelData, GridBase cell)
        {
            var maxX = gridBases.GetLength(0) - 1;
            var maxY = gridBases.GetLength(1) - 1;
            var currentY = cell.GetYAxis();
            var currentX = cell.GetXAxis();
            var connectionCount = 0;
            if (currentY > 0)
            {
                var upperCell = gridBases[currentX, currentY - 1];
                var upperCellData = levelData.GetGridCell(currentX, currentY - 1);
                if (upperCellData.stackData.gridColorType == LevelData.GridColorType.Trail)
                {
                    cell.AddToAdjacent(upperCell);
                    connectionCount++;
                }
            }

            if (currentY < maxY)
            {
                var lowerCell = gridBases[currentX, currentY + 1];
                var lowerCellData = levelData.GetGridCell(currentX, currentY + 1);
                if (lowerCellData.stackData.gridColorType == LevelData.GridColorType.Trail)
                {
                    cell.AddToAdjacent(lowerCell);
                    connectionCount++;
                }
            }

            if (currentX > 0)
            {
                var leftCell = gridBases[currentX - 1, currentY];
                var leftCellData = levelData.GetGridCell(currentX - 1, currentY);
                if (leftCellData.stackData.gridColorType == LevelData.GridColorType.Trail)
                {
                    cell.AddToAdjacent(leftCell);
                    connectionCount++;
                }
            }

            if (currentX < maxX)
            {
                var rightCell = gridBases[currentX + 1, currentY];
                var rightCellData = levelData.GetGridCell(currentX + 1, currentY);
                if (rightCellData.stackData.gridColorType == LevelData.GridColorType.Trail)
                {
                    cell.AddToAdjacent(rightCell);
                    connectionCount++;
                }
            }

            if (connectionCount > 2)
            {
                // cell.gameObject.AddComponent<CurvyConnection>();
                cell.SetIsControlPoint(true);
            }
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