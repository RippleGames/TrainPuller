using System.Collections.Generic;
using System.Linq;
using DG.Tweening;
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
        public TrainContainer trainContainer;
        public LevelData.GridColorType cartsColor;
        [SerializeField] public CartScript currentLeader;
        public CartScript previousLeader;
        [SerializeField] public CartScript currentLastCart;
        [SerializeField] public List<Vector3> trainPath = new List<Vector3>();
        public bool isMovingBackwards;
        public bool canMoveBackwards = true;
        public bool canMoveForward = true;
        public bool isTrainMoving;
        private Vector3 _lastCartPrevPosition;
        private float _lastCartBackwardTimer;
        private float stopTimer;
        private float stopDelay = 0.5f;
        private bool _isLeaderChanged = false;
        private bool isLeaderChanging = false;
        private Vector3 lastCartPrevPosition;
        private float positionThreshold = 0.01f;

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

            DetectMovementDirection();
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


        private void DetectMovementDirection()
        {
            if (isLeaderChanging) return;
            if (!currentLeader) return;
            if (trainPath.Count < 2) return;

            var targetPosition = currentLeader.interactionManager.GetProjectedMousePositionOnTrail();
            var movementDirection = (targetPosition - currentLeader.transform.position).normalized;

            if (movementDirection.magnitude < 0.01f) return;

            var lastPosition = trainPath[^1];
            var secondLastPosition = trainPath[^2];

            var previousPathDirection = (lastPosition - secondLastPosition).normalized;

            if (_isLeaderChanged)
            {
                var dotWithPreviousLeader = -1f * Vector3.Dot(previousPathDirection, movementDirection);

                isMovingBackwards = dotWithPreviousLeader < 0;
                _isLeaderChanged = false;
            }
            else
            {
                float dot = Vector3.Dot(previousPathDirection, movementDirection);
                isMovingBackwards = dot < 0;
            }

            if (!canMoveForward && isMovingBackwards)
            {
                canMoveForward = true;
                isTrainMoving = true;
                foreach (var cart in carts)
                {
                    cart.isMoving = true;
                }
            }

            if (!canMoveBackwards && !isMovingBackwards)
            {
                canMoveBackwards = true;
                isTrainMoving = true;
                foreach (var cart in carts)
                {
                    cart.isMoving = true;
                }
            }
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
                    LayerMask.GetMask("TrainCartLayer") | LayerMask.GetMask("ObstacleLayer")))
            {
                if (hit.collider.CompareTag("TrainCart") || hit.collider.CompareTag("Exit"))
                {
                    if (hit.collider.TryGetComponent(out CartScript cart))
                    {
                        if (cart.trainMovement != this)
                        {
                            return true;
                        }
                    }

                    if (hit.collider.TryGetComponent(out ExitBarrierScript exitBarrierScript))
                    {
                        if (trainContainer.isAllFull)
                        {
                            GetOutFromExit(exitBarrierScript);
                            return !trainContainer.isAllFull;
                        }

                        return !trainContainer.isAllFull;
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
                if (hit.collider.CompareTag("TrainCart") || hit.collider.CompareTag("Exit"))
                {
                    if (hit.collider.TryGetComponent(out CartScript cart))
                    {
                        if (cart.trainMovement != this)
                        {
                            return true;
                        }
                    }

                    if (hit.collider.TryGetComponent(out ExitBarrierScript exitBarrierScript))
                    {
                        if (trainContainer.isAllFull)
                        {
                            GetOutFromExit(exitBarrierScript);
                            return trainContainer.isAllFull;
                        }

                        return !trainContainer.isAllFull;
                    }
                }
            }

            return false;
        }


       private void CheckLastCartMovement()
{
    if (carts.Count < 2) return;

    CartScript lastCart = carts[^1];
    CartScript secondLastCart = carts[^2];

    if (!isMovingBackwards || lastCartPrevPosition == Vector3.zero)
    {
        lastCartPrevPosition = lastCart.transform.position;
        return;
    }

    float distanceMoved = Vector3.Distance(lastCart.transform.position, lastCartPrevPosition);

    // **Son vagonun geri gidebileceği en yakın noktayı bul**
    Vector3 lastCartBackwardDirection = -lastCart.transform.forward;
    Vector3 bestBackwardPoint = Vector3.zero;
    bool hasMorePathToMove = false;
    float closestValidDistance = float.MaxValue;

    foreach (Vector3 pathPoint in trainPath)
    {
        Vector3 directionToPoint = (pathPoint - lastCart.transform.position).normalized;
        float dot = Vector3.Dot(lastCartBackwardDirection, directionToPoint);
        float dist = Vector3.Distance(lastCart.transform.position, pathPoint);

        // Eğer nokta gerçekten geri yöndeyse ve en yakın mesafedeyse, onu seç
        if (dot > 0.25f && dist < closestValidDistance && dist > positionThreshold)
        {
            closestValidDistance = dist;
            bestBackwardPoint = pathPoint;
            hasMorePathToMove = true;
        }
    }

    // **Son vagon ile sondan bir önceki vagon arasındaki mesafeyi kontrol et**
    float lastCartDistance = Vector3.Distance(lastCart.transform.position, secondLastCart.transform.position);

    // **Treni durdurma koşullarını esnetiyoruz**
    if (!hasMorePathToMove) 
    {
        // Eğer geri gidilecek nokta yoksa, tren durmalı
        canMoveBackwards = false;
        isTrainMoving = false;
    } 
    else if (lastCartDistance < cartSpacing * 0.9f) 
    {
        // Eğer vagonlar iç içe girmeye başlıyorsa, dur
        canMoveBackwards = false;
        isTrainMoving = false;
    } 
    else 
    {
        // Eğer geri gidilecek nokta varsa ve mesafe uygunsa, hareket devam etsin
        canMoveBackwards = true;
    }

    lastCartPrevPosition = lastCart.transform.position;
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

            if (trainPath.Count > 300)
            {
                trainPath.RemoveAt(0);
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

                    if (accumulatedDistance >= targetDistance)
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
            isLeaderChanging = true;
            if (!carts.Contains(selectedCart)) return;
            if (selectedCart == carts[0])
            {
                currentLeader = carts[0];
                previousLeader = currentLeader;
                currentLastCart = carts[^1];
                isLeaderChanging = false;
                return;
            }

            if (trainPath.Count < 2)
            {
                carts.Reverse();
                currentLeader = carts[0];
                currentLastCart = carts[^1];
                isLeaderChanging = false;
                return;
            }


            previousLeader = carts[0];
            carts.Reverse();
            currentLeader = carts[0];
            currentLastCart = carts[^1];
            _isLeaderChanged = !_isLeaderChanged;
            isLeaderChanging = false;
        }

        private void GetOutFromExit(ExitBarrierScript exitBarrierScript)
        {
            currentLeader.interactionManager.HandleExit();
            transform.SetParent(null);
            isTrainMoving = true;
            canMoveBackwards = true;
            canMoveForward = true;
            currentLeader.gameObject.transform.DOMove(exitBarrierScript.GetPathEnd().position, 2f).OnComplete(() =>
            {
                transform.gameObject.SetActive(false);
            });
        }
    }
}