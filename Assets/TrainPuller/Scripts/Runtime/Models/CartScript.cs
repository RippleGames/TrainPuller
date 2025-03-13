using System.Collections.Generic;
using FluffyUnderware.Curvy.Controllers;
using TemplateProject.Scripts.Data;
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

        private bool isMoving = false;
        private float moveSpeed = 10f;
        private Queue<Vector2Int> pathQueue = new Queue<Vector2Int>();
        private Vector3 movementTarget;

        private void Update()
        {
            if (isMoving)
            {
                MoveCart();
            }
        }

        public void AddToPath(Vector2Int newGridCell, Vector3 worldPos)
        {
            if (!pathQueue.Contains(newGridCell))
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
                isMoving = false;
                MoveToNextGridCell();
            }
        }

        private Vector3 GetWorldPositionFromGrid(Vector2Int gridPos)
        {
            var gridBase = FindObjectOfType<LevelContainer>().GetGridBases()[gridPos.x, gridPos.y];
            return gridBase != null ? gridBase.transform.position : Vector3.zero;
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