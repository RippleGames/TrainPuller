using System.Collections.Generic;
using TrainPuller.Scripts.Runtime.Managers;
using UnityEngine;

namespace TrainPuller.Scripts.Runtime.Models
{
    public class CartFollower : MonoBehaviour
    {
        [SerializeField] private CartScript leaderCart; // Lider kart
        [SerializeField] private CartScript thisCart; // Bu kart (takipçi)
        [SerializeField] private float followSpeed = 5f; // Takip hızı
        [SerializeField] private float followDistance = 1f; // Liderden uzaklık
        [SerializeField] private InteractionManager interactionManager; // InteractionManager referansı

        private void Update()
        {
            if (leaderCart != null)
            {
                FollowLeader();
            }
        }

        public void SetLeaderCart(CartScript leader)
        {
            leaderCart = leader; // Lider kartı ayarla
        }

        private void FollowLeader()
        {
            // Liderin geçtiği pozisyonları al
            List<Vector3> leaderPath = leaderCart.GetPathPositions();

            // Takipçi kartın hedef pozisyonunu hesapla
            Vector3 targetPosition = GetTargetPositionOnPath(leaderPath);

            // Hedef pozisyonun Trail hücresi içinde olup olmadığını kontrol et
            if (IsPositionOnTrail(targetPosition))
            {
                // Takipçi kartı hedef pozisyona doğru hareket ettir
                thisCart.transform.position = Vector3.Lerp(thisCart.transform.position, targetPosition,
                    followSpeed * Time.deltaTime);

                // Takipçi kartın rotasyonunu hedef pozisyona doğru yumuşakça döndür
                Vector3 direction = (targetPosition - thisCart.transform.position).normalized;
                if (direction != Vector3.zero)
                {
                    Quaternion targetRotation = Quaternion.LookRotation(direction);
                    thisCart.transform.rotation = Quaternion.Slerp(thisCart.transform.rotation, targetRotation,
                        followSpeed * Time.deltaTime);
                }
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
                if (accumulatedDistance >= followDistance)
                {
                    float overshoot = accumulatedDistance - followDistance;
                    Vector3 direction = (leaderPath[i - 1] - leaderPath[i]).normalized;
                    return leaderPath[i] + direction * (segmentDistance - overshoot);
                }
            }

            // Eğer followDistance kadar yol yoksa, liderin ilk pozisyonunu döndür
            return leaderPath[0];
        }

        private bool IsPositionOnTrail(Vector3 position)
        {
            // Pozisyonun en yakın grid hücresini bul
            Vector2Int nearestGridPos = interactionManager.GetNearestGridCell(position,true);

            // Eğer bu pozisyon Trail hücresiyse true döndür
            return interactionManager.trailPositions.Contains(nearestGridPos);
        }
    }
}