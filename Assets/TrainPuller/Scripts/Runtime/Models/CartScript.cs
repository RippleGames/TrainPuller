using System.Collections.Generic;
using DG.Tweening;
using TemplateProject.Scripts.Data;
using TrainPuller.Scripts.Runtime.Managers;
using UnityEngine;

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
        [SerializeField] private Outline outline;
        [SerializeField] private GameObject confettiObject;
        [SerializeField] private GameObject cartCover;
        [SerializeField] private GameObject crashParticle;
        [SerializeField] private GameObject onlyOneDirectionSign;

        [SerializeField] private Queue<Vector2Int> pathQueue = new();
        private Vector3 _movementTarget;
        [SerializeField] private float moveSpeed = 10f;
        private Vector3 _movementDirection;
        public bool isMoving;
        public bool isOnlyOneDirection;
        public bool isHorizontalLocked;
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
            var targetPos = interactionManager.GetProjectedMousePositionOnTrail(false);

            if (!trainMovement.isTrainMoving)
            {
                if (interactionManager.IsPositionOnTrail(targetPos))
                {
                    trainMovement.isTrainMoving = true;
                }
            }

            if (Vector3.Distance(transform.position, targetPos) > 1.75f)
            {
                targetPos = interactionManager.GetPointFromPath(transform.position);
            }

            if (Vector3.Distance(transform.position, targetPos) > 1.75f) return;
            if (interactionManager.IsPositionOnTrail(targetPos))
            {
                if (!isOnlyOneDirection)
                {
                    UpdateRotation(this, targetPos);
                }

                if ((!trainMovement.isMovingBackwards || !trainMovement.canMoveBackwards) &&
                    (trainMovement.isMovingBackwards || !trainMovement.canMoveForward)) return;

                if (isOnlyOneDirection)
                {
                    var position = transform.position;
                    var direction = (targetPos - position).normalized;
                    var dot = Vector3.Dot(direction, transform.forward);
                    targetPos = isHorizontalLocked
                        ? new Vector3(position.x, position.y, dot < 0 ? position.z : targetPos.z)
                        : new Vector3(dot < 0 ? position.x : targetPos.x, position.y, position.z);
                }


                transform.position = Vector3.Lerp(transform.position, targetPos, (moveSpeed * Time.deltaTime));
            }
            else
            {
                StopMovement();
            }
        }

        private void UpdateRotation(CartScript follower, Vector3 targetPosition)
        {
            if (trainMovement.isMovingToExit) return;
            var followerPosition = follower.transform.position;
            var direction = ((targetPosition + followerPosition) / 2 - followerPosition).normalized;

            if (direction.magnitude < 0.01f) return;

            direction.y = 0;
            var cartTransform = transform;
            var movementDirection = (targetPosition - cartTransform.position).normalized;
            var dot = Vector3.Dot(cartTransform.forward, movementDirection);

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
                90f * moveSpeed * Time.fixedDeltaTime
            );
        }


        public void AddToPath(Vector2Int newGridCell)
        {
            currentGridCell = newGridCell;
        }


        public void SetTrainMovementScript(TrainMovement movement)
        {
            trainMovement = movement;
        }

        public void SetCartProperties(int x, int y, LevelData.GridColorType colorType, bool onlyOneDirection)
        {
            currentGridCell = new Vector2Int(x, y);
            var currentMaterial = colors.activeMaterials[(int)colorType];

            foreach (var cartRenderer in cartRenderers)
            {
                cartRenderer.sharedMaterial = currentMaterial;
            }

            cartColor = colorType;

            isOnlyOneDirection = onlyOneDirection;
        }

        public void ActivateDirectionSign()
        {
            onlyOneDirectionSign.transform.localScale = Vector3.one * 2f;
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

        public void ChangeOutlineColor(Color color)
        {
            outline.OutlineColor = color;
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
                        trainContainer.TakeCardWithDelay(card);
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
                            if (!interactionManager) return;
                            if (interactionManager.GetCurrentlySelectedCart() == this)
                            {
                                trainMovement.HandleFrontCollision();
                                HandleCrashParticle(other.ClosestPoint(transform.position));
                            }
                        }
                    }
                }

                if (other.TryGetComponent(out ExitBarrierScript exitBarrierScript))
                {
                    if (trainMovement.trainContainer.isAllFull)
                    {
                        trainMovement.GetOutFromExit(exitBarrierScript);
                    }
                    else
                    {
                        trainMovement.HandleFrontCollision();
                        HandleCrashParticle(other.ClosestPoint(transform.position));
                    }
                }
            }

            // if (other.CompareTag("BackwardsEnd"))
            // {
            //     if (other.TryGetComponent(out BackwardsEnd backwardsEnd))
            //     {
            //         if (backwardsEnd.GetTrainMovement() == trainMovement)
            //         {
            //             trainMovement.HandleBackwardsMovement();
            //         }
            //     }
            // }

            if (other.CompareTag("RoadBarrier"))
            {
                if (other.TryGetComponent(out RoadBarrierScript roadBarrierScript))
                {
                    if (!roadBarrierScript.GetIsOpen())
                    {
                        if (roadBarrierScript.GetColor() == trainMovement.cartsColor)
                        {
                            if (!roadBarrierScript.TryOpenBarrier(trainMovement.cartsColor))
                            {
                                trainMovement.HandleFrontCollision();
                                HandleCrashParticle(other.ClosestPoint(transform.position));
                            }
                        }
                        else
                        {
                            trainMovement.HandleFrontCollision();
                            HandleCrashParticle(other.ClosestPoint(transform.position));
                        }
                    }
                }
            }
        }

        private void HandleCrashParticle(Vector3 closestPoint)
        {
            var particle = Instantiate(crashParticle, new Vector3(closestPoint.x, 0.5f, closestPoint.z),
                Quaternion.identity);
            Destroy(particle, 1f);
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
                        trainContainer.TakeCardWithDelay(card);
                    }
                }
            }

            // if (other.CompareTag("RoadBarrier"))
            // {
            //     if (other.TryGetComponent(out RoadBarrierScript roadBarrierScript))
            //     {
            //         if (roadBarrierScript.GetColor() == trainMovement.cartsColor)
            //         {
            //             if (!roadBarrierScript.TryOpenBarrier(trainMovement.cartsColor))
            //             {
            //                 trainMovement.HandleFrontCollision();
            //             }
            //         }
            //         else
            //         {
            //             trainMovement.HandleFrontCollision();
            //         }
            //     }
            // }
        }

        public List<CardSlot> GetCardSlots()
        {
            return cardSlots;
        }

        public bool IsAdjacent(Vector2Int futureGridCell)
        {
            return currentGridCell.x - futureGridCell.x <= 1 || currentGridCell.y - futureGridCell.y <= 1;
        }

        public void BlastConfetti()
        {
            confettiObject.SetActive(true);
        }

        public void CloseCartCover()
        {
            cartCover.SetActive(true);
            foreach (var slot in cardSlots)
            {
                slot.cartSlotTransform.DOScaleY(0f, 0.15f);
            }

            cartCover.transform.DOScale(Vector3.one, 0.15f).OnComplete(() =>
            {
                cartCover.transform.DOLocalRotate(Vector3.zero, 0.15f).SetEase(Ease.OutBounce);
            });
        }
    }
}