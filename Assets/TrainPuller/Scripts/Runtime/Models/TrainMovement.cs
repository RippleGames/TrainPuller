using System.Collections.Generic;
using System.Linq;
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
        [SerializeField] private CartScript currentLastCart;
        [SerializeField] public List<Vector3> trainPath = new List<Vector3>();
        public bool isMovingBackwards;
        public bool canMoveBackwards = true;
        public bool canMoveForward = true;
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
            if (carts.Count == 0) return;

            HandleObstacles();

            if (!isTrainMoving) return;

            if (!currentLeader)
            {
                currentLeader = carts[0];
            }

            UpdateTrainPath();

            for (var i = 1; i < carts.Count; i++)
            {
                FollowLeader(carts[i], carts[i - 1], i);
            }

            CheckLastCartMovement();
        }

        private void HandleObstacles()
        {
            if (!isMovingBackwards)
            {
                if (!CheckForObstaclesForward()) return;
                isTrainMoving = false;
                canMoveForward = false;
            }
            else
            {
                if (!CheckForObstaclesBackwards()) return;
                isTrainMoving = false;
                canMoveBackwards = false;
            }
        }

        private bool CheckForObstaclesForward()
        {
            if (!currentLeader) return false;
            if (isMovingBackwards) return false;
            var checkDistance = 0.5f;
            var leaderTransform = currentLeader.transform;
            var leaderPosition = leaderTransform.position;
            var direction = leaderTransform.forward;
            if (Physics.Raycast(leaderPosition + new Vector3(0f, 0.5f, 0f), direction, out var hit, checkDistance,
                    LayerMask.GetMask("TrainCartLayer")))
            {
                if (hit.collider.CompareTag("TrainCart"))
                {
                    if (hit.collider.TryGetComponent(out CartScript cart))
                    {
                        if (cart.trainMovement != this)
                        {
                            return true;
                        }
                    }
                }
            }

            return false;
        }

        private bool CheckForObstaclesBackwards()
        {
            if (!currentLastCart) return false;
            if (!isMovingBackwards) return false;
            var checkDistance = 0.5f;
            var lastCartTransform = currentLastCart.transform;
            var leaderPosition = lastCartTransform.position;
            var direction = -lastCartTransform.forward;
            if (Physics.Raycast(leaderPosition + new Vector3(0f, 0.5f, 0f), direction, out var hit, checkDistance))
            {
                if (hit.collider.CompareTag("TrainCart"))
                {
                    if (hit.collider.TryGetComponent(out CartScript cart))
                    {
                        if (cart.trainMovement != this)
                        {
                            return true;
                        }
                    }
                }
            }

            return false;
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
                    canMoveBackwards = false;
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
                if (trainPath.Count > 1)
                {
                    if (Vector3.Distance(carts[0].transform.position, trainPath[^1]) > 0.01f)
                    {
                        trainPath.RemoveAt(trainPath.Count - 1);
                    }
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

                    if (accumulatedDistance >= targetDistance)
                    {
                        var overshoot = accumulatedDistance - targetDistance;
                        var direction = (trainPath[i - 1] - trainPath[i]).normalized;
                        targetPosition = trainPath[i] + direction * (segmentDistance - overshoot);
                        break;
                    }
                }
            }
            else
            {
                for (var i = trainPath.Count - 1; i > 0; i--)
                {
                    var segmentDistance = Vector3.Distance(trainPath[i], trainPath[i - 1]);
                    accumulatedDistance += segmentDistance;

                    if (accumulatedDistance >= targetDistance || trainPath.Count is > 0 and < 50)
                    {
                        var overshoot = accumulatedDistance - targetDistance;
                        var direction = (trainPath[i - 1] - trainPath[i]).normalized;
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
            if (selectedCart == carts[0])
            {
                return;
            }

            var lastPositions = new List<Vector3>();
            if (trainPath.Count > 1)
            {
                lastPositions = trainPath.GetRange(trainPath.Count / 2, trainPath.Count / 2 - 1);
            }

            if (currentLeader && trainPath.Count > 0)
            {
                trainPath.Clear();
                trainPath.AddRange(lastPositions);
            }

            carts.Reverse();
            currentLeader = carts[0];
            currentLastCart = carts[^1];
        }
    }
}