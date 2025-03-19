#if UNITY_EDITOR
using System.Collections.Generic;
using System.Linq;
using FluffyUnderware.Curvy;
using TemplateProject.Scripts.Data;
using TemplateProject.Scripts.Runtime.LevelCreation;
using TemplateProject.Scripts.Runtime.Models;
using TemplateProject.Scripts.Utilities;
using TrainPuller.Scripts.Data;
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
        [SerializeField] private GameColors gameColors;
        [SerializeField] private AddressablePrefabSaver prefabSaver;
        [SerializeField] private AddressablePrefabLoader prefabLoader;
        [SerializeField] private TestConfig testConfig;
        private GameObject _currentParentObject;
        private GameObject _loadedLevel;
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
        private GridBase[,] _gridBases;

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
            _gridBases = new GridBase[gridWidth, gridHeight];
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

                    cell.stackData.colorTypes ??= new List<LevelData.GridColorType>();

                    var pos = transform.position + GridSpaceToWorldSpace(x, y);

                    var gridBaseObj = PrefabUtility.InstantiatePrefab(gridBasePrefab.gameObject) as GameObject;
                    if (!gridBaseObj) continue;
                    gridBaseObj.transform.SetParent(gridParentObject.transform);
                    gridBaseObj.transform.position = pos;
                    gridBaseObj.transform.localEulerAngles = new Vector3(0f, 180f, 0);

                    var gridBaseScript = gridBaseObj.GetComponent<GridBase>();
                    _gridBases[x, y] = gridBaseScript;

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

            HandleAdjacentSet(_gridBases);
            var trailParent = new GameObject("Trail Parent");
            trailParent.transform.SetParent(newParentObject.transform);
            HandleRoadPrefabs(_gridBases, trailParent.transform);
            var cardParent = new GameObject("Card Parent");
            cardParent.transform.SetParent(newParentObject.transform);
            var exitParent = new GameObject("Exit Parent");
            exitParent.transform.SetParent(newParentObject.transform);
            var trainParent = new GameObject("Train Parent");
            trainParent.transform.SetParent(newParentObject.transform);
            HandleTrainsAndCards(_gridBases, cardParent.transform, trainParent.transform, exitParent.transform);

            levelContainer.Init(gridWidth, gridHeight, levelTime, _gridBases);
            EditorUtility.SetDirty(levelContainer);
            _currentParentObject = newParentObject;
        }

        private void HandleTrainsAndCards(GridBase[,] gridBaseArray, Transform cardParent, Transform trainCartParent,
            Transform exitParent)
        {
            var trainParentList = new List<TrainMovement>();
            for (var i = 0; i < gridBaseArray.GetLength(0); i++)
            {
                for (var j = 0; j < gridBaseArray.GetLength(1); j++)
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
                            var trainContainer = trainParent?.GetComponent<TrainContainer>();
                            if (!trainParent)
                            {
                                trainParent = new GameObject("Train Parent");
                                trainParent.transform.SetParent(trainCartParent.transform);
                                trainMovement = trainParent.AddComponent<TrainMovement>();
                                trainContainer = trainParent.AddComponent<TrainContainer>();
                                trainContainer.trainMovement = trainMovement;
                                trainMovement.trainContainer = trainContainer;
                                trainMovement.speed = 30f;
                                trainMovement.cartSpacing = 1;
                                trainMovement.cartsColor = stackDataColorTypes[1];
                                trainParentList.Add(trainMovement);
                            }

                            var trainCart = Instantiate(trainCartPrefab, gridBaseArray[i, j].transform.position,
                                Quaternion.identity);
                            trainCart.transform.SetParent(trainParent.transform);
                            var cartScript = trainCart.GetComponent<CartScript>();
                            cartScript.SetCartProperties(i, j, stackDataColorTypes[1]);
                            if (trainMovement.carts.Contains(cartScript)) continue;
                            trainMovement.carts.Add(cartScript);
                            cartScript.SetTrainMovementScript(trainMovement);
                        }
                    }
                    else if (stackDataColorTypes.Count >= 1 &&
                             !stackDataColorTypes.Contains(LevelData.GridColorType.None) &&
                             !_levelData.GetGridCell(i, j).isExit)
                    {
                        
                        var cardBase = Instantiate(cardBasePrefab, gridBaseArray[i, j].transform.position,
                            Quaternion.identity);
                        
                        cardBase.transform.SetParent(cardParent);
                        var cartBaseScript = cardBase.GetComponent<CardBase>();

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
                                cartBaseScript.AddToCardStack(cardScript);
                                yOffset += 0.1f;
                                yRotation += 90f;
                            }
                        }
                    }
                    else if (_levelData.GetGridCell(i, j).isExit)
                    {
                        var exitBarrier = Instantiate(exitPrefab, gridBaseArray[i, j].transform.position,
                            Quaternion.identity).GetComponent<ExitBarrierScript>();
                        exitBarrier.transform.SetParent(exitParent);
                        exitBarrier.SetBarrierProperties(stackDataColorTypes[^1]);
                        if (i == gridWidth - 1)
                        {
                            var barrierTransform = exitBarrier.transform;
                            barrierTransform.position += new Vector3(0.3f, 0, 0);
                            barrierTransform.eulerAngles += new Vector3(0f, -90f, 0f);
                        }
                        else if (i == 0)
                        {    var barrierTransform = exitBarrier.transform;
                            barrierTransform.position -= new Vector3(0.3f, 0, 0);
                            barrierTransform.eulerAngles += new Vector3(0f, 90f, 0f);
                        }
                        else if (j == gridHeight - 1)
                        {
                            var barrierTransform = exitBarrier.transform;
                            barrierTransform.position += new Vector3(0f, 0f, 0.3f);
                            barrierTransform.eulerAngles += new Vector3(0f, 180f, 0f);
                            
                        }
                        else if (j == 0)
                        {
                            var barrierTransform = exitBarrier.transform;
                            barrierTransform.position -= new Vector3(0f, 0f, 0.3f);
                        }
                    }
                }
            }

            foreach (var trainMovement in trainParentList)
            {
                trainMovement.trainContainer.SetCartSlots(trainMovement.carts);
                EditorUtility.SetDirty(trainMovement.gameObject);
            }
            
            
        }

        private void HandleRoadPrefabs(GridBase[,] gridBaseArray, Transform parent)
        {
            var forwardsDone = false;
            for (var x = 0; x < 2; x++)
            {
                for (var i = 0; i < gridBaseArray.GetLength(0); i++)
                {
                    for (var j = 0; j < gridBaseArray.GetLength(1); j++)
                    {
                        if (gridBaseArray[i, j].isTrail)
                        {
                            var neighbors = gridBaseArray[i, j].GetNeighbors();
                            if (!forwardsDone)
                            {
                                if (neighbors.Count == 2)
                                {
                                    if (neighbors[0].GetXAxis() == neighbors[1].GetXAxis())
                                    {
                                        var trailObject = Instantiate(roadPrefabForward,
                                            gridBaseArray[i, j].transform.position,
                                            Quaternion.identity);
                                        trailObject.transform.eulerAngles = new Vector3(0, 90f, 0);
                                        trailObject.transform.SetParent(parent);
                                        gridBaseArray[i, j].SetRoadPrefab(trailObject, true);
                                    }
                                    else if (neighbors[0].GetYAxis() == neighbors[1].GetYAxis())
                                    {
                                        var trailObject = Instantiate(roadPrefabForward,
                                            gridBaseArray[i, j].transform.position,
                                            Quaternion.identity);
                                        trailObject.transform.SetParent(parent);
                                        gridBaseArray[i, j].SetRoadPrefab(trailObject, true);
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
                                            gridBaseArray[i, j].transform.position,
                                            Quaternion.identity);
                                        trailObject.transform.eulerAngles = new Vector3(0, 180f, 0);
                                        trailObject.transform.SetParent(parent);
                                        gridBaseArray[i, j].SetRoadPrefab(trailObject, false);
                                        foreach (var neighbor in neighbors)
                                        {
                                            neighbor.DeleteRoadPrefab();
                                        }
                                    }
                                    else if ((neighbors[0].GetYAxis() == j && neighbors[1].GetYAxis() > j) ||
                                             (neighbors[0].GetYAxis() > j && neighbors[1].GetYAxis() == j))
                                    {
                                        var trailObject = Instantiate(roadPrefabRight,
                                            gridBaseArray[i, j].transform.position,
                                            Quaternion.identity);
                                        trailObject.transform.SetParent(parent);
                                        gridBaseArray[i, j].SetRoadPrefab(trailObject, false);
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
                                            gridBaseArray[i, j].transform.position,
                                            Quaternion.identity);
                                        trailObject.transform.eulerAngles = new Vector3(0, 180f, 0);
                                        trailObject.transform.SetParent(parent);
                                        gridBaseArray[i, j].SetRoadPrefab(trailObject, false);
                                        foreach (var neighbor in neighbors)
                                        {
                                            neighbor.DeleteRoadPrefab();
                                        }
                                    }
                                    else if ((neighbors[0].GetYAxis() == j && neighbors[1].GetYAxis() > j) ||
                                             (neighbors[0].GetYAxis() > j && neighbors[1].GetYAxis() == j))
                                    {
                                        var trailObject = Instantiate(roadPrefabLeft,
                                            gridBaseArray[i, j].transform.position,
                                            Quaternion.identity);
                                        trailObject.transform.SetParent(parent);
                                        gridBaseArray[i, j].SetRoadPrefab(trailObject, false);
                                        foreach (var neighbor in neighbors)
                                        {
                                            neighbor.DeleteRoadPrefab();
                                        }
                                    }
                                }
                            }
                            else if (neighbors.Count == 3)
                            {
                                var trailObject = Instantiate(roadPrefabTri, gridBaseArray[i, j].transform.position,
                                    Quaternion.identity);
                                gridBaseArray[i, j].SetRoadPrefab(trailObject, false);
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
                                var trailObject = Instantiate(roadPrefabFour, gridBaseArray[i, j].transform.position,
                                    Quaternion.identity);
                                trailObject.transform.SetParent(parent);
                                foreach (var neighbor in neighbors)
                                {
                                    neighbor.DeleteRoadPrefab();
                                }

                                gridBaseArray[i, j].SetRoadPrefab(trailObject, false);
                            }
                        }
                    }
                }

                forwardsDone = true;
            }
        }

        private void HandleAdjacentSet(GridBase[,] bases)
        {
            for (var y = 0; y < _levelData.height; y++)
            {
                for (var x = 0; x < _levelData.width; x++)
                {
                    var cell = _levelData.GetGridCell(x, y);
                    var gridBaseScript = bases[x, y];

                    cell.stackData.colorTypes ??= new List<LevelData.GridColorType>();

                    if (!cell.stackData.colorTypes.Contains(LevelData.GridColorType.Trail)) continue;

                    gridBaseScript.isTrail = true;
                    CheckForAdjacentCells(bases, _levelData, gridBaseScript);
                }
            }
        }


        private void CheckForAdjacentCells(GridBase[,] bases, LevelData levelData, GridBase cell)
        {
            var maxX = bases.GetLength(0) - 1;
            var maxY = bases.GetLength(1) - 1;
            var currentY = cell.GetYAxis();
            var currentX = cell.GetXAxis();

            var gridCell = levelData.GetGridCell(currentX, currentY);
            gridCell.stackData.colorTypes ??= new List<LevelData.GridColorType>();

            if (!gridCell.stackData.colorTypes.Contains(LevelData.GridColorType.Trail))
            {
                return; // Not a Trail cell, skip it
            }

            if (currentY > 0)
            {
                var upperCell = bases[currentX, currentY - 1];
                if (levelData.GetGridCell(currentX, currentY - 1).stackData.colorTypes
                    .Contains(LevelData.GridColorType.Trail))
                {
                    cell.AddToAdjacent(upperCell);
                }
            }

            if (currentY < maxY)
            {
                var lowerCell = bases[currentX, currentY + 1];
                if (levelData.GetGridCell(currentX, currentY + 1).stackData.colorTypes
                    .Contains(LevelData.GridColorType.Trail))
                {
                    cell.AddToAdjacent(lowerCell);
                }
            }

            if (currentX > 0)
            {
                var leftCell = bases[currentX - 1, currentY];
                if (levelData.GetGridCell(currentX - 1, currentY).stackData.colorTypes
                    .Contains(LevelData.GridColorType.Trail))
                {
                    cell.AddToAdjacent(leftCell);
                }
            }

            if (currentX < maxX)
            {
                var rightCell = bases[currentX + 1, currentY];
                if (levelData.GetGridCell(currentX + 1, currentY).stackData.colorTypes
                    .Contains(LevelData.GridColorType.Trail))
                {
                    cell.AddToAdjacent(rightCell);
                }
            }
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
            EditorSceneManager.OpenScene($"Assets/{Application.productName}/Scenes/TestScene.unity");
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