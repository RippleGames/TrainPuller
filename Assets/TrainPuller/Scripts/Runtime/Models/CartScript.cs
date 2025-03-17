using System.Collections.Generic;
using FluffyUnderware.Curvy.Controllers;
using TemplateProject.Scripts.Data;
using TrainPuller.Scripts.Runtime.Managers;
using UnityEngine;

namespace TrainPuller.Scripts.Runtime.Models
{
    public class CartScript : MonoBehaviour
    {
        [SerializeField] private TrainMovement trainMovement;
        [SerializeField] private SplineController cartSplineController;
        [SerializeField] private List<Renderer> cartRenderers;
        [SerializeField] private GameColors colors;
        [SerializeField] public Vector2Int currentGridCell;

        [SerializeField] private Queue<Vector2Int> pathQueue = new Queue<Vector2Int>();
        private Vector3 movementTarget;


        [SerializeField] private float moveSpeed = 10f;
        private Vector3 movementDirection;
        public bool isMoving = false;
        public InteractionManager interactionManager;
        [SerializeField] private List<Vector3> pathPositions = new List<Vector3>(); // Liderin geçtiği pozisyonlar
        [SerializeField] private int maxPathLength = 10; // Kaydedilecek maksimum pozisyon sayısı


        public void StopMovement()
        {
            isMoving = false;
            pathQueue.Clear();
        }

        private void FixedUpdate()
        {
            if (isMoving)
            {
                MoveTowardsTarget();
            }
        }

        private void MoveTowardsTarget()
        {

            var targetPos = interactionManager.GetProjectedMousePositionOnTrail();
            UpdatePath(transform.position);
            if (Vector3.Distance(transform.position, targetPos) <= 2f)
            {
                // Hedef pozisyonun Trail hücresi içinde olup olmadığını kontrol et
                if (interactionManager.IsPositionOnTrail(targetPos))
                {
                    Vector3 snappedPosition = interactionManager.GetNearestTrailPosition(targetPos);
                    transform.position = Vector3.Lerp(transform.position, targetPos, (moveSpeed * Time.deltaTime));
                    UpdateRotation(targetPos);
                    if (Vector3.Distance(transform.position, movementTarget) < 0.1f && Vector3.Distance(
                            transform.position,
                            targetPos) <= 0.1f)
                    {
                        MoveToNextGridCell();
                    }
                }
                else
                {
                    // Eğer hedef pozisyon Trail dışındaysa, hareketi durdur
                    StopMovement();
                }
            }
        }

        private void UpdateRotation(Vector3 targetPosition)
        {
            Vector3 direction = (targetPosition - transform.position).normalized;
            if (direction != Vector3.zero)
            {
                direction.y = 0;
                transform.rotation = Quaternion.Lerp(transform.rotation, Quaternion.LookRotation(direction),
                    moveSpeed * Time.deltaTime);
            }
        }

        public List<Vector3> GetPathPositions()
        {
            return pathPositions; // Liderin geçtiği pozisyonları döndür
        }

        public void AddToPath(Vector2Int newGridCell)
        {
            if (!pathQueue.Contains(newGridCell) && !pathQueue.Contains(currentGridCell))
            {
                pathQueue.Enqueue(newGridCell);
                if (!isMoving)
                {
                    MoveToNextGridCell();
                }
            }
        }

        private void MoveToNextGridCell()
        {
            if (pathQueue.Count > 0)
            {
                Vector2Int nextGridCell = pathQueue.Dequeue();
                movementTarget = GetWorldPositionFromGrid(nextGridCell);
                currentGridCell = nextGridCell;
                isMoving = true;
            }
        }

        private void MoveCart()
        {
            transform.position = Vector3.MoveTowards(transform.position, movementTarget, moveSpeed * Time.deltaTime);

            if (Vector3.Distance(transform.position, movementTarget) < 0.01f)
            {
                if (pathQueue.Count > 0)
                {
                    MoveToNextGridCell();
                    return;
                }

                isMoving = false;
            }
        }

        public Vector3 GetWorldPositionFromGrid(Vector2Int gridPos)
        {
            var gridBase = FindObjectOfType<LevelContainer>().GetGridBases()[gridPos.x, gridPos.y];
            return gridBase.transform.position;
        }


        public void SetTrainMovementScript(TrainMovement movement)
        {
            trainMovement = movement;
        }

        public void SetCartProperties(int x, int y, LevelData.GridColorType colorType)
        {
            currentGridCell = new Vector2Int(x, y);
            var currentMaterial = colors.activeMaterials[(int)colorType];
            foreach (var renderer in cartRenderers)
            {
                renderer.sharedMaterial = currentMaterial;
            }
        }

        public TrainMovement GetTrainMovement()
        {
            return trainMovement;
        }

        public SplineController GetSplineController()
        {
            return cartSplineController;
        }

        public void SetCartGridBase(int x, int y)
        {
            currentGridCell = new Vector2Int(x, y);
        }

        public Queue<Vector2Int> GetPath()
        {
            return pathQueue;
        }

        public void UpdatePath(Vector3 targetPosition)
        {
            if (pathPositions.Count == 0 || Vector3.Distance(transform.position, pathPositions[^1]) > 0.01f)
            {
                pathPositions.Add(targetPosition);

                if (pathPositions.Count > maxPathLength)
                {
                    pathPositions.RemoveAt(0);
                }
            }
        }
    }
}