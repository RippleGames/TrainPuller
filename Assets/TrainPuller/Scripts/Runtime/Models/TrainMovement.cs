using System.Collections.Generic;
using TemplateProject.Scripts.Data;
using TrainPuller.Scripts.Data;
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
        public bool isTrainMoving;
        private Vector3 _lastCartPrevPosition;
        private float _lastCartBackwardTimer;
        public float backwardVelocityThreshold = 0.1f;
        public float backwardTimeThreshold = 0.5f;

        private void Start()
        {
            SetupCarts();
        }

        private void SetupCarts()
        {
            var levelContainer = FindObjectOfType<LevelContainer>();
            foreach (var cart in carts)
            {
                cart.gridBases = levelContainer.GetGridBases();
            }
        }

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
            var lastCart = carts[^1];
            var currentPos = lastCart.transform.position;

            if (_lastCartPrevPosition == Vector3.zero)
            {
                _lastCartPrevPosition = currentPos;
                return;
            }

            var velocity = (currentPos - _lastCartPrevPosition) / Time.deltaTime;
            _lastCartPrevPosition = currentPos;

            if (isMovingBackwards)
            {
                if (velocity.magnitude < backwardVelocityThreshold)
                {
                    _lastCartBackwardTimer += Time.deltaTime;
                    if (!(_lastCartBackwardTimer > backwardTimeThreshold)) return;
                    isTrainMoving = false;
                    foreach (var cart in carts)
                    {
                        cart.isMoving = false;
                    }

                    isMovingBackwards = false;
                }
                else
                {
                    _lastCartBackwardTimer = 0f;
                }
            }
            else
            {
                _lastCartBackwardTimer = 0f;
            }
        }


        private void UpdateTrainPath()
        {
            if (carts.Count == 0) return;
            var leaderPosition = carts[0].transform.position;

            if (!isMovingBackwards)
            {
                if (trainPath.Count == 0 || Vector3.Distance(leaderPosition, trainPath[^1]) > 0.01f)
                {
                    trainPath.Add(leaderPosition);
                }
            }
            else
            {
                var gap = Vector3.Distance(carts[0].transform.position, carts[1].transform.position);
                if (!(gap < cartSpacing)) return;
                if (Vector3.Distance(carts[0].transform.position, trainPath[^1]) > 0.01f && trainPath.Count > 1)
                {
                    trainPath.RemoveAt(trainPath.Count - 1);
                }
            }
        }


        private void FollowLeader(CartScript follower, CartScript leader, int index)
        {
            var targetDistance = index * cartSpacing;
            var accumulatedDistance = 0f;
            var targetPosition = follower.transform.position;
            if (isMovingBackwards)
            {
                for (var i = trainPath.Count - 1; i > 0; i--)
                {
                    var segmentDistance = Vector3.Distance(trainPath[i], trainPath[i - 1]);
                    accumulatedDistance += segmentDistance;

                    if (!(accumulatedDistance >= targetDistance)) continue;
                    var overshoot = accumulatedDistance - targetDistance;
                    var direction = (trainPath[i - 1] - trainPath[i]).normalized;
                    targetPosition = trainPath[i] + direction * (segmentDistance - overshoot);
                    break;
                }
            }
            else
            {
                for (var i = trainPath.Count - 1; i > 0; i--)
                {
                    var segmentDistance = Vector3.Distance(trainPath[i], trainPath[i - 1]);
                    accumulatedDistance += segmentDistance;

                    if (!(accumulatedDistance >= targetDistance) && trainPath.Count >= 10) continue;
                    var overshoot = accumulatedDistance - targetDistance;
                    var direction = (trainPath[i - 1] - trainPath[i]).normalized;
                    targetPosition = trainPath[i] + direction * (segmentDistance - overshoot);
                    break;
                }
            }

            follower.transform.position = targetPosition;
            UpdateFollowerRotation(follower, leader.transform.position);
        }

        private void UpdateFollowerRotation(CartScript follower, Vector3 targetPosition)
        {
            var followerPosition = follower.transform.position;
            var direction = ((targetPosition + followerPosition) / 2 - followerPosition).normalized;

            if (direction.magnitude < 0.01f)
            {
                return;
            }

            var targetRotation = Quaternion.LookRotation(direction);

            var angleDifference = Quaternion.Angle(follower.transform.rotation, targetRotation);

            if (angleDifference > 170f)
            {
                follower.isMovingBackwards = true;
                targetRotation *= Quaternion.Euler(0, 180, 0);
            }
            else
            {
                follower.isMovingBackwards = false;
            }

            follower.transform.rotation = Quaternion.Slerp(
                follower.transform.rotation,
                targetRotation,
                speed * Time.fixedDeltaTime
            );
        }


        public void MakeLeader(CartScript selectedCart)
        {
            if (!carts.Contains(selectedCart)) return;
            if (currentLeader && trainPath.Count > 0)
            {
                trainPath.Clear();
            }

            carts.Reverse();
            currentLeader = carts[0];
        }
    }
}