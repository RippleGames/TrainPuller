using System.Collections.Generic;
using UnityEngine;

namespace TrainPuller.Scripts.Runtime.Models
{
    public class GridBase : MonoBehaviour
    {
        [Header("Cached References")]
        // [SerializeField] private Renderer gridRenderer;
        // [SerializeField] private GameObject wallObject;
        [SerializeField] private GridBase parent;
        [SerializeField] private List<GridBase> adjacentCells;
        [SerializeField] private GameObject associatedRoadPrefab;

        [Header("Parameters")] [SerializeField]
        private int x, y;

        [SerializeField] private float gCost, hCost;
        private float fCost => gCost + hCost;

        [Header("Flags")] [SerializeField] private bool isClosed;
        [SerializeField] private bool visited;
        [SerializeField] private bool isControlPoint;
        public bool isTrail;
        public bool isForward;
        public HashSet<Vector2Int> connections = new HashSet<Vector2Int>();

        public void HandlePath()
        {
            // if (!stickman) return;
            //
            // UniTask.SwitchToTaskPool();
            // closestPath = GridManager.instance.GetPathfinder().FindPath(new Vector2Int(x, y));
            // UniTask.SwitchToMainThread();
            //
            // GridManager.instance.GetPathfinder().ResetVisitedStates();
            //
            // if (closestPath == null)
            // {
            //     stickman.DisableInteraction();
            // }
            // else
            // {
            //     if (stickman.GetIsSecret())
            //     {
            //         stickman.ResetColor();
            //     }
            //
            //     stickman.EnableInteraction();
            // }
        }

        public void Init(bool flag, int xAxis, int yAxis)
        {
            x = xAxis;
            y = yAxis;

        }

        public void ResetVisited()
        {
            visited = false;
        }


        // public List<GridBase> GetClosestPath()
        // {
        //     return closestPath;
        // }

        public int GetYAxis()
        {
            return y;
        }

        public int GetXAxis()
        {
            return x;
        }

        public GridBase GetBaseParent()
        {
            return parent;
        }

        public void SetBaseParent(GridBase newParent)
        {
            parent = newParent;
        }

        public float GetFCost()
        {
            return fCost;
        }

        public float GetHCost()
        {
            return hCost;
        }

        public float GetGCost()
        {
            return gCost;
        }

        public void SetHCost(float newHCost)
        {
            hCost = newHCost;
        }

        public void SetGCost(float newGCost)
        {
            gCost = newGCost;
        }

        public bool GetVisited()
        {
            return visited;
        }

        public bool GetIsClosed()
        {
            return isClosed;
        }
        public void SetVisited(bool flag)
        {
            visited = flag;
        }

        public void SetIsControlPoint(bool flag)
        {
            isControlPoint = flag;
        }

        public bool GetIsControlPoint()
        {
            return isControlPoint;
        }

        public void AddToAdjacent(GridBase adjacentGridCell)
        {
            if (!adjacentCells.Contains(adjacentGridCell))
            {
                adjacentCells.Add(adjacentGridCell);
            }
        }

        public List<GridBase> GetNeighbors()
        {
            return adjacentCells;
        }

        public void SetRoadPrefab(GameObject prefab, bool flag)
        {
            isForward = flag;
            associatedRoadPrefab = prefab;
        }

        public void DeleteRoadPrefab()
        {
            if(!isForward) return;
            DestroyImmediate(associatedRoadPrefab);
        }
    }
}