using TemplateProject.Scripts.Data;
using TemplateProject.Scripts.Mechanic;
using TemplateProject.Scripts.Runtime.Models;
using UnityEngine;

namespace TemplateProject.Scripts.Runtime.Managers
{
    public class GridManager : MonoBehaviour
    {
        public static GridManager instance;

        [Header("Cached References")] 
        [SerializeField] private GridBase[,] gridBaseArray;
        [SerializeField] private LevelContainer currentLevel;
        private AStarPathfinding pathfinder;

        private void Awake()
        {
            MakeSingleton();
        }

        private void InitializePathfinder()
        {
            pathfinder = new AStarPathfinding(gridBaseArray);
        }

        private void MakeSingleton()
        {
            if (instance == null)
            {
                instance = this;
            }
            else
            {
                Destroy(this);
            }
        }

        public void Init(GridBase[,] gridBases, LevelContainer level)
        {
            gridBaseArray = gridBases;
            currentLevel = level;
            InitializePathfinder();
        }

        public void RecalculatePaths()
        {
            currentLevel.HandleGridBasesPathfinding(gridBaseArray);
        }

        public AStarPathfinding GetPathfinder()
        {
            return pathfinder;
        }
    }
}