using System.Collections.Generic;
using TemplateProject.Scripts.Data;
using UnityEngine;

namespace TrainPuller.Scripts.Runtime.Models
{
    public class TrainMovement : MonoBehaviour
    {
        public float speed = 5f;
        public float cartSpacing = 1f;
        public List<CartScript> carts = new List<CartScript>();
        public LevelData.GridColorType cartsColor;
        [SerializeField] private CartScript currentLeader;
        [SerializeField] public List<Vector3> trainPath = new List<Vector3>();
        public bool isMovingBackwards;
        public float maxRemovalSpeed;
        public bool isTrainMoving;
        private Vector3 lastCartPrevPosition;
        private float lastCartBackwardTimer = 0f;
        public float backwardVelocityThreshold = 0.1f; // Geri yönde minimum hız (birim/saniye)
        public float backwardTimeThreshold = 0.5f;

        private void Update()
        {
            if (!isTrainMoving) return;
            if (carts.Count == 0) return;

            if (!currentLeader)
            {
                currentLeader = carts[0];
            }

            UpdateTrainPath();

            for (int i = 1; i < carts.Count; i++)
            {
                FollowLeader(carts[i], carts[i - 1], i);
            }

            CheckLastCartMovement();
        }


        private void CheckLastCartMovement()
        {
            CartScript lastCart = carts[carts.Count - 1];
            Vector3 currentPos = lastCart.transform.position;

            if (lastCartPrevPosition == Vector3.zero)
            {
                lastCartPrevPosition = currentPos;
                return;
            }

            Vector3 velocity = (currentPos - lastCartPrevPosition) / Time.deltaTime;
            lastCartPrevPosition = currentPos;

            if (isMovingBackwards)
            {
                if (velocity.magnitude < backwardVelocityThreshold)
                {
                    lastCartBackwardTimer += Time.deltaTime;
                    if (lastCartBackwardTimer > backwardTimeThreshold)
                    {
                        // Son vagon yeterince geri hareket etmiyorsa tüm treni durdur
                        isTrainMoving = false;
                        foreach (CartScript cart in carts)
                        {
                            cart.isMoving = false;
                        }

                        isMovingBackwards = false;
                    }
                }
                else
                {
                    lastCartBackwardTimer = 0f;
                }
            }
            else
            {
                // İleri harekette timer sıfırlansın
                lastCartBackwardTimer = 0f;
            }
        }


        private void UpdateTrainPath()
        {
            if (carts.Count == 0) return;
            Vector3 leaderPosition = carts[0].transform.position;

            if (!isMovingBackwards)
            {
                if (trainPath.Count == 0 || Vector3.Distance(leaderPosition, trainPath[^1]) > 0.01f)
                {
                    trainPath.Add(leaderPosition);
                }
            }
            else
            {
                float gap = Vector3.Distance(carts[0].transform.position, carts[1].transform.position);
                if (gap < cartSpacing)
                {
                    if (Vector3.Distance(carts[0].transform.position, trainPath[^1]) > 0.01f && trainPath.Count > 1)
                    {
                        trainPath.RemoveAt(trainPath.Count - 1);
                    }
                }
            }
        }


        private void FollowLeader(CartScript follower, CartScript leader, int index)
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
                        targetPosition = trainPath[i] + direction * (segmentDistance - overshoot);
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

                    if (accumulatedDistance >= targetDistance || trainPath.Count < 10)
                    {
                        float overshoot = accumulatedDistance - targetDistance;
                        Vector3 direction = (trainPath[i - 1] - trainPath[i]).normalized;
                        targetPosition = trainPath[i] + direction * (segmentDistance - overshoot);
                        break;
                    }
                }
            }

            follower.transform.position = targetPosition;
            UpdateFollowerRotation(follower, leader.transform.position);
        }

        private void UpdateFollowerRotation(CartScript follower, Vector3 targetPosition)
        {
            var followerPosition = follower.transform.position;
            Vector3 direction = ((targetPosition + followerPosition) / 2 - followerPosition).normalized;

            if (direction.magnitude < 0.01f)
            {
                return;
            }

            Quaternion targetRotation = Quaternion.LookRotation(direction);

            // Eğer follower ile hedef arasındaki açı yaklaşık 180 dereceyse, ters bakıyor demektir
            float angleDifference = Quaternion.Angle(follower.transform.rotation, targetRotation);

            if (angleDifference > 170f) // 180 yerine biraz tolerans bıraktık
            {
                follower.isMovingBackwards = true;
                targetRotation *= Quaternion.Euler(0, 180, 0); // 180 derece çevir
            }
            else
            {
                follower.isMovingBackwards = false;
            }

            // Rotasyonu set et
            follower.transform.rotation = Quaternion.Slerp(
                follower.transform.rotation,
                targetRotation,
                speed * Time.fixedDeltaTime
            );
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