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
        private int movementDirection = 1; // 1 = forward, -1 = backward
        private Dictionary<CartScript, Vector2> cartGridPositions = new Dictionary<CartScript, Vector2>();
        private HashSet<Vector2> trailPositions;

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
                Vector2Int gridPos = GetGridPosition(cart.transform.position);
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

                Vector2 leaderGridPos = cartGridPositions[leader];
                Vector2 targetPos = GetNextValidPosition(leaderGridPos);

                if (trailPositions.Contains(targetPos))
                {
                    cartGridPositions[follower] = targetPos;
                    MoveCartSmoothly(follower, targetPos);
                }
            }
        }

        private Vector2Int GetGridPosition(Vector3 worldPos)
        {
            return new Vector2Int(Mathf.RoundToInt(worldPos.x), Mathf.RoundToInt(worldPos.z));
        }

        private Vector2 GetNextValidPosition(Vector2 leaderPos)
        {
            Vector2 targetPos = leaderPos;
            return trailPositions.Contains(targetPos) ? targetPos : leaderPos;
        }

        private void MoveCartSmoothly(CartScript cart, Vector2 targetPos)
        {
            Vector3 worldTargetPos = new Vector3(targetPos.x, cart.transform.position.y, targetPos.y);
            cart.transform.position = Vector3.Lerp(cart.transform.position, worldTargetPos, Time.deltaTime * speed);
        }

        public HashSet<Vector2> GetTrailPositions(GridBase[,] gridBases)
        {
            HashSet<Vector2> trailCells = new HashSet<Vector2>();
            for (var x = 0; x < gridBases.GetLength(0); x++)
            {
                for (var y = 0; y < gridBases.GetLength(1); y++)
                {
                    if (gridBases[x, y].isTrail)
                    {
                        trailCells.Add(new Vector2(gridBases[x,y].transform.position.x,gridBases[x,y].transform.position.z));
                    }
                }
            }
            return trailCells;
        }

        private Vector2 FindClosestTrailPosition(Vector2Int startPos)
        {
            Vector2 closest = startPos;
            float minDistance = float.MaxValue;
            
            foreach (var trailPos in trailPositions)
            {
                float distance = Vector2.Distance(startPos, trailPos);
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

            movementDirection *= -1;
            carts.Remove(selectedCart);
            carts.Reverse();
            carts.Insert(0, selectedCart);
        }
    }
}