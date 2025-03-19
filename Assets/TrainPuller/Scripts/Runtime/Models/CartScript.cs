using System.Collections.Generic;
using FluffyUnderware.Curvy.Controllers;
using TemplateProject.Scripts.Data;
using TemplateProject.Scripts.Runtime.Models;
using TrainPuller.Scripts.Runtime.Managers;
using UnityEngine;

namespace TrainPuller.Scripts.Runtime.Models
{
    public class CartScript : MonoBehaviour
    {
        [SerializeField] public TrainMovement trainMovement;
        [SerializeField] private SplineController cartSplineController;
        [SerializeField] private List<Renderer> cartRenderers;
        [SerializeField] private GameColors colors;
        [SerializeField] public Vector2Int currentGridCell;
        [SerializeField] private LevelData.GridColorType cartColor;

        [SerializeField] private Queue<Vector2Int> pathQueue = new();
        private Vector3 _movementTarget;
        [SerializeField] private float moveSpeed = 10f;
        private Vector3 _movementDirection;
        public bool isMoving;
        public InteractionManager interactionManager;
        public bool isMovingBackwards;
        public Vector3 previousDirection;
        public Quaternion previousRotation;
        public GridBase[,] gridBases;


        public void StopMovement()
        {
            isMoving = false;
            trainMovement.isTrainMoving = false;
            pathQueue.Clear();
        }

        private void Update()
        {
            if (isMoving && interactionManager.GetCurrentlySelectedCart() == this)
            {
                MoveTowardsTarget();
            }
        }

        private void MoveTowardsTarget()
        {
            var targetPos = interactionManager.GetProjectedMousePositionOnTrail();

            if (!trainMovement.isTrainMoving && !trainMovement.isMovingBackwards)
            {
                if (interactionManager.IsPositionOnTrail(targetPos))
                {
                    trainMovement.isTrainMoving = true;
                }
            }

            if (!(Vector3.Distance(transform.position, targetPos) <= 2f)) return;
            if (interactionManager.IsPositionOnTrail(targetPos))
            {
                UpdateRotation(targetPos);
                
                if ((trainMovement.isMovingBackwards && trainMovement.canMoveBackwards) ||
                    (!trainMovement.isMovingBackwards && trainMovement.canMoveForward))
                {
                    transform.position = Vector3.Lerp(transform.position, targetPos, (moveSpeed * Time.deltaTime));
                }

                if (Vector3.Distance(transform.position, _movementTarget) < 0.1f &&
                    Vector3.Distance(transform.position, targetPos) <= 0.1f)
                {
                    MoveToNextGridCell();
                }
            }
            else
            {
                StopMovement();
            }
        }


        private void UpdateRotation(Vector3 targetPosition)
        {
            var direction = (targetPosition - transform.position).normalized;
            if (direction.magnitude < 0.01f) return;

            var checkRange = Mathf.Min(3, trainMovement.trainPath.Count - 1);
            var averagePreviousDirection = Vector3.zero;

            for (int i = 1; i <= checkRange; i++)
            {
                var segment = (trainMovement.trainPath[^i] - trainMovement.trainPath[^(i + 1)]).normalized;
                averagePreviousDirection += segment;
            }

            averagePreviousDirection.Normalize();

            var angle = Vector3.Angle(averagePreviousDirection, direction);
            isMovingBackwards = angle > 90f;
            trainMovement.isMovingBackwards = isMovingBackwards;

            if (isMovingBackwards)
            {
                if (!trainMovement.canMoveForward)
                {
                    trainMovement.canMoveForward = true;
                }

                transform.rotation = Quaternion.Lerp(transform.rotation,
                    Mathf.Abs(Mathf.Abs(Vector3.Angle(previousDirection, direction) - 90f)) < 20f
                        ? Quaternion.LookRotation(-previousDirection)
                        : Quaternion.LookRotation(-direction), moveSpeed * Time.deltaTime);
            }
            else
            {
                if (!trainMovement.canMoveBackwards)
                {
                    trainMovement.canMoveBackwards = true;
                }

                transform.rotation = Quaternion.Lerp(transform.rotation, Quaternion.LookRotation(direction),
                    moveSpeed * Time.deltaTime);
            }

            previousDirection = direction;
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
            if (pathQueue.Count <= 0) return;
            var nextGridCell = pathQueue.Dequeue();
            _movementTarget = GetWorldPositionFromGrid(nextGridCell);
            currentGridCell = nextGridCell;
            isMoving = true;
            trainMovement.isTrainMoving = true;
        }

        private Vector3 GetWorldPositionFromGrid(Vector2Int gridPos)
        {
            var gridBase = gridBases[gridPos.x, gridPos.y];
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

            foreach (var cartRenderer in cartRenderers)
            {
                cartRenderer.sharedMaterial = currentMaterial;
            }

            cartColor = colorType;
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

        public LevelData.GridColorType GetCartColor()
        {
            return cartColor;
        }
    }
}