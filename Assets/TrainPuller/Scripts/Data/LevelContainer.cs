using System.Collections.Generic;
using TemplateProject.Scripts.Runtime.LevelCreation;
using TemplateProject.Scripts.Runtime.Managers;
using TemplateProject.Scripts.Runtime.Models;
using TrainPuller.Scripts.Runtime.LevelCreation;
using UnityEngine;

namespace TemplateProject.Scripts.Data
{
    public class LevelContainer : MonoBehaviour
    {

        [Header("Cached References")]
        [SerializeField] private GridSaveClass[] levelGridBases;
        [SerializeField] private List<GoalScript> levelGoalScripts;
        [SerializeField] private List<LevelGoal> levelGoals;
        
        [Header("Parameters")]
        [SerializeField] private int gridWidth;
        [SerializeField] private int gridHeight;
        [SerializeField] private int levelTime;

        public void Init(int width, int height, int time, GridBase[,] gridBases, List<GoalScript> busScripts, List<LevelGoal> goals)
        {
            CopyGridArray(gridBases);
            gridWidth = width;
            gridHeight = height;
            levelGoalScripts = busScripts;
            levelTime = time;
            levelGoals = goals;
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


        public void InitializeVariables(GameplayManager gameplayManager, GridManager gridManager,
            TimeManager timeManager)
        {
            InitializeGameplayManager(gameplayManager);
            InitializeGridManager(gridManager);
            InitializeTimer(timeManager);
        }

        private void InitializeGameplayManager(GameplayManager gameplayManager)
        {
            gameplayManager.SetBuses(levelGoalScripts);
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
                   gridBasesArray[i,j].HandlePath();
                }
            }
        }

        private GridBase[,] MorphTo2DArray(GridSaveClass[] gridBases)
        {
            var newGridBases = new GridBase[gridWidth,gridHeight];
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

        public List<LevelGoal> GetLevelGoals()
        {
            return levelGoals;
        }

        public int GetLevelTime()
        {
            return levelTime;
        }
    }

    [System.Serializable]
    public class GridSaveClass
    {
        public GridBase[] gridCells;

    }
}