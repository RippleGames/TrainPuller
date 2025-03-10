using System.Collections.Generic;
using TemplateProject.Scripts.Data;
using TemplateProject.Scripts.Runtime.Models;
using TrainPuller.Scripts.Runtime.LevelCreation;
using TrainPuller.Scripts.Runtime.Models;
using UnityEngine;

namespace TrainPuller.Scripts.Runtime.Managers
{
    public class InteractionManager : MonoBehaviour
    {
        [Header("Cached References")]
        private Camera _mainCam;
        [SerializeField] private CartScript currentlySelectedCart;

        [Header("Parameters")] 
        public LayerMask trainCartLayer;

        [Header("Flags")] 
        public bool isHolding;
        private HashSet<Vector2Int> trailPositions;

        private void Start()
        {
            AssignMainCam();
            trailPositions = GetTrailPositions();
        }

        private void AssignMainCam()
        {
            _mainCam = Camera.main;
        }

        private void Update()
        {
            if (Input.GetMouseButtonDown(0) && !currentlySelectedCart && !isHolding)
            {
                ProcessRaycastInteraction();
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
            var ray = _mainCam.ScreenPointToRay(Input.mousePosition);
            if (Physics.Raycast(ray, out RaycastHit hit))
            {
                if (hit.collider.CompareTag("GridBase") && hit.collider.gameObject.TryGetComponent(out GridBase gridBase))
                {
                    Vector2Int gridPos = GetGridPosition(gridBase.transform.position);

                    if (gridBase.isTrail && IsAdjacentToCart(gridPos))
                    {
                        Vector3 targetWorldPos = new Vector3(gridBase.transform.position.x,
                            currentlySelectedCart.transform.position.y, gridBase.transform.position.z);

                        currentlySelectedCart.transform.position = Vector3.MoveTowards(
                            currentlySelectedCart.transform.position, targetWorldPos, 5f * Time.deltaTime);

                        UpdateCartRotation(targetWorldPos);
                    }
                }
            }
        }

        private bool IsAdjacentToCart(Vector2Int targetPos)
        {
            Vector2Int cartPos = GetGridPosition(currentlySelectedCart.transform.position);

            int dx = Mathf.Abs(targetPos.x - cartPos.x);
            int dy = Mathf.Abs(targetPos.y - cartPos.y);

            return (dx == 1 && dy == 0) || (dx == 0 && dy == 1); 
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

        private Vector2Int GetGridPosition(Vector3 worldPos)
        {
            return new Vector2Int(Mathf.RoundToInt(worldPos.x), Mathf.RoundToInt(worldPos.z));
        }

        private void ProcessRaycastInteraction()
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
    }
}
