using System.Collections.Generic;
using System.Linq;
using TemplateProject.Scripts.Data;
using TemplateProject.Scripts.Runtime.Models;
using TrainPuller.Scripts.Runtime.LevelCreation;
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

        public GridBase[,] gridBases;
        public HashSet<Vector2Int> trailPositions;
        public HashSet<Vector2Int> gridPositions;

        private void Start()
        {
            AssignMainCam();
            trailPositions = GetTrailPositions();
            gridPositions = GetGridPositions();
            gridBases = FindObjectOfType<LevelContainer>().GetGridBases();
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
                isHolding = true; // Mouse'un başlangıç pozisyonunu kaydet
            }

            if (Input.GetMouseButtonUp(0))
            {
                isHolding = false;
                if (currentlySelectedCart != null)
                {
                    currentlySelectedCart.StopMovement(); // Hareketi durdur
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
            Vector3 mouseWorldPos = GetMouseWorldPosition(); // Mouse'un dünya pozisyonunu al
            Vector2Int nearestGridPos = GetNearestGridCell(mouseWorldPos, true); // En yakın grid pozisyonunu bul

            // Eğer bu pozisyon Trail hücresiyse, mouse pozisyonunu Trail üzerine project et
            if (trailPositions.Contains(nearestGridPos))
            {
                Vector3 gridWorldPos = GetWorldPositionFromGrid(nearestGridPos); // Grid hücresinin dünya pozisyonu
                Vector3 projectedPos =
                    ProjectPositionOnTrail(mouseWorldPos, nearestGridPos); // Trail üzerine project et
                return projectedPos;
            }

            // Eğer Trail hücresi değilse, en yakın Trail hücresine snap et
            return GetNearestTrailPosition(mouseWorldPos);
        }

        public Vector3 GetNearestTrailPosition(Vector3 position)
        {
            Vector2Int nearestTrailPos = Vector2Int.zero;
            float minDistance = float.MaxValue;

            foreach (var trailPos in trailPositions)
            {
                Vector3 trailWorldPos = GetWorldPositionFromGrid(trailPos);
                float distance = Vector3.Distance(position, trailWorldPos);

                if (distance < minDistance)
                {
                    minDistance = distance;
                    nearestTrailPos = trailPos;
                }
            }

            return GetWorldPositionFromGrid(nearestTrailPos);
        }

        private Vector3 ProjectPositionOnTrail(Vector3 mousePos, Vector2Int gridPos)
        {
            // Grid hücresinin dünya pozisyonunu al
            Vector3 gridWorldPos = GetWorldPositionFromGrid(gridPos);

            // Grid hücresinin komşularını al
            GridBase currentGrid = gridBases[gridPos.x, gridPos.y];
            List<GridBase> neighbors = currentGrid.GetNeighbors();

            // Komşu hücrelerin pozisyonlarını kontrol et
            bool isHorizontal = false; // Yatay komşu varsa true
            bool isVertical = false; // Dikey komşu varsa true

            foreach (var neighbor in neighbors)
            {
                if (neighbor.GetXAxis() == gridPos.x)
                {
                    // Aynı X ekseninde (dikey komşu)
                    isVertical = true;
                }
                else if (neighbor.GetYAxis() == gridPos.y)
                {
                    // Aynı Y ekseninde (yatay komşu)
                    isHorizontal = true;
                }
            }

            // Mouse pozisyonunu hizala
            Vector3 offset = mousePos - gridWorldPos;

            if (isHorizontal && !isVertical)
            {
                // Yalnızca yatay komşu varsa, Y eksenini hizala (Z eksenini sabit tut)
                offset.z = 0;
            }
            else if (isVertical && !isHorizontal)
            {
                // Yalnızca dikey komşu varsa, Z eksenini hizala (Y eksenini sabit tut)
                offset.x = 0;
            }
            else
            {
                offset.z = 0; // X eksenine hizala

                offset.x = 0; // Z eksenine hizala
            }

            // Project edilmiş pozisyonu döndür
            return gridWorldPos + offset;
        }

        public bool IsPositionOnTrail(Vector3 position)
        {
            Vector2Int nearestGridPos = GetNearestGridCell(position, false);

            var contains = trailPositions.Contains(nearestGridPos);

            return contains;
        }

        private void MoveObjectAlongGrid()
        {
            Vector3 mouseWorldPos = GetMouseWorldPosition();
            Vector2Int targetGridPos = GetNearestGridCell(mouseWorldPos, true);

            if (trailPositions.Contains(targetGridPos) && IsAdjacentToCart(targetGridPos))
            {
                currentlySelectedCart.AddToPath(targetGridPos);
            }
        }

        public Vector2Int GetNearestGridCell(Vector3 worldPos, bool inTrail)
        {
            Vector2Int closestGridPos = Vector2Int.zero;
            float minDistance = float.MaxValue;

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
            Plane plane = new Plane(Vector3.up, Vector3.zero);
            Ray ray = _mainCam.ScreenPointToRay(Input.mousePosition);

            if (plane.Raycast(ray, out float enter))
            {
                return ray.GetPoint(enter);
            }

            return Vector3.zero;
        }

        public Vector3 GetWorldPositionFromGrid(Vector2Int gridPos)
        {
            var gridBase = gridBases[gridPos.x, gridPos.y];
            return gridBase != null ? gridBase.transform.position : Vector3.zero;
        }

        private bool IsAdjacentToCart(Vector2Int targetPos)
        {
            var isAdjacentToPath = false;
            var neighbor = gridBases[currentlySelectedCart.currentGridCell.x, currentlySelectedCart.currentGridCell.y]
                .GetNeighbors().FirstOrDefault(x => x.GetXAxis() == targetPos.x && x.GetYAxis() == targetPos.y);

            if (neighbor)
            {
                // Debug.Log($"Current = [{currentlySelectedCart.currentGridCell.x},{currentlySelectedCart.currentGridCell.y}] Neighbor = [{neighbor.GetXAxis()},{neighbor.GetYAxis()}]");
            }

            if (currentlySelectedCart.GetPath().Count <= 0)
            {
                isAdjacentToPath = neighbor;
            }
            else
            {
                var gridBase = gridBases[currentlySelectedCart.GetPath().ToList()[^1].x,
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
            if (hitInfo.transform.gameObject.TryGetComponent(out CartScript cartScript))
            {
                var trainMovement = cartScript.GetTrainMovement();
                if (trainMovement.carts[0] == cartScript || trainMovement.carts[^1] == cartScript)
                {
                    trainMovement.MakeLeader(cartScript);
                    currentlySelectedCart = cartScript;
                    currentlySelectedCart.isMoving = true;
                    currentlySelectedCart.interactionManager = this;
                }
            }
        }

        public HashSet<Vector2Int> GetTrailPositions()
        {
            var trailCells = new HashSet<Vector2Int>();
            var grid = FindObjectOfType<LevelContainer>().GetGridBases();
            for (int x = 0; x < grid.GetLength(0); x++)
            {
                for (int y = 0; y < grid.GetLength(1); y++)
                {
                    if (grid[x, y].isTrail)
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
            var grid = FindObjectOfType<LevelContainer>().GetGridBases();
            for (int x = 0; x < grid.GetLength(0); x++)
            {
                for (int y = 0; y < grid.GetLength(1); y++)
                {
                    cells.Add(new Vector2Int(x, y));
                }
            }

            return cells;
        }

        private void UpdateCartRotation(Vector3 targetWorldPos)
        {
            Vector3 direction = (targetWorldPos - currentlySelectedCart.transform.position).normalized;
            if (direction != Vector3.zero)
            {
                float angle = Mathf.Atan2(direction.x, direction.z) * Mathf.Rad2Deg;
                Quaternion targetRotation = Quaternion.Euler(0, angle, 0);

                currentlySelectedCart.transform.rotation = Quaternion.RotateTowards(
                    currentlySelectedCart.transform.rotation, targetRotation, Time.deltaTime * 200f);
            }
        }
    }
}