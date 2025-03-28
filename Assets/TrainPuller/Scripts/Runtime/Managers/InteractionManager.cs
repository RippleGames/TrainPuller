using System.Collections.Generic;
using System.Linq;
using TrainPuller.Scripts.Data;
using TrainPuller.Scripts.Runtime.Models;
using UnityEngine;

namespace TrainPuller.Scripts.Runtime.Managers
{
    public class InteractionManager : MonoBehaviour
    {
        [Header("Cached References")] private Camera _mainCam;
        [SerializeField] private CartScript currentlySelectedCart;
        [SerializeField] LevelContainer levelContainer;
        public LineRenderer lineRenderer;

        [Header("Parameters")] public LayerMask trainCartLayer;
        public HashSet<Vector2Int> trailPositions;
        public HashSet<Vector2Int> gridPositions;
        private GridBase[,] _gridBases;
        [SerializeField] public List<Vector3> dragPath = new List<Vector3>();

        [Header("Flags")] public bool isHolding;


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
            if (!LevelManager.instance.isGamePlayable) return;
            if (Input.GetMouseButtonDown(0) && !currentlySelectedCart && !isHolding)
            {
                HandleTimerStart();
                ProcessInteraction();
                isHolding = true;
            }


            if (Input.GetMouseButton(0))
            {
                if (currentlySelectedCart && isHolding)
                {
                    HandleLeaderChange();
                }
            }

            if (Input.GetMouseButtonUp(0))
            {
                isHolding = false;

                if (currentlySelectedCart)
                {
                    currentlySelectedCart.StopMovement();
                    currentlySelectedCart.trainMovement.ChangeOutlineColor(Color.black);
                    currentlySelectedCart = null;
                }

                lineRenderer.enabled = false;

                dragPath.Clear();
            }


            if (currentlySelectedCart && isHolding)
            {
                MoveObjectAlongGrid();
            }
        }

        private void HandleLeaderChange()
        {
            var ray = _mainCam.ScreenPointToRay(Input.mousePosition);
            if (!TryRayCast(ray, out var hitInfo, trainCartLayer)) return;
            if (!hitInfo.transform || !hitInfo.transform.CompareTag("TrainCart")) return;
            if (!hitInfo.transform.gameObject.TryGetComponent(out CartScript cartScript)) return;
            var trainMovement = cartScript.GetTrainMovement();
            if (trainMovement.carts[0] != cartScript && trainMovement.carts[^1] != cartScript) return;
            if (currentlySelectedCart == trainMovement.carts[0] && cartScript == trainMovement.carts[^1])
            {
                trainMovement.MakeLeader(cartScript);
                if (!cartScript.interactionManager)
                {
                    cartScript.interactionManager = this;
                }

                currentlySelectedCart = cartScript;
            }
        }

        private void HandleTimerStart()
        {
            if (LevelManager.instance.isTutorialOn)
            {
                UIManager.instance.CloseTutorialHand();
            }

            if (!TimeManager.instance.GetIsTimerActive())
            {
                TimeManager.instance.StartTimer();
            }
        }

        public Vector3 GetProjectedMousePositionOnTrail(bool onlyCheck)
        {
            var mouseWorldPos = GetMouseWorldPosition();

            var nearestGridPos = GetNearestGridCell(mouseWorldPos, true);

            if (!trailPositions.Contains(nearestGridPos)) return GetNearestTrailPosition(mouseWorldPos);
            var projectedPos =
                ProjectPositionOnTrail(mouseWorldPos, nearestGridPos);

            if (currentlySelectedCart)
            {
                currentlySelectedCart.AddToPath(nearestGridPos);
            }

            if (Vector3.Distance(transform.position, projectedPos) > 1.75f)
            {
                if (!onlyCheck)
                {
                    UpdateDragPath(projectedPos);
                }

                return projectedPos;
            }

            return projectedPos;
        }

        public void UpdateDragPath(Vector3 newPoint)
        {
            if (dragPath.Count == 0 || Vector3.Distance(dragPath[^1], newPoint) > 1f)
            {
                var trainPosition = currentlySelectedCart.transform.position;

                var movementDirection = (newPoint - trainPosition).normalized;

                if (dragPath.Count > 1)
                {
                    if (Vector3.Dot(currentlySelectedCart.transform.forward, movementDirection) < 0f)
                    {
                        return;
                    }
                }

                newPoint = ProjectPositionOnTrail(newPoint, GetNearestGridCell(newPoint, true));
                dragPath.Add(newPoint);
            }
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

        public Vector3 ProjectPositionOnTrail(Vector3 mousePos, Vector2Int gridPos)
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

            UpdateLineRenderer(mouseWorldPos);
        }

        private void UpdateLineRenderer(Vector3 mouseWorldPos)
        {
            if (!currentlySelectedCart)
            {
                lineRenderer.enabled = false;
                return;
            }

            if (!lineRenderer.enabled)
            {
                lineRenderer.enabled = true;
            }

            var selectedCartPos = currentlySelectedCart.transform.position;
            lineRenderer.SetPosition(0, new Vector3(selectedCartPos.x, 1f, selectedCartPos.z));
            lineRenderer.SetPosition(1, new Vector3(mouseWorldPos.x, 1f, mouseWorldPos.z));
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
            if (currentlySelectedCart) return;
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
            if (trainMovement.carts[0] != cartScript && trainMovement.carts[^1] != cartScript)
            {
                var mousePos = GetMouseWorldPosition();
                if (Vector3.Distance(trainMovement.carts[0].transform.position, mousePos) <
                    Vector3.Distance(trainMovement.carts[^1].transform.position, mousePos))
                {
                    cartScript = trainMovement.carts[0];
                }
                else
                {
                    cartScript = trainMovement.carts[^1];
                }
            }

            trainMovement.MakeLeader(cartScript);
            currentlySelectedCart = cartScript;
            currentlySelectedCart.isMoving = true;
            currentlySelectedCart.trainMovement.isTrainMoving = true;
            currentlySelectedCart.interactionManager = this;
            currentlySelectedCart.trainMovement.ChangeOutlineColor(Color.white);
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

        public CartScript GetCurrentlySelectedCart()
        {
            return currentlySelectedCart;
        }

        public void SetLevelContainer(LevelContainer container)
        {
            levelContainer = container;
        }

        public void HandleExit()
        {
            lineRenderer.enabled = false;
            currentlySelectedCart = null;
            isHolding = false;
        }

        public Vector3 GetPointFromPath(Vector3 cartPos)
        {
            if (dragPath.Count <= 0) return cartPos;

            var pos = dragPath[0];
            var maxIterations = 100;
            var iteration = 0;

            while (Vector3.Distance(pos, cartPos) > 1.75f && iteration < maxIterations)
            {
                var closerPoint = (pos + cartPos) / 2f;
                closerPoint = ProjectPositionOnTrail(closerPoint, GetNearestGridCell(closerPoint, true));

                var direction = (closerPoint - cartPos).normalized;

                dragPath.Remove(dragPath[0]);
                if (Vector3.Dot(direction, currentlySelectedCart.transform.forward) < 0f)
                {
                    if (dragPath.Count > 0)
                    {
                        pos = dragPath[0];
                        continue;
                    }
                }

                dragPath.Insert(0, closerPoint);
                pos = dragPath[0];

                iteration++;
            }

            dragPath.RemoveAt(0);
            return pos;
        }
    }
}