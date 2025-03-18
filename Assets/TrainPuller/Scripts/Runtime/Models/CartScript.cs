using System.Collections.Generic;
using FluffyUnderware.Curvy.Controllers;
using TemplateProject.Scripts.Data;
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

        [SerializeField] private Queue<Vector2Int> pathQueue = new Queue<Vector2Int>();
        private Vector3 movementTarget;


        [SerializeField] private float moveSpeed = 10f;
        private Vector3 movementDirection;
        public bool isMoving = false;
        public InteractionManager interactionManager;
        [SerializeField] private int maxPathLength = 10; // Kaydedilecek maksimum pozisyon sayısı
        public bool isMovingBackwards;
        public Vector3 previousDirection;
        public Quaternion previousRotation;


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
            if (direction.magnitude < 0.01f) return; // Eğer hareket yoksa dönüş yapma

            // **Son 3-5 noktayı kontrol ederek önceki yönü bul**
            int checkRange = Mathf.Min(3, trainMovement.trainPath.Count - 1);
            Vector3 averagePreviousDirection = Vector3.zero;

            for (int i = 1; i <= checkRange; i++)
            {
                Vector3 segment = (trainMovement.trainPath[^i] - trainMovement.trainPath[^(i + 1)]).normalized;
                averagePreviousDirection += segment;
            }

            averagePreviousDirection.Normalize();

            // **Yön değişimini analiz et**
            float angle = Vector3.Angle(averagePreviousDirection, direction);
            isMovingBackwards = angle > 90f;
            trainMovement.isMovingBackwards = isMovingBackwards;

            // **Dönüş noktasında ters dönüşü yap**
            if (isMovingBackwards)
            {
                // Eğer dönüş noktası 90 derece ise, ters rotasyonu uygula
                if (Mathf.Abs(Mathf.Abs(Vector3.Angle(previousDirection, direction) - 90f)) < 20f)
                {
                    transform.rotation = Quaternion.Lerp(transform.rotation,
                        Quaternion.LookRotation(-previousDirection), moveSpeed * Time.deltaTime);
                }
                else
                {
                    transform.rotation = Quaternion.Lerp(transform.rotation, Quaternion.LookRotation(-direction),
                        moveSpeed * Time.deltaTime);
                }
            }
            else
            {
                transform.rotation = Quaternion.Lerp(transform.rotation, Quaternion.LookRotation(direction),
                    moveSpeed * Time.deltaTime);
            }

            // **Önceki yönü güncelle**
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
            if (pathQueue.Count > 0)
            {
                Vector2Int nextGridCell = pathQueue.Dequeue();
                movementTarget = GetWorldPositionFromGrid(nextGridCell);
                currentGridCell = nextGridCell;
                isMoving = true;
                trainMovement.isTrainMoving = true;
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
    }
}