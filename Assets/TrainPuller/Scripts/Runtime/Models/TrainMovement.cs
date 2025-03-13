using System;
using System.Collections.Generic;
using UnityEngine;
using TemplateProject.Scripts.Data;
using TemplateProject.Scripts.Runtime.Models;
using TrainPuller.Scripts.Runtime.LevelCreation;

namespace TrainPuller.Scripts.Runtime.Models
{
    public class TrainMovement : MonoBehaviour
    {
        public float speed = 2f;
        public float cartSpacing = 1f;
        public List<CartScript> carts = new List<CartScript>();
        public LevelData.GridColorType cartsColor;
        private Dictionary<CartScript, Vector2Int> cartGridPositions = new Dictionary<CartScript, Vector2Int>();
        private HashSet<Vector2Int> trailPositions;

        private void Start()
        {
            InitializeCartPositions(FindObjectOfType<LevelContainer>().GetGridBases());
        }

        private void Update()
        {
            MoveTrain();
        }

        public void InitializeCartPositions(GridBase[,] gridBases)
        {
            trailPositions = GetTrailPositions(gridBases);
            cartGridPositions.Clear();
            
            foreach (var cart in carts)
            {
                Vector2Int gridPos = cart.currentGridCell;
                if (trailPositions.Contains(gridPos))
                {
                    cartGridPositions[cart] = gridPos;
                }
                else
                {
                    Debug.LogWarning($"Cart {cart.name} is not on a Trail! Assigning closest valid position.");
                    cartGridPositions[cart] = FindClosestTrailPosition(gridPos);
                }
            }
        }

        private void MoveTrain()
        {
            if (carts.Count == 0) return;

            for (int i = 1; i < carts.Count; i++)
            {
                CartScript leader = carts[i - 1];
                CartScript follower = carts[i];

                if (!cartGridPositions.ContainsKey(leader)) continue;

                Vector2Int leaderGridPos = cartGridPositions[leader];
                Vector2Int targetPos = GetNextValidPosition(leaderGridPos);

                if (trailPositions.Contains(targetPos))
                {
                    cartGridPositions[follower] = targetPos;
                    MoveCartSmoothly(follower, targetPos);
                }
            }
        }

        private Vector2Int GetNextValidPosition(Vector2Int leaderPos)
        {
            return trailPositions.Contains(leaderPos) ? leaderPos : leaderPos;
        }

        private void MoveCartSmoothly(CartScript cart, Vector2Int targetGridPos)
        {
            Vector3 worldTargetPos = GetWorldPositionFromGrid(targetGridPos);
            cart.transform.position = Vector3.Lerp(cart.transform.position, worldTargetPos, Time.deltaTime * speed);
            cart.currentGridCell = targetGridPos;
        }

        private Vector3 GetWorldPositionFromGrid(Vector2Int gridPos)
        {
            var gridBase = FindObjectOfType<LevelContainer>().GetGridBases()[gridPos.x, gridPos.y];
            return gridBase != null ? gridBase.transform.position : Vector3.zero;
        }

        public HashSet<Vector2Int> GetTrailPositions(GridBase[,] gridBases)
        {
            HashSet<Vector2Int> trailCells = new HashSet<Vector2Int>();
            for (var x = 0; x < gridBases.GetLength(0); x++)
            {
                for (var y = 0; y < gridBases.GetLength(1); y++)
                {
                    if (gridBases[x, y].isTrail)
                    {
                        trailCells.Add(new Vector2Int(x, y));
                    }
                }
            }
            return trailCells;
        }

        private Vector2Int FindClosestTrailPosition(Vector2Int startPos)
        {
            Vector2Int closest = startPos;
            float minDistance = float.MaxValue;
            
            foreach (var trailPos in trailPositions)
            {
                float distance = Vector2Int.Distance(startPos, trailPos);
                if (distance < minDistance)
                {
                    minDistance = distance;
                    closest = trailPos;
                }
            }
            return closest;
        }

        public void MakeLeader(CartScript selectedCart)
        {
            if (carts.Count == 0 || carts[0] == selectedCart) return;
            if (!carts.Contains(selectedCart)) return;

            carts.Remove(selectedCart);
            carts.Insert(0, selectedCart);
        }
    }
}
