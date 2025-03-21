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
            if (!trainMovement.isTrainMoving) return;

            // Hareket yönünü hesapla
            Vector3 movementDirection = (targetPosition - transform.position).normalized;
            movementDirection.y = 0;

            if (movementDirection.magnitude < 0.01f) return;

            // **Eğer gidilen yön, trenin yönünün tam tersi ise dönüş yapma**
            float dot = Vector3.Dot(transform.forward, movementDirection);
            if (dot < -0.9f) return;

            // **Mevcut yön ile hedef yön arasındaki açıyı hesapla**
            float angleDifference = Vector3.SignedAngle(-transform.forward, movementDirection, Vector3.up);

            // **Yön kontrolü yaparak sola -90°, sağa +90° dönüş uygula**
            if (angleDifference > 0)
            {
                angleDifference = 90f; // Sağa dönüş
            }
            else if (angleDifference < 0)
            {
                angleDifference = -90f; // Sola dönüş
            }

            // **Yeni hedef rotasyonu uygula**
            Quaternion targetRotation = Quaternion.Euler(0, transform.eulerAngles.y + angleDifference, 0);
            transform.rotation =
                Quaternion.RotateTowards(transform.rotation, targetRotation, 50f * moveSpeed * Time.deltaTime);
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