#if UNITY_EDITOR
using System.Collections.Generic;
using System.Linq;
using Cinemachine;
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
        [SerializeField] private GameObject roadBarrierPrefab;
        [SerializeField] private GameObject backwardsEndPrefab;
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
        [SerializeField] private LevelContainer currentLevelContainer;
        [SerializeField] private CinemachineVirtualCamera vCam;

        [Header("Level Settings")] [HideInInspector]
        public int gridWidth;

        [HideInInspector] public int gridHeight;
        [HideInInspector] public int levelIndex;
        [SerializeField] public List<LevelGoal> levelGoals;
        [SerializeField] private int levelTime;
        [SerializeField] private LevelData.GridColorType colorTypes;
        public bool isExit;
        public bool isBarrier;

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
            _levelData.SetCellColor(x, y, colorTypes, isExit, isBarrier, index);
            EditorUtility.SetDirty(this);
        }


        public void GridRemoveButtonAction(int x, int y, int index)
        {
            if (_levelData == null) return;
            _levelData.RemoveCellColor(x, y, index);
        }


        public void SaveLevel()
        {
            var cam = Camera.main;
            currentLevelContainer.SetCameraSettings(cam.transform.position, cam.transform.rotation.eulerAngles,
                cam.orthographicSize);
            EditorUtility.SetDirty(currentLevelContainer);
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
                    currentLevelContainer = level.GetComponent<LevelContainer>();
                    AssignCameraSettings();
                    levelGoals = currentLevelContainer.GetLevelGoals();
                    levelTime = currentLevelContainer.GetLevelTime();
                    _loadedLevel = level;
                });
        }

        private void AssignCameraSettings()
        {
            vCam.transform.position = currentLevelContainer.GetCameraPos();
            vCam.transform.eulerAngles = currentLevelContainer.GetCameraEuler();
            vCam.m_Lens.OrthographicSize = currentLevelContainer.GetCameraOrthoSize();
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
            currentLevelContainer = newParentObject.AddComponent<LevelContainer>();
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
                        gridBaseScript.Init(false, x, y);
                    }
                    else
                    {
                        gridBaseScript.Init(false, x, y);
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
            var barrierParent = new GameObject("Barrier Parent");
            barrierParent.transform.SetParent(newParentObject.transform);
            HandleTrainsAndCards(_gridBases, cardParent.transform, trainParent.transform, exitParent.transform,
                barrierParent.transform);

            currentLevelContainer.Init(gridWidth, gridHeight, levelTime, _gridBases);
            EditorUtility.SetDirty(currentLevelContainer);
            _currentParentObject = newParentObject;
        }

        private void HandleTrainsAndCards(GridBase[,] gridBaseArray, Transform cardParent, Transform trainCartParent,
            Transform exitParent, Transform barrierParent)
        {
            var trainParentList = new List<TrainMovement>();
            for (var i = 0; i < gridBaseArray.GetLength(0); i++)
            {
                for (var j = 0; j < gridBaseArray.GetLength(1); j++)
                {
                    var stackDataColorTypes = _levelData.GetGridCell(i, j).stackData.colorTypes;

                    if (stackDataColorTypes.Contains(LevelData.GridColorType.Trail) &&
                        !_levelData.GetGridCell(i, j).isExit && !_levelData.GetGridCell(i, j).isBarrier)
                    {
                        stackDataColorTypes = stackDataColorTypes.Distinct().ToList();
                        if (stackDataColorTypes.Count > 1)
                        {
                            var trainMovementList =
                                trainParentList.Where(x => x.cartsColor == stackDataColorTypes[1]).ToList();

                            if (trainMovementList.Count <= 0)
                            {
                                var trainParent = new GameObject("Train Parent");
                                trainParent.transform.SetParent(trainCartParent.transform);
                                var trainMovement = trainParent.AddComponent<TrainMovement>();
                                var trainContainer = trainParent.AddComponent<TrainContainer>();
                                trainMovement.backwardsEndPrefab = backwardsEndPrefab;
                                trainContainer.trainMovement = trainMovement;
                                trainMovement.trainContainer = trainContainer;
                                trainMovement.speed = 30f;
                                trainMovement.cartSpacing = 1;
                                trainMovement.cartsColor = stackDataColorTypes[1];
                                trainParentList.Add(trainMovement);
                                trainMovementList =
                                    trainParentList.Where(x => x.cartsColor == stackDataColorTypes[1]).ToList();
                            }

                            for (var t = 0; t < trainMovementList.Count; t++)
                            {
                                var trainMovement = trainMovementList[t];
                                var isNeighborToTrain = GetIsNeighborToTrain(trainMovement, i, j);
                                var trainParent = trainMovement.gameObject;
                                var trainContainer = trainParent.GetComponent<TrainContainer>();

                                if (!isNeighborToTrain)
                                {
                                    if (t == trainMovementList.Count - 1)
                                    {
                                        trainParent = new GameObject("Train Parent");
                                        trainParent.transform.SetParent(trainCartParent.transform);
                                        var newTrainMovement = trainParent.AddComponent<TrainMovement>();
                                        trainContainer = trainParent.AddComponent<TrainContainer>();
                                        newTrainMovement.backwardsEndPrefab = backwardsEndPrefab;
                                        trainContainer.trainMovement = trainMovement;
                                        newTrainMovement.trainContainer = trainContainer;
                                        newTrainMovement.speed = 30f;
                                        newTrainMovement.cartSpacing = 1;
                                        newTrainMovement.cartsColor = stackDataColorTypes[1];
                                        trainParentList.Add(newTrainMovement);
                                        trainMovement = newTrainMovement;
                                        trainParent = newTrainMovement.gameObject;
                                    }
                                    else
                                    {
                                        continue;
                                    }
                                }

                                var trainCart =
                                    PrefabUtility.InstantiatePrefab(trainCartPrefab) as GameObject;
                                trainCart.transform.position = gridBaseArray[i, j].transform.position;
                                trainCart.transform.rotation = Quaternion.identity;
                                trainCart.transform.SetParent(trainParent.transform);
                                var trainCartRotation = GetCartRotation(i, j);
                                trainCart.transform.eulerAngles = trainCartRotation;
                                var cartScript = trainCart.GetComponent<CartScript>();
                                cartScript.SetCartProperties(i, j, stackDataColorTypes[1]);
                                if (trainMovement.carts.Contains(cartScript)) continue;
                                trainMovement.carts.Add(cartScript);
                                trainMovement.cartCells.Add(_gridBases[i, j]);
                                cartScript.SetTrainMovementScript(trainMovement);
                                break;
                            }
                        }
                    }
                    else if (stackDataColorTypes.Count >= 1 &&
                             !stackDataColorTypes.Contains(LevelData.GridColorType.None) &&
                             !_levelData.GetGridCell(i, j).isExit && !_levelData.GetGridCell(i, j).isBarrier)
                    {
                        var cardBase =
                            PrefabUtility.InstantiatePrefab(cardBasePrefab) as GameObject;
                        cardBase.transform.position = gridBaseArray[i, j].transform.position;
                        cardBase.transform.rotation = Quaternion.identity;

                        cardBase.transform.SetParent(cardParent);
                        var cartBaseScript = cardBase.GetComponent<CardBase>();

                        var yOffset = 0.05f;
                        var yRotation = 0f;
                        foreach (var colorType in stackDataColorTypes)
                        {
                            for (var k = 0; k < 3; k++)
                            {
                                var cardObject =
                                    PrefabUtility.InstantiatePrefab(cardPrefab) as GameObject;
                                cardObject.transform.position =
                                    cardBase.transform.position + new Vector3(0f, yOffset, 0f);
                                cardObject.transform.rotation = Quaternion.identity;
                                var card = cardObject.GetComponent<CardScript>();
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
                        var exitBarrierObject =
                            PrefabUtility.InstantiatePrefab(exitPrefab) as GameObject;
                        exitBarrierObject.transform.position = gridBaseArray[i, j].transform.position;
                        exitBarrierObject.transform.rotation = Quaternion.identity;
                        var exitBarrier = exitBarrierObject.GetComponent<ExitBarrierScript>();
                        exitBarrier.transform.SetParent(exitParent);
                        // exitBarrier.SetBarrierProperties(stackDataColorTypes[^1]);
                        if (i == gridWidth - 1)
                        {
                            var barrierTransform = exitBarrier.transform;
                            barrierTransform.position += new Vector3(0.3f, 0, 0);
                            barrierTransform.eulerAngles += new Vector3(0f, -90f, 0f);
                        }
                        else if (i == 0)
                        {
                            var barrierTransform = exitBarrier.transform;
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
                    else if (_levelData.GetGridCell(i, j).isBarrier)
                    {
                        var roadBarrierGameObject =
                            PrefabUtility.InstantiatePrefab(roadBarrierPrefab) as GameObject;
                        var roadBarrier = roadBarrierGameObject.GetComponent<RoadBarrierScript>();
                        roadBarrier.transform.position = gridBaseArray[i, j].transform.position;
                        roadBarrier.transform.rotation = Quaternion.identity;
                        roadBarrier.transform.SetParent(exitParent);
                        var rotation = HandleBarrierRotation(i, j);
                        roadBarrier.transform.eulerAngles = rotation;
                        roadBarrier.SetColorType(stackDataColorTypes[1]);
                    }
                }
            }

            foreach (var trainMovement in trainParentList)
            {
                trainMovement.trainContainer.SetCartSlots(trainMovement.carts);
                EditorUtility.SetDirty(trainMovement.gameObject);
            }
        }

        private Vector3 HandleBarrierRotation(int x, int y)
        {
            var neighbors = _gridBases[x, y].GetNeighbors();
            var isHorizontal = true;
            var isVertical = true;
            var isHorizontalRight = false;
            var isVerticalDown = false;
            var cartGridCell = _levelData.GetGridCell(x, y);
            foreach (var neighbor in neighbors)
            {
                var neighborX = neighbor.GetXAxis();
                var neighborY = neighbor.GetYAxis();

                if (!isHorizontalRight && x > neighborX && y == neighborY)
                {
                    isHorizontalRight = true;
                }

                if (!isVerticalDown && y > neighborY && x == neighborX)
                {
                    isVerticalDown = true;
                }

                isVertical = isVertical && neighborX == x;

                isHorizontal = isHorizontal && neighborY == y;
            }

            if (!isHorizontal && !isVertical)
            {
                return new Vector3(0f, x == 1 ? 90f : -90f, 0f);
            }

            if (isHorizontal)
            {
                return Vector3.zero;
            }

            if (isVertical)
            {
                return new Vector3(0f, x == 1 ? 90f : -90f, 0f);
            }

            return Vector3.zero;
        }

        private bool GetIsNeighborToTrain(TrainMovement trainMovement, int x, int y)
        {
            if (!trainMovement) return false;
            if (trainMovement.carts.Count == 0) return true;
            var isNeighbor = false;
            var currentGridCell = _gridBases[x, y];
            foreach (var cartCell in trainMovement.cartCells)
            {
                if (cartCell.GetNeighbors().Contains(currentGridCell))
                {
                    isNeighbor = true;
                }
            }

            return isNeighbor;
        }

        private Vector3 GetCartRotation(int x, int y)
        {
            var neighbors = _gridBases[x, y].GetNeighbors();
            var isHorizontal = true;
            var isVertical = true;
            var isHorizontalRight = false;
            var isVerticalDown = false;
            var cartGridCell = _levelData.GetGridCell(x, y);
            foreach (var neighbor in neighbors)
            {
                var neighborX = neighbor.GetXAxis();
                var neighborY = neighbor.GetYAxis();
                var neighborGridCell = _levelData.GetGridCell(neighborX, neighborY);
                if (neighborGridCell.isExit || neighborGridCell.isBarrier ||
                    neighborGridCell.stackData.colorTypes[^1] != cartGridCell.stackData.colorTypes[^1]) continue;

                if (!isHorizontalRight && x > neighborX && y == neighborY)
                {
                    isHorizontalRight = true;
                }

                if (!isVerticalDown && y > neighborY && x == neighborX)
                {
                    isVerticalDown = true;
                }

                isVertical = isVertical && neighborX == x;

                isHorizontal = isHorizontal && neighborY == y;
            }

            if (!isHorizontal && !isVertical)
            {
                if (isHorizontalRight && !isVerticalDown)
                {
                    return new Vector3(0f, 45f, 0f);
                }

                if (isHorizontalRight && isVerticalDown)
                {
                    return new Vector3(0f, -45f, 0f);
                }

                if (!isHorizontalRight && !isVerticalDown)
                {
                    return new Vector3(0f, -45f, 0f);
                }

                if (!isHorizontalRight && isVerticalDown)
                {
                    return new Vector3(0f, 45f, 0f);
                }
            }

            if (isHorizontal && !isVertical)
            {
                return new Vector3(0f, 90f, 0f);
            }

            return Vector3.zero;
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
                                if (neighbors.Count == 1)
                                {
                                    var trailObject =
                                        PrefabUtility.InstantiatePrefab(roadPrefabForward) as GameObject;
                                    trailObject.transform.position = gridBaseArray[i, j].transform.position;
                                    trailObject.transform.rotation = Quaternion.identity;
                                    trailObject.transform.eulerAngles =
                                        new Vector3(0, neighbors[0].GetXAxis() == i ? 90f : 0f, 0);
                                    trailObject.transform.SetParent(parent);
                                    gridBaseArray[i, j].SetRoadPrefab(trailObject, true);
                                }
                                else if (neighbors.Count == 2)
                                {
                                    if (neighbors[0].GetXAxis() == neighbors[1].GetXAxis())
                                    {
                                        var trailObject =
                                            PrefabUtility.InstantiatePrefab(roadPrefabForward) as GameObject;
                                        trailObject.transform.position = gridBaseArray[i, j].transform.position;
                                        trailObject.transform.rotation = Quaternion.identity;
                                        trailObject.transform.eulerAngles = new Vector3(0, 90f, 0);
                                        trailObject.transform.SetParent(parent);
                                        gridBaseArray[i, j].SetRoadPrefab(trailObject, true);
                                    }
                                    else if (neighbors[0].GetYAxis() == neighbors[1].GetYAxis())
                                    {
                                        var trailObject =
                                            PrefabUtility.InstantiatePrefab(roadPrefabForward) as GameObject;
                                        trailObject.transform.position = gridBaseArray[i, j].transform.position;
                                        trailObject.transform.rotation = Quaternion.identity;
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
                                        var trailObject =
                                            PrefabUtility.InstantiatePrefab(roadPrefabLeft) as GameObject;
                                        trailObject.transform.position = gridBaseArray[i, j].transform.position;
                                        trailObject.transform.rotation = Quaternion.identity;
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
                                        var trailObject =
                                            PrefabUtility.InstantiatePrefab(roadPrefabRight) as GameObject;
                                        trailObject.transform.position = gridBaseArray[i, j].transform.position;
                                        trailObject.transform.rotation = Quaternion.identity;
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
                                        var trailObject =
                                            PrefabUtility.InstantiatePrefab(roadPrefabRight) as GameObject;
                                        trailObject.transform.position = gridBaseArray[i, j].transform.position;
                                        trailObject.transform.rotation = Quaternion.identity;
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
                                        var trailObject =
                                            PrefabUtility.InstantiatePrefab(roadPrefabLeft) as GameObject;
                                        trailObject.transform.position = gridBaseArray[i, j].transform.position;
                                        trailObject.transform.rotation = Quaternion.identity;
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
                                var trailObject =
                                    PrefabUtility.InstantiatePrefab(roadPrefabTri) as GameObject;
                                trailObject.transform.position = gridBaseArray[i, j].transform.position;
                                trailObject.transform.rotation = Quaternion.identity;
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
                                var trailObject =
                                    PrefabUtility.InstantiatePrefab(roadPrefabFour) as GameObject;
                                trailObject.transform.position = gridBaseArray[i, j].transform.position;
                                trailObject.transform.rotation = Quaternion.identity;
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