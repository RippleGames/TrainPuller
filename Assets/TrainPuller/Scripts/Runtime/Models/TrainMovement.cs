using System.Collections.Generic;
using TemplateProject.Scripts.Data;
using TemplateProject.Scripts.Runtime.Models;
using TrainPuller.Scripts.Runtime.Managers;
using UnityEngine;

namespace TrainPuller.Scripts.Runtime.Models
{
    public class TrainMovement : MonoBehaviour
    {
        public float speed = 2f; // Trenin hareket hızı
        public float cartSpacing = 1f; // Kartlar arası mesafe
        public List<CartScript> carts = new List<CartScript>(); // Tüm kartlar
        public LevelData.GridColorType cartsColor; // Kartların rengi

        private Dictionary<CartScript, Vector2Int>
            cartGridPositions = new Dictionary<CartScript, Vector2Int>(); // Kartların grid pozisyonları

        private HashSet<Vector2Int> trailPositions; // Trail hücreleri
        [SerializeField] private InteractionManager interactionManager;

        private void Start()
        {
            InitializeCartPositions();
        }

        private void Update()
        {
            MoveTrain();
        }

        public void InitializeCartPositions()
        {
            trailPositions = interactionManager.GetTrailPositions();
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

                FollowLeader(leader, follower);
            }
        }

        private Vector2Int GetNextValidPosition(Vector2Int leaderPos)
        {
            return trailPositions.Contains(leaderPos) ? leaderPos : leaderPos;
        }

        private void FollowLeader(CartScript leaderCart, CartScript thisCart)
        {
            // Liderin geçtiği pozisyonları al
            List<Vector3> leaderPath = leaderCart.GetPathPositions();

            // Takipçi kartın hedef pozisyonunu hesapla
            Vector3 targetPosition = GetTargetPositionOnPath(leaderPath);

            // Hedef pozisyonun Trail hücresi içinde olup olmadığını kontrol et
            if (IsPositionOnTrail(targetPosition))
            {
                thisCart.AddToPath(interactionManager.GetNearestGridCell(targetPosition, true));
                // // Takipçi kartı hedef pozisyona doğru hareket ettir
                // thisCart.transform.position = Vector3.Lerp(thisCart.transform.position, targetPosition,
                //     speed * Time.deltaTime);
                //
                // // Takipçi kartın rotasyonunu hedef pozisyona doğru yumuşakça döndür
                // Vector3 direction = (targetPosition - thisCart.transform.position).normalized;
                // if (direction != Vector3.zero)
                // {
                //     Quaternion targetRotation = Quaternion.LookRotation(direction);
                //     thisCart.transform.rotation = Quaternion.Slerp(thisCart.transform.rotation, targetRotation,
                //         speed * Time.deltaTime);
                // }
            }
        }

        private Vector3 GetTargetPositionOnPath(List<Vector3> leaderPath)
        {
            // Liderin yolundaki pozisyonları kullanarak takipçi kartın hedef pozisyonunu hesapla
            float accumulatedDistance = 0f;
            for (int i = leaderPath.Count - 1; i > 0; i--)
            {
                float segmentDistance = Vector3.Distance(leaderPath[i], leaderPath[i - 1]);
                accumulatedDistance += segmentDistance;

                // Eğer birikmiş mesafe followDistance'ı aştıysa, hedef pozisyonu bul
                if (accumulatedDistance >= cartSpacing)
                {
                    float overshoot = accumulatedDistance - cartSpacing;
                    Vector3 direction = (leaderPath[i - 1] - leaderPath[i]).normalized;
                    return leaderPath[i] + direction * (segmentDistance - overshoot);
                }
            }

            if (leaderPath.Count <= 0)
            {
                return Vector3.zero;
            }

            // Eğer followDistance kadar yol yoksa, liderin ilk pozisyonunu döndür
            return leaderPath[0];
        }

        private bool IsPositionOnTrail(Vector3 position)
        {
            // Pozisyonun en yakın grid hücresini bul
            Vector2Int nearestGridPos = interactionManager.GetNearestGridCell(position, true);

            // Eğer bu pozisyon Trail hücresiyse true döndür
            return interactionManager.trailPositions.Contains(nearestGridPos);
        }

        private Vector3 GetWorldPositionFromGrid(Vector2Int gridPos)
        {
            var gridBase = FindObjectOfType<LevelContainer>().GetGridBases()[gridPos.x, gridPos.y];
            return gridBase != null ? gridBase.transform.position : Vector3.zero;
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