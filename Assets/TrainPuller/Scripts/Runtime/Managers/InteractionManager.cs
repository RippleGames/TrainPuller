using System.Collections.Generic;
using System.Linq;
using TemplateProject.Scripts.Data;
using TemplateProject.Scripts.Runtime.Models;
using TrainPuller.Scripts.Data;
using TrainPuller.Scripts.Runtime.Models;
using UnityEngine;

namespace TrainPuller.Scripts.Runtime.Managers
{
    public class InteractionManager : MonoBehaviour
    {
        [Header("Cached References")] private Camera _mainCam;
        [SerializeField] private CartScript currentlySelectedCart;

        [Header("Parameters")] public LayerMask trainCartLayer;

        [Header("Flags")] public bool isHolding;
        private GridBase[,] _gridBases;
        public HashSet<Vector2Int> trailPositions;
        public HashSet<Vector2Int> gridPositions;
        [SerializeField] LevelContainer levelContainer;

        public void InitializeInteractionManager()
        {
            AssignMainCam();            
            _gridBases = levelContainer.GetGridBases();
            trailPositions = GetTrailPositions();
            gridPositions = GetGridPositions();
        }

        private void AssignMainCam()
        {
            _mainCam = Camera.main;
        }

        private void Update()
        {

            if (Input.GetMouseButtonDown(0) && !currentlySelectedCart && !isHolding)
            {
                ProcessInteraction();
                isHolding = true;
            }

            if (Input.GetMouseButtonUp(0))
            {
                isHolding = false;
                
                if (currentlySelectedCart)
                {
                    currentlySelectedCart.StopMovement();
                    currentlySelectedCart = null;
                }
            }

            if (currentlySelectedCart && isHolding)
            {
                MoveObjectAlongGrid();
            }
        }

        public Vector3 GetProjectedMousePositionOnTrail()
        {
            var mouseWorldPos = GetMouseWorldPosition();
            var nearestGridPos = GetNearestGridCell(mouseWorldPos, true);

            if (!trailPositions.Contains(nearestGridPos)) return GetNearestTrailPosition(mouseWorldPos);
            var projectedPos =
                ProjectPositionOnTrail(mouseWorldPos, nearestGridPos);
            return projectedPos;

        }

        private Vector3 GetNearestTrailPosition(Vector3 position)
        {
            var nearestTrailPos = Vector2Int.zero;
            var minDistance = float.MaxValue;

            foreach (var trailPos in trailPositions)
            {
                var trailWorldPos = GetWorldPositionFromGrid(trailPos);
                var distance = Vector3.Distance(position, trailWorldPos);

                if (!(distance < minDistance)) continue;
                minDistance = distance;
                nearestTrailPos = trailPos;
            }

            return GetWorldPositionFromGrid(nearestTrailPos);
        }

        private Vector3 ProjectPositionOnTrail(Vector3 mousePos, Vector2Int gridPos)
        {
            var gridWorldPos = GetWorldPositionFromGrid(gridPos);

            var currentGrid = _gridBases[gridPos.x, gridPos.y];
            var neighbors = currentGrid.GetNeighbors();

            var isHorizontal = false;
            var isVertical = false; 

            foreach (var neighbor in neighbors)
            {
                if (neighbor.GetXAxis() == gridPos.x)
                {
                    isVertical = true;
                }
                else if (neighbor.GetYAxis() == gridPos.y)
                {
                    isHorizontal = true;
                }
            }

            var offset = mousePos - gridWorldPos;

            if (isHorizontal && !isVertical)
            {
                offset.z = 0;
            }
            else if (isVertical && !isHorizontal)
            {
                offset.x = 0;
            }
            else
            {
                offset.z = 0;

                offset.x = 0;
            }

            return gridWorldPos + offset;
        }

        public bool IsPositionOnTrail(Vector3 position)
        {
            var nearestGridPos = GetNearestGridCell(position, false);

            var contains = trailPositions.Contains(nearestGridPos);

            return contains;
        }

        private void MoveObjectAlongGrid()
        {
            var mouseWorldPos = GetMouseWorldPosition();
            var targetGridPos = GetNearestGridCell(mouseWorldPos, true);

            if (trailPositions.Contains(targetGridPos) && IsAdjacentToCart(targetGridPos))
            {
                currentlySelectedCart.AddToPath(targetGridPos);
            }
        }

