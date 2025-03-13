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
        private HashSet<Vector2Int> trailPositions;

        private void Start()
        {
            AssignMainCam();
            trailPositions = GetTrailPositions();
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
                isHolding = true;
            }

            if (Input.GetMouseButtonUp(0))
            {
                isHolding = false;
                currentlySelectedCart = null;
            }

            if (currentlySelectedCart && isHolding)
            {
                MoveObjectAlongGrid();
            }
        }

        private void MoveObjectAlongGrid()
        {
            Vector3 mouseWorldPos = GetMouseWorldPosition();
            Vector2Int targetGridPos = GetNearestGridCell(mouseWorldPos);

            if (trailPositions.Contains(targetGridPos) && IsAdjacentToCart(targetGridPos))
            {
                Vector3 targetWorldPos = GetWorldPositionFromGrid(targetGridPos);
                currentlySelectedCart.AddToPath(targetGridPos, targetWorldPos);
            }
        }

        private Vector2Int GetNearestGridCell(Vector3 worldPos)
        {
            Vector2Int closestGridPos = Vector2Int.zero;
            float minDistance = float.MaxValue;

            foreach (var trailPos in trailPositions)
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

        private Vector3 GetWorldPositionFromGrid(Vector2Int gridPos)
        {
            var gridBase = gridBases[gridPos.x, gridPos.y];
            return gridBase != null ? gridBase.transform.position : Vector3.zero;
        }

        private bool IsAdjacentToCart(Vector2Int targetPos)
        {
            var isAdjacentToPath = false;
            if (currentlySelectedCart.GetPath().Count <= 0)
            {
                isAdjacentToPath = true;
            }
            else
            {
                var gridBase = gridBases[currentlySelectedCart.GetPath().ToList()[^1].x,
                    currentlySelectedCart.GetPath().ToList()[^1].y];
                isAdjacentToPath = gridBase.GetNeighbors()
                    .Any(x => x.GetXAxis() == targetPos.x && x.GetYAxis() == targetPos.y);
            }

            var isNeighbor = gridBases[currentlySelectedCart.currentGridCell.x, currentlySelectedCart.currentGridCell.y]
                .GetNeighbors().Any(x => x.GetXAxis() == targetPos.x && x.GetYAxis() == targetPos.y);

            return isNeighbor || isAdjacentToPath;
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
                }
            }
        }

        private HashSet<Vector2Int> GetTrailPositions()
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