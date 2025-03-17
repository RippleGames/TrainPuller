using System.Collections.Generic;
using TemplateProject.Scripts.Data;
using TrainPuller.Scripts.Runtime.Managers;
using UnityEngine;

namespace TrainPuller.Scripts.Runtime.Models
{
    public class TrainMovement : MonoBehaviour
    {
        public float speed = 5f;
        public float cartSpacing = 1f;
        public List<CartScript> carts = new List<CartScript>();
        public LevelData.GridColorType cartsColor;
        [SerializeField] private InteractionManager interactionManager;
        [SerializeField] private CartScript currentLeader;

        private void Update()
        {
            if (carts.Count == 0 || !interactionManager.GetCurrentlySelectedCart()) return;
            if (!currentLeader) return;
            if (!currentLeader.isMoving) return;
            // Tüm takipçi kartları güncelle
            for (int i = 1; i < carts.Count; i++)
            {
                CartScript leader = carts[i - 1];
                CartScript follower = carts[i];
                FollowLeader(leader, follower);
            }
        }

        private void FollowLeader(CartScript leader, CartScript follower)
        {
            // Liderin yolunu al
            List<Vector3> leaderPath = leader.GetPathPositions();
            if (leaderPath.Count == 0) return;

            // Takipçinin hedef pozisyonunu hesapla
            Vector3 targetPosition = CalculateTargetPosition(leaderPath, follower, cartSpacing);
            // Pozisyon Trail üzerindeyse hareket ettir
            if (interactionManager.IsPositionOnTrail(targetPosition) &&
                Vector3.Distance(leader.transform.position, follower.transform.position) >= cartSpacing)
            {
                // Hedef pozisyona doğru hareket
                follower.transform.position = targetPosition;

                // Rotasyonu güncelle
                UpdateFollowerRotation(follower, leader.transform.position);

                // Takipçinin grid pozisyonunu güncelle
                Vector2Int nearestGrid = interactionManager.GetNearestGridCell(targetPosition, true);
                follower.currentGridCell = nearestGrid;
                follower.UpdatePath(targetPosition);
            }
        }

        private Vector3 CalculateTargetPosition(List<Vector3> leaderPath, CartScript follower, float spacing)
        {
            float accumulatedDistance = 0f;
            for (int i = leaderPath.Count - 1; i > 0; i--)
            {
                Vector3 currentPoint = leaderPath[i];
                Vector3 nextPoint = leaderPath[i - 1];
                float segmentDistance = Vector3.Distance(currentPoint, nextPoint);

                accumulatedDistance += segmentDistance;

                // Eğer istenen mesafe aşıldıysa, hedef pozisyonu bul
                if (accumulatedDistance >= spacing)
                {
                    float overshoot = accumulatedDistance - spacing;
                    Vector3 direction = (nextPoint - currentPoint).normalized;
                    return currentPoint + direction * (segmentDistance - overshoot);
                }
            }

            // Debug.Log("Path 0 pos");
            return follower.transform.position;
        }

        private void UpdateFollowerRotation(CartScript follower, Vector3 targetPosition)
        {
            Vector3 direction = (targetPosition - follower.transform.position).normalized;
            if (direction != Vector3.zero)
            {
                Quaternion targetRotation = Quaternion.LookRotation(direction);
                follower.transform.rotation = Quaternion.Slerp(
                    follower.transform.rotation,
                    targetRotation,
                    speed * Time.fixedDeltaTime
                );
            }
        }

        public void MakeLeader(CartScript selectedCart)
        {
            if (!carts.Contains(selectedCart)) return;
            carts.Remove(selectedCart);
            carts.Insert(0, selectedCart);
            currentLeader = selectedCart;
        }
    }
}