        public Vector2Int GetNearestGridCell(Vector3 worldPos, bool inTrail)
        {
            var closestGridPos = Vector2Int.zero;
            var minDistance = float.MaxValue;

            foreach (var trailPos in inTrail ? trailPositions : gridPositions)
            {
                Vector3 cellWorldPos = GetWorldPositionFromGrid(trailPos);
                float distance = Vector3.Distance(worldPos, cellWorldPos);

                if (distance < minDistance)
                {
                    minDistance = distance;
                    closestGridPos = trailPos;
                }
            }

            return closestGridPos;
        }

        private Vector3 GetMouseWorldPosition()
        {
            var plane = new Plane(Vector3.up, Vector3.zero);
            var ray = _mainCam.ScreenPointToRay(Input.mousePosition);

            return plane.Raycast(ray, out var enter) ? ray.GetPoint(enter) : Vector3.zero;
        }

        private Vector3 GetWorldPositionFromGrid(Vector2Int gridPos)
        {
            var gridBase = _gridBases[gridPos.x, gridPos.y];
            return gridBase ? gridBase.transform.position : Vector3.zero;
        }

        private bool IsAdjacentToCart(Vector2Int targetPos)
        {
            bool isAdjacentToPath;
            var neighbor = _gridBases[currentlySelectedCart.currentGridCell.x, currentlySelectedCart.currentGridCell.y]
                .GetNeighbors().FirstOrDefault(x => x.GetXAxis() == targetPos.x && x.GetYAxis() == targetPos.y);

            if (currentlySelectedCart.GetPath().Count <= 0)
            {
                isAdjacentToPath = neighbor;
            }
            else
            {
                var gridBase = _gridBases[currentlySelectedCart.GetPath().ToList()[^1].x,
                    currentlySelectedCart.GetPath().ToList()[^1].y];

                isAdjacentToPath = gridBase.GetNeighbors()
                    .Any(x => x.GetXAxis() == targetPos.x && x.GetYAxis() == targetPos.y);
                if (isAdjacentToPath)
                {
                    // Debug.Log($"Last node at Path : [{gridBase.GetXAxis()},{gridBase.GetYAxis()}]");
                    // Debug.Log($"Checking Node : [{targetPos.x},{targetPos.y}]");
                }
            }


            return neighbor || isAdjacentToPath;
        }

        private void ProcessInteraction()
        {
            var ray = _mainCam.ScreenPointToRay(Input.mousePosition);
            if (!TryRayCast(ray, out var hitInfo, trainCartLayer)) return;
            if (!hitInfo.transform || !hitInfo.transform.CompareTag("TrainCart")) return;
            TrySelectCart(hitInfo);
        }

        private bool TryRayCast(Ray ray, out RaycastHit hitInfo, LayerMask layer)
        {
            return Physics.Raycast(ray, out hitInfo, Mathf.Infinity, layer);
        }

        private void TrySelectCart(RaycastHit hitInfo)
        {
            if (!hitInfo.transform.gameObject.TryGetComponent(out CartScript cartScript)) return;
            var trainMovement = cartScript.GetTrainMovement();
            if (trainMovement.carts[0] != cartScript && trainMovement.carts[^1] != cartScript) return;
            trainMovement.MakeLeader(cartScript);
            currentlySelectedCart = cartScript;
            currentlySelectedCart.isMoving = true;
            currentlySelectedCart.trainMovement.isTrainMoving = true;
            currentlySelectedCart.interactionManager = this;
        }

        private HashSet<Vector2Int> GetTrailPositions()
        {
            var trailCells = new HashSet<Vector2Int>();
            for (var x = 0; x < _gridBases.GetLength(0); x++)
            {
                for (var y = 0; y < _gridBases.GetLength(1); y++)
                {
                    if (_gridBases[x, y].isTrail)
                    {
                        trailCells.Add(new Vector2Int(x, y));
                    }
                }
            }

            return trailCells;
        }

        private HashSet<Vector2Int> GetGridPositions()
        {
            var cells = new HashSet<Vector2Int>();
            for (var x = 0; x < _gridBases.GetLength(0); x++)
            {
                for (var y = 0; y < _gridBases.GetLength(1); y++)
                {
                    cells.Add(new Vector2Int(x, y));
                }
            }

            return cells;
        }
        
        public bool GetCurrentlySelectedCart()
        {
            return currentlySelectedCart;
        }

        public void SetLevelContainer(LevelContainer container)
        {
            levelContainer = container;
        }
    }
}