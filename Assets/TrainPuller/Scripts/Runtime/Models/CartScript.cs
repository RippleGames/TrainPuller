using System;
using System.Collections.Generic;
using TemplateProject.Scripts.Data;
using TemplateProject.Scripts.Runtime.Models;
using TrainPuller.Scripts.Runtime.Managers;
using UnityEngine;
using UnityEngine.UIElements;

namespace TrainPuller.Scripts.Runtime.Models
{
    public class CartScript : MonoBehaviour
    {
        [SerializeField] public TrainMovement trainMovement;
        [SerializeField] private List<Renderer> cartRenderers;
        [SerializeField] private GameColors colors;
        [SerializeField] public Vector2Int currentGridCell;
        [SerializeField] private LevelData.GridColorType cartColor;
        [SerializeField] private List<CardSlot> cardSlots;

        [SerializeField] private Queue<Vector2Int> pathQueue = new();
        private Vector3 _movementTarget;
        [SerializeField] private float moveSpeed = 10f;
        private Vector3 _movementDirection;
        public bool isMoving;
        public InteractionManager interactionManager;
        public GridBase[,] gridBases;
        private Vector3 _previousLeaderPosition;


        public void StopMovement()
        {
            isMoving = false;
            trainMovement.StopMovement();
            pathQueue.Clear();
        }

        private void Update()
        {
            if (!interactionManager) return;
            if (!interactionManager.GetCurrentlySelectedCart()) return;
            if (isMoving && interactionManager.GetCurrentlySelectedCart() == this)
            {
                MoveTowardsTarget();
            }
        }

        private void MoveTowardsTarget()
        {
            var targetPos = interactionManager.GetProjectedMousePositionOnTrail();

            if (!trainMovement.isTrainMoving)
            {
                if (interactionManager.IsPositionOnTrail(targetPos))
                {
                    trainMovement.isTrainMoving = true;
                }
            }

            if (!(Vector3.Distance(transform.position, targetPos) <= 1f)) return;

            if (interactionManager.IsPositionOnTrail(targetPos))
            {
                UpdateRotation(this, targetPos);

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

        private void UpdateRotation(CartScript follower, Vector3 targetPosition)
        {
            var followerPosition = follower.transform.position;
            var direction = ((targetPosition + followerPosition) / 2 - followerPosition).normalized;

            if (direction.magnitude < 0.01f) return;

            direction.y = 0;
            var movementDirection = (targetPosition - transform.position).normalized;
            var dot = Vector3.Dot(transform.forward, movementDirection);

            var targetRotation = Quaternion.LookRotation(direction);
            var angleDifference = Vector3.SignedAngle(follower.transform.forward, direction, Vector3.up);

            if (Mathf.Abs(angleDifference) <= 1f) return;

            if (trainMovement.isMovingBackwards)
            {
                var offset = 180f * ((dot * (trainMovement.isMovingBackwards ? -1f : 1f) <= 0) ? -1f : 1f);

                var fixedAngle =
                    Mathf.DeltaAngle(targetRotation.eulerAngles.y, targetRotation.eulerAngles.y + offset);
                targetRotation = Quaternion.Euler(0, targetRotation.eulerAngles.y + fixedAngle, 0);
            }

            if (Quaternion.Angle(follower.transform.rotation, targetRotation) > 120f) return;
            follower.transform.rotation = Quaternion.RotateTowards(
                follower.transform.rotation,
                targetRotation,
                30f * moveSpeed * Time.fixedDeltaTime
            );
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

        private void OnTriggerEnter(Collider other)
        {
            if (other.gameObject.CompareTag("CardBase"))
            {
                if (other.TryGetComponent(out CardBase cardBase))
                {
                    var trainContainer = trainMovement.trainContainer;
                    if (!trainContainer.isAllFull)
                    {
                        var card = cardBase.TryGetCardFromStack(trainMovement.cartsColor);
                        if (!card) return;
                        trainContainer.TakeCard(card);
                    }
                }
            }

            if (other.CompareTag("TrainCart") || other.CompareTag("Exit"))
            {
                if (other.TryGetComponent(out CartScript cart))
                {
                    if (cart.trainMovement != trainMovement)
                    {
                        if (trainMovement.isMovingBackwards)
                        {
                            trainMovement.HandleBackCollision();
                        }
                        else
                        {
                            trainMovement.HandleFrontCollision();
                        }
                    }
                }

                if (other.TryGetComponent(out ExitBarrierScript exitBarrierScript))
                {
                    if (trainMovement.trainContainer.isAllFull)
                    {
                        trainMovement.GetOutFromExit(exitBarrierScript);
                    }

                    trainMovement.HandleFrontCollision();
                }
            }

            if (other.CompareTag("BackwardsEnd"))
            {
                if (other.TryGetComponent(out BackwardsEnd backwardsEnd))
                {
                    if (backwardsEnd.GetTrainMovement() == trainMovement)
                    {
                        trainMovement.HandleBackwardsMovement();
                    }
                }
            }
        }

        private void OnTriggerStay(Collider other)
        {
            if (other.gameObject.CompareTag("CardBase"))
            {
                if (other.TryGetComponent(out CardBase cardBase))
                {
                    var trainContainer = trainMovement.trainContainer;
                    if (!trainContainer.isAllFull)
                    {
                        var card = cardBase.TryGetCardFromStack(trainMovement.cartsColor);
                        if (!card) return;
                        trainContainer.TakeCard(card);
                    }
                }
            }
        }

        public List<CardSlot> GetCardSlots()
        {
            return cardSlots;
        }
    }
}