#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.Linq;
using FluffyUnderware.Curvy;
using FluffyUnderware.Curvy.Controllers;
using TemplateProject.Scripts.Data;
using TemplateProject.Scripts.Runtime.LevelCreation;
using TemplateProject.Scripts.Runtime.Models;
using TemplateProject.Scripts.Utilities;
using TrainPuller.Scripts.Runtime.Models;
using TrainPuller.Scripts.Utilities;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace TrainPuller.Scripts.Runtime.LevelCreation
{
    public class LevelCreator : MonoBehaviour
    {
        [Header("Cached References")] [SerializeField]
        private GameObject gridBasePrefab;

        [SerializeField] private GameObject trainCartPrefab;
        [SerializeField] private GameObject cardBasePrefab;
        [SerializeField] private GameObject cardPrefab;
        [SerializeField] private GameObject exitPrefab;
        [SerializeField] private GameObject goalPrefab;
        [SerializeField] private GameColors gameColors;
        [SerializeField] private AddressablePrefabSaver prefabSaver;
        [SerializeField] private AddressablePrefabLoader prefabLoader;
        [SerializeField] private TestConfig testConfig;
        private GameObject _currentParentObject;
        private GameObject _loadedLevel;
        [SerializeField] private RoadSplineGenerator splineGenerator;
        [SerializeField] private GameObject roadPrefabForward;
        [SerializeField] private GameObject roadPrefabRight;
        [SerializeField] private GameObject roadPrefabLeft;
        [SerializeField] private GameObject roadPrefabTri;
        [SerializeField] private GameObject roadPrefabFour;

        [Header("Level Settings")] [HideInInspector]
        public int gridWidth;

        [HideInInspector] public int gridHeight;
        [HideInInspector] public int levelIndex;
        [SerializeField] public List<LevelGoal> levelGoals;
        [SerializeField] private int levelTime;
        [SerializeField] private LevelData.GridColorType colorTypes;
        public bool isExit;

        [Header("Constant Variables")] [SerializeField]
        private float spaceModifier;

        private LevelData _levelData;
        private CurvySpline _curvyGenerator;
        public GridBase[,] gridBases;

        public void GenerateLevel()
        {
            if (_levelData != null)
                return;

            _levelData = LevelSaveSystem.LoadLevel(levelIndex);
            if (_levelData != null) return;
            _levelData = new LevelData(gridWidth, gridHeight);
        }

        public void GridButtonAction(int x, int y, int index)
        {
            if (_levelData == null) return;
            _levelData.SetCellColor(x, y, colorTypes, isExit, index);
            EditorUtility.SetDirty(this);
        }


        public void GridRemoveButtonAction(int x, int y, int index)
        {
            if (_levelData == null) return;
            _levelData.RemoveCellColor(x, y, index);
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
            gridBases = new GridBase[gridWidth, gridHeight];
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

                    if (cell.stackData.colorTypes == null)
                    {
                        cell.stackData.colorTypes = new List<LevelData.GridColorType>();
                    }

                    var pos = transform.position + GridSpaceToWorldSpace(x, y);

                    var gridBaseObj = PrefabUtility.InstantiatePrefab(gridBasePrefab.gameObject) as GameObject;
                    if (!gridBaseObj) continue;
                    gridBaseObj.transform.SetParent(gridParentObject.transform);
                    gridBaseObj.transform.position = pos;
                    gridBaseObj.transform.localEulerAngles = new Vector3(0f, 180f, 0);

                    var gridBaseScript = gridBaseObj.GetComponent<GridBase>();
                    gridBases[x, y] = gridBaseScript;

                    if (cell.stackData.colorTypes.Contains(LevelData.GridColorType.Trail))
                    {
                        gridBaseScript.Init(null, false, x, y);
                    }
                    else
                    {
                        gridBaseScript.Init(null, false, x, y);
                    }
                }
            }

            HandleAdjacentSet(gridBases);
            // var splineParent = new GameObject("Spline Parent");
            // splineParent.transform.SetParent(newParentObject.transform);
            // var splines = splineGenerator.GenerateSplines(gridBases, splineParent.transform);
            var trailParent = new GameObject("Trail Parent");
            trailParent.transform.SetParent(newParentObject.transform);
            HandleRoadPrefabs(gridBases, trailParent.transform);
            var cardParent = new GameObject("Card Parent");
            cardParent.transform.SetParent(newParentObject.transform);
            var exitParent = new GameObject("Exit Parent");
            exitParent.transform.SetParent(newParentObject.transform);
            HandleTrainsAndCards(gridBases, cardParent.transform, exitParent.transform);
            var currentGoals = SpawnLevelGoals(newParentObject.transform);

            levelContainer.Init(gridWidth, gridHeight, levelTime, gridBases, currentGoals, levelGoals);
            EditorUtility.SetDirty(levelContainer);
            _currentParentObject = newParentObject;
        }

        private void HandleTrainsAndCards(GridBase[,] gridBases, Transform cardParent,
            Transform exitParent)
        {
            var trainParentList = new List<TrainMovement>();
            for (var i = 0; i < gridBases.GetLength(0); i++)
            {
                for (var j = 0; j < gridBases.GetLength(1); j++)
                {
                    var stackDataColorTypes = _levelData.GetGridCell(i, j).stackData.colorTypes;

                    if (stackDataColorTypes.Contains(LevelData.GridColorType.Trail) &&
                        !_levelData.GetGridCell(i, j).isExit)
                    {
                        stackDataColorTypes = stackDataColorTypes.Distinct().ToList();
                        if (stackDataColorTypes.Count > 1)
                        {
                            var trainMovement =
                                trainParentList.FirstOrDefault(x => x.cartsColor == stackDataColorTypes[1]);
                            var trainParent = trainMovement?.gameObject;
                            if (!trainParent)
                            {
                                trainParent = new GameObject("Train Parent");
                                trainParent.transform.SetParent(cardParent.parent.transform);
                                trainMovement = trainParent.AddComponent<TrainMovement>();
                                trainMovement.speed = 30f;
                                trainMovement.cartSpacing = 1;
                                trainMovement.cartsColor = stackDataColorTypes[1];
                                trainParentList.Add(trainMovement);
                            }

                            var trainCart = Instantiate(trainCartPrefab, gridBases[i, j].transform.position,
                                Quaternion.identity);
                            trainCart.transform.SetParent(trainParent.transform);
                            var cartScript = trainCart.GetComponent<CartScript>();
                            cartScript.SetCartProperties(i, j, stackDataColorTypes[1]);
                            // var closestSpline = FindClosestSpline(gridBases[i, j].transform.position, splines);
                            // if (!closestSpline)
                            // {
                            //     Debug.LogError("No spline found!");
                            //     return;
                            // }

                            // var closestTF =
                            //     closestSpline.GetNearestPointTF(trainController.transform.position, Space.World);
                            // trainController.Spline = closestSpline;
                            // trainController.RelativePosition = closestTF;
                            if (!trainMovement.carts.Contains(cartScript))
                            {
                                trainMovement.carts.Add(cartScript);
                                cartScript.SetTrainMovementScript(trainMovement);
                            }
                        }
                    }
                    else if (stackDataColorTypes.Count >= 1 &&
                             !stackDataColorTypes.Contains(LevelData.GridColorType.None) &&
                             !_levelData.GetGridCell(i, j).isExit)
                    {
                        var cardBase = Instantiate(cardBasePrefab, gridBases[i, j].transform.position,
                            Quaternion.identity);
                        cardBase.transform.SetParent(cardParent);

                        var yOffset = 0.05f;
                        var yRotation = 0f;
                        foreach (var colorType in stackDataColorTypes)
                        {
                            for (var k = 0; k < 3; k++)
                            {
                                var card = Instantiate(cardPrefab,
                                    cardBase.transform.position + new Vector3(0f, yOffset, 0f),
                                    Quaternion.identity);
                                card.transform.eulerAngles = new Vector3(0f, yRotation, 0f);
                                card.transform.SetParent(cardBase.transform);
                                var cardScript = card.GetComponent<CardScript>();
                                cardScript.SetCardProperties(colorType);
                                yOffset += 0.05f;
                                yRotation += 90f;
                            }
                        }
                    }
                    else if (_levelData.GetGridCell(i, j).isExit)
                    {
                        var exitBarrier = Instantiate(exitPrefab, gridBases[i, j].transform.position,
                            Quaternion.identity).GetComponent<ExitBarrierScript>();
                        exitBarrier.transform.SetParent(exitParent);
                        exitBarrier.SetBarrierProperties(stackDataColorTypes[^1]);
                        if (i == gridWidth - 1)
                        {
                            exitBarrier.transform.position += new Vector3(0.3f, 0, 0);
                        }
                        else if (i == 0)
                        {
                            exitBarrier.transform.position -= new Vector3(0.3f, 0, 0);
                        }
                        else if (j == gridHeight - 1)
                        {
                            exitBarrier.transform.position += new Vector3(0f, 0f, 0.3f);
                            exitBarrier.transform.eulerAngles += new Vector3(0f, 90f, 0f);
                        }
                        else if (j == 0)
                        {
                            exitBarrier.transform.position -= new Vector3(0f, 0f, 0.3f);
                            exitBarrier.transform.eulerAngles += new Vector3(0f, 90f, 0f);
                        }
                    }
                }
            }
        }

        private CurvySpline FindClosestSpline(Vector3 position, List<CurvySpline> splines)
        {
            CurvySpline closestSpline = null;
            var minDistance = Mathf.Infinity;

            foreach (var spline in splines)
            {
                var closestPoint = spline.GetNearestPoint(position, Space.World);
                var distance = Vector3.Distance(position, closestPoint);

                if (!(distance < minDistance)) continue;
                minDistance = distance;
                closestSpline = spline;
            }

            return closestSpline;
        }


        private void HandleRoadPrefabs(GridBase[,] gridBases, Transform parent)
        {
            var forwardsDone = false;
            for (var x = 0; x < 2; x++)
            {
                for (var i = 0; i < gridBases.GetLength(0); i++)
                {
                    for (var j = 0; j < gridBases.GetLength(1); j++)
                    {
                        if (gridBases[i, j].isTrail)
                        {
                            var neighbors = gridBases[i, j].GetNeighbors();
                            if (!forwardsDone)
                            {
                                if (neighbors.Count == 2)
                                {
                                    if (neighbors[0].GetXAxis() == neighbors[1].GetXAxis())
                                    {
                                        var trailObject = Instantiate(roadPrefabForward,
                                            gridBases[i, j].transform.position,
                                            Quaternion.identity);
                                        trailObject.transform.eulerAngles = new Vector3(0, 90f, 0);
                                        trailObject.transform.SetParent(parent);
                                        gridBases[i, j].SetRoadPrefab(trailObject, true);
                                    }
                                    else if (neighbors[0].GetYAxis() == neighbors[1].GetYAxis())
                                    {
                                        var trailObject = Instantiate(roadPrefabForward,
                                            gridBases[i, j].transform.position,
                                            Quaternion.identity);
                                        trailObject.transform.SetParent(parent);
                                        gridBases[i, j].SetRoadPrefab(trailObject, true);
                                    }
                                }

                                continue;
                            }

                            if (neighbors.Count == 2)
                            {
                                if (neighbors[0].GetXAxis() < i || neighbors[1].GetXAxis() < i)
                                {
                                    if ((neighbors[0].GetYAxis() == j && neighbors[1].GetYAxis() < j) ||
                                        (neighbors[0].GetYAxis() < j && neighbors[1].GetYAxis() == j))
                                    {
                                        var trailObject = Instantiate(roadPrefabLeft,
                                            gridBases[i, j].transform.position,
                                            Quaternion.identity);
                                        trailObject.transform.eulerAngles = new Vector3(0, 180f, 0);
                                        trailObject.transform.SetParent(parent);
                                        gridBases[i, j].SetRoadPrefab(trailObject, false);
                                        foreach (var neighbor in neighbors)
                                        {
                                            neighbor.DeleteRoadPrefab();
                                        }
                                    }
                                    else if ((neighbors[0].GetYAxis() == j && neighbors[1].GetYAxis() > j) ||
                                             (neighbors[0].GetYAxis() > j && neighbors[1].GetYAxis() == j))
                                    {
                                        var trailObject = Instantiate(roadPrefabRight,
                                            gridBases[i, j].transform.position,
                                            Quaternion.identity);
                                        trailObject.transform.SetParent(parent);
                                        gridBases[i, j].SetRoadPrefab(trailObject, false);
                                        foreach (var neighbor in neighbors)
                                        {
                                            neighbor.DeleteRoadPrefab();
                                        }
                                    }
                                }
                                else if (neighbors[0].GetXAxis() > i || neighbors[1].GetXAxis() > i)
                                {
                                    if ((neighbors[0].GetYAxis() == j && neighbors[1].GetYAxis() < j) ||
                                        (neighbors[0].GetYAxis() < j && neighbors[1].GetYAxis() == j))
                                    {
                                        var trailObject = Instantiate(roadPrefabRight,
                                            gridBases[i, j].transform.position,
                                            Quaternion.identity);
                                        trailObject.transform.eulerAngles = new Vector3(0, 180f, 0);
                                        trailObject.transform.SetParent(parent);
                                        gridBases[i, j].SetRoadPrefab(trailObject, false);
                                        foreach (var neighbor in neighbors)
                                        {
                                            neighbor.DeleteRoadPrefab();
                                        }
                                    }
                                    else if ((neighbors[0].GetYAxis() == j && neighbors[1].GetYAxis() > j) ||
                                             (neighbors[0].GetYAxis() > j && neighbors[1].GetYAxis() == j))
                                    {
                                        var trailObject = Instantiate(roadPrefabLeft,
                                            gridBases[i, j].transform.position,
                                            Quaternion.identity);
                                        trailObject.transform.SetParent(parent);
                                        gridBases[i, j].SetRoadPrefab(trailObject, false);
                                        foreach (var neighbor in neighbors)
                                        {
                                            neighbor.DeleteRoadPrefab();
                                        }
                                    }
                                }
                            }
                            else if (neighbors.Count == 3)
                            {
                                var trailObject = Instantiate(roadPrefabTri, gridBases[i, j].transform.position,
                                    Quaternion.identity);
                                gridBases[i, j].SetRoadPrefab(trailObject, false);
                                var xMatchingNeighbors = new List<GridBase>();
                                foreach (var neighbor in neighbors)
                                {
                                    if (i == neighbor.GetXAxis())
                                    {
                                        xMatchingNeighbors.Add(neighbor);
                                    }
                                }

                                if (xMatchingNeighbors.Count == 1)
                                {
                                    if (xMatchingNeighbors[0].GetYAxis() > j)
                                    {
                                        trailObject.transform.eulerAngles = new Vector3(0, 180f, 0);
                                    }
                                }
                                else
                                {
                                    var uniqueNeighbor = neighbors.Except(xMatchingNeighbors).FirstOrDefault();
                                    if (uniqueNeighbor && uniqueNeighbor.GetXAxis() > i)
                                    {
                                        trailObject.transform.eulerAngles = new Vector3(0, 270f, 0);
                                    }
                                    else
                                    {
                                        trailObject.transform.eulerAngles = new Vector3(0, 90f, 0);
                                    }
                                }

                                trailObject.transform.SetParent(parent);

                                foreach (var neighbor in neighbors)
                                {
                                    neighbor.DeleteRoadPrefab();
                                }
                            }
                            else if (neighbors.Count == 4)
                            {
                                var trailObject = Instantiate(roadPrefabFour, gridBases[i, j].transform.position,
                                    Quaternion.identity);
                                trailObject.transform.SetParent(parent);
                                foreach (var neighbor in neighbors)
                                {
                                    neighbor.DeleteRoadPrefab();
                                }

                                gridBases[i, j].SetRoadPrefab(trailObject, false);
                            }
                        }
                    }
                }

                forwardsDone = true;
            }
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

                foreach (Vector2Int neighbor in GetValidNeighbors(current, visited, gridBases))
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
            for (var x = gridBases.GetLength(0) - 1; x >= 0; x--)
                if (gridBases[x, y].isTrail)
                    return new Vector2Int(x, y);
            return Vector2Int.one * -1;
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
                if (IsValid(next, gridBases) && gridBases[next.x, next.y].isTrail && !visited.Contains(next))
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
            if (Mathf.Abs(a.x - b.x) + Mathf.Abs(a.y - b.y) != 1)
                return false;

            return gridBases[a.x, a.y].connections.Contains(b) && gridBases[b.x, b.y].connections.Contains(a);
        }


        private void HandleAdjacentSet(GridBase[,] gridBases)
        {
            for (var y = 0; y < _levelData.height; y++)
            {
                for (var x = 0; x < _levelData.width; x++)
                {
                    var cell = _levelData.GetGridCell(x, y);
                    var gridBaseScript = gridBases[x, y];

                    if (cell.stackData.colorTypes == null)
                    {
                        cell.stackData.colorTypes = new List<LevelData.GridColorType>();
                    }

                    if (!cell.stackData.colorTypes.Contains(LevelData.GridColorType.Trail)) continue;

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

            var gridCell = levelData.GetGridCell(currentX, currentY);
            if (gridCell.stackData.colorTypes == null)
            {
                gridCell.stackData.colorTypes = new List<LevelData.GridColorType>();
            }

            if (!gridCell.stackData.colorTypes.Contains(LevelData.GridColorType.Trail))
            {
                return; // Not a Trail cell, skip it
            }

            if (currentY > 0)
            {
                var upperCell = gridBases[currentX, currentY - 1];
                if (levelData.GetGridCell(currentX, currentY - 1).stackData.colorTypes
                    .Contains(LevelData.GridColorType.Trail))
                {
                    cell.AddToAdjacent(upperCell);
                }
            }

            if (currentY < maxY)
            {
                var lowerCell = gridBases[currentX, currentY + 1];
                if (levelData.GetGridCell(currentX, currentY + 1).stackData.colorTypes
                    .Contains(LevelData.GridColorType.Trail))
                {
                    cell.AddToAdjacent(lowerCell);
                }
            }

            if (currentX > 0)
            {
                var leftCell = gridBases[currentX - 1, currentY];
                if (levelData.GetGridCell(currentX - 1, currentY).stackData.colorTypes
                    .Contains(LevelData.GridColorType.Trail))
                {
                    cell.AddToAdjacent(leftCell);
                }
            }

            if (currentX < maxX)
            {
                var rightCell = gridBases[currentX + 1, currentY];
                if (levelData.GetGridCell(currentX + 1, currentY).stackData.colorTypes
                    .Contains(LevelData.GridColorType.Trail))
                {
                    cell.AddToAdjacent(rightCell);
                }
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