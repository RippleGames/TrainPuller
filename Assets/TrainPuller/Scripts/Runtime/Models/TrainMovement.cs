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
        [SerializeField] private List<Vector3> trainPath = new List<Vector3>();
        public bool isMovingBackwards;

        private void Update()
        {
            if (carts.Count == 0) return;

            if (!currentLeader)
            {
                currentLeader = carts[0];
            }

            UpdateTrainPath();

            for (int i = 1; i < carts.Count; i++)
            {
                FollowLeader(carts[i], i);
            }
        }

        private void UpdateTrainPath()
        {
            if (carts.Count == 0) return;
            Vector3 leaderPosition = carts[0].transform.position;

            if (isMovingBackwards)
            {
                if (trainPath.Count > 0)
                {
                    if (Vector3.Distance(leaderPosition, trainPath[^1]) > 0.09f * cartSpacing)
                    {
                        trainPath.RemoveAt(trainPath.Count - 1);
                    }
                }

                return;
            }

            // Yeni pozisyonu sadece yeterince farklıysa ekle
            if (trainPath.Count == 0 || Vector3.Distance(leaderPosition, trainPath[^1]) > 0.01f)
            {
                trainPath.Add(leaderPosition);
            }

            // Aşırı büyümeyi engelle
            if (trainPath.Count > 300)
            {
                trainPath.RemoveAt(0);
            }
        }

        private void FollowLeader(CartScript follower, int index)
        {
            float targetDistance = index * cartSpacing;
            float accumulatedDistance = 0f;
            Vector3 targetPosition = follower.transform.position;
            if (isMovingBackwards)
            {
                for (int i = trainPath.Count - 1; i > 0; i--)
                {
                    float segmentDistance = Vector3.Distance(trainPath[i], trainPath[i - 1]);
                    accumulatedDistance += segmentDistance;

                    if (accumulatedDistance >= targetDistance)
                    {
                        float overshoot = accumulatedDistance - targetDistance;
                        Vector3 direction = (trainPath[i - 1] - trainPath[i]).normalized;
                        targetPosition = trainPath[i] + direction * overshoot;

                        break;
                    }
                }
            }
            else
            {
                for (int i = trainPath.Count - 1; i > 0; i--)
                {
                    float segmentDistance = Vector3.Distance(trainPath[i], trainPath[i - 1]);
                    accumulatedDistance += segmentDistance;

                    if (accumulatedDistance >= targetDistance)
                    {
                        float overshoot = accumulatedDistance - targetDistance;
                        Vector3 direction = (trainPath[i - 1] - trainPath[i]).normalized;
                        targetPosition = trainPath[i] + direction * overshoot;
                        break;
                    }
                }
            }

            follower.transform.position =
                Vector3.Lerp(follower.transform.position, targetPosition, speed * Time.deltaTime);
            UpdateFollowerRotation(follower, targetPosition, carts[0].isMovingBackwards);
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

        private void UpdateFollowerRotation(CartScript follower, Vector3 targetPosition, bool isReversing)
        {
            Vector3 direction = (targetPosition - follower.transform.position).normalized;

            if (direction != Vector3.zero)
            {
                Quaternion targetRotation =
                    isReversing ? Quaternion.LookRotation(-direction) : Quaternion.LookRotation(direction);

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