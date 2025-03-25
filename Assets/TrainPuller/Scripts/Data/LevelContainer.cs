using System.Collections.Generic;
using Cinemachine;
using TemplateProject.Scripts.Runtime.Managers;
using TrainPuller.Scripts.Runtime.Managers;
using TrainPuller.Scripts.Runtime.Models;
using UnityEngine;

namespace TrainPuller.Scripts.Data
{
    public class LevelContainer : MonoBehaviour
    {
        [Header("Cached References")] [SerializeField]
        private GridSaveClass[] levelGridBases;

        [SerializeField] private List<TrainMovement> trains = new List<TrainMovement>();

        [Header("Parameters")] [SerializeField]
        private int gridWidth;

        [SerializeField] private int gridHeight;
        [SerializeField] private int levelTime;
        [SerializeField] private Vector3 cameraPosition;
        [SerializeField] private Vector3 cameraEuler;
        [SerializeField] private float orthoVal;

        public void Init(int width, int height, int time, GridBase[,] gridBases, List<TrainMovement> trainMovements)
        {
            trains.Clear();
            trains.AddRange(trainMovements);
            CopyGridArray(gridBases);
            gridWidth = width;
            gridHeight = height;
            levelTime = time;
        }

        public void SetCameraSettings(Vector3 pos, Vector3 euler, float val)
        {
            cameraPosition = pos;
            cameraEuler = euler;
            orthoVal = val;
        }
        private void CopyGridArray(GridBase[,] gridBases)
        {
            levelGridBases = new GridSaveClass[gridBases.GetLength(0)];
            for (var x = 0; x < gridBases.GetLength(0); x++)
            {
                levelGridBases[x] = new GridSaveClass
                {
                    gridCells = new GridBase[gridBases.GetLength(1)]
                };
                for (var y = 0; y < gridBases.GetLength(1); y++)
                {
                    levelGridBases[x].gridCells[y] = gridBases[x, y];
                }
            }
        }


        public void InitializeVariables(InteractionManager interactionManager, GameplayManager gameplayManager,
            GridManager gridManager,
            TimeManager timeManager, CinemachineVirtualCamera virtualCamera)
        {
            InitializeInteractionManager(interactionManager);
            InitializeGameplayManager(gameplayManager);
            InitializeGridManager(gridManager);
            InitializeTimer(timeManager);
            InitializeCamera(virtualCamera);
        }

        private void InitializeCamera(CinemachineVirtualCamera virtualCamera)
        {
            virtualCamera.transform.position = cameraPosition;
            virtualCamera.transform.eulerAngles = cameraEuler;
            virtualCamera.m_Lens.OrthographicSize = orthoVal;
        }

        private void InitializeInteractionManager(InteractionManager interactionManager)
        {
            interactionManager.SetLevelContainer(this);
            interactionManager.InitializeInteractionManager();
        }

        private void InitializeGameplayManager(GameplayManager gameplayManager)
        {
            gameplayManager.SetTrains(trains);
        }

        private void InitializeGridManager(GridManager gridManager)
        {
            var gridBasesArray = MorphTo2DArray(levelGridBases);
            gridManager.Init(gridBasesArray, this);
            HandleGridBasesPathfinding(gridBasesArray);
        }

        public GridBase[,] GetGridBases()
        {
            var gridBasesArray = MorphTo2DArray(levelGridBases);
            return gridBasesArray;
        }

        public void HandleGridBasesPathfinding(GridBase[,] gridBasesArray)
        {
            for (var i = 0; i < gridBasesArray.GetLength(0); i++)
            {
                for (var j = 0; j < gridBasesArray.GetLength(1); j++)
                {
                    gridBasesArray[i, j].HandlePath();
                }
            }
        }

        private GridBase[,] MorphTo2DArray(GridSaveClass[] gridBases)
        {
            var newGridBases = new GridBase[gridWidth, gridHeight];
            for (var x = 0; x < gridBases.GetLength(0); x++)
            {
                for (var y = 0; y < gridBases[x].gridCells.Length; y++)
                {
                    newGridBases[x, y] = gridBases[x].gridCells[y];
                }
            }

            return newGridBases;
        }


        private void InitializeTimer(TimeManager timeManager)
        {
            timeManager.SetTimer(levelTime);
        }

        public int GetLevelTime()
        {
            return levelTime;
        }

        public Vector3 GetCameraPos()
        {
            return cameraPosition;
        }

        public Vector3 GetCameraEuler()
        {
            return cameraEuler;
        }

        public float GetCameraOrthoSize()
        {
            return orthoVal;
        }
    }

    [System.Serializable]
    public class GridSaveClass
    {
        public GridBase[] gridCells;
    }
}