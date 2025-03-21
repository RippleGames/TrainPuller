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
        private float _stopTimer;
        private float _stopDelay = 0.5f;
        private bool _isLeaderChanged = false;
        private bool _isLeaderChanging = false;
        private Vector3 lastCartPrevPosition;
        private float positionThreshold = 0.01f;
        public GameObject backwardsEndPrefab;
        public GameObject backwardsEndObject;
        private bool hasMorePathToMove = true;

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

            HandlePathInitial();


            var backwardsEnd = Instantiate(backwardsEndPrefab, carts[^1].transform.position, Quaternion.identity,
                    transform)
                .GetComponent<BackwardsEnd>();
            backwardsEndObject = backwardsEnd.gameObject;
            backwardsEnd.SetTrainMovement(this);
        }

        private void HandlePathInitial()
        {
            var direction = (carts[0].transform.position - carts[^1].transform.position).normalized;
            var distance = Vector3.Distance(carts[^1].transform.position, carts[0].transform.position);

            for (float d = 0; d <= distance; d += 0.01f)
            {
                trainPath.Add(carts[^1].transform.position + direction * d);
            }

            trainPath.Add(carts[0].transform.position);
        }

        private void Update()
        {
            if (carts.Count == 0) return;
            if (!currentLeader) return;
            if (!currentLeader.interactionManager) return;
            if (!currentLeader.interactionManager.GetCurrentlySelectedCart()) return;
            if (currentLeader.interactionManager.GetCurrentlySelectedCart() != currentLeader)
            {
                if (isTrainMoving)
                {
                    isTrainMoving = false;
                    canMoveForward = true;
                    canMoveBackwards = true;
                    foreach (var cart in carts)
                    {
                        cart.isMoving = false;
                    }
                }

                return;
            }

            if (_isLeaderChanging) return;

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
            if (_isLeaderChanging) return;
            if (!currentLeader || carts.Count < 2) return;
            if (trainPath.Count < 2) return;

            // **Mouse hedef pozisyonunu al ve hareket yönünü hesapla**
            Vector3 targetPosition = currentLeader.interactionManager.GetProjectedMousePositionOnTrail();
            Vector3 movementDirection = (targetPosition - currentLeader.transform.position).normalized;

            if (movementDirection.magnitude < 0.01f) return; // **Eğer hareket yoksa işlem yapma**

            // **TrainPath içindeki son iki noktayı al**
            Vector3 lastPosition = trainPath[^1];
            Vector3 secondLastPosition = trainPath[^2];

            // **Önceki path yönünü hesapla**
            Vector3 previousPathDirection = (lastPosition - secondLastPosition).normalized;

            // **Liderin bir önceki vagonun pozisyonuyla ilişkisini al**
            CartScript previousCart = carts[carts.IndexOf(currentLeader) + 1];
            Vector3 previousCartPosition = previousCart.transform.position;
            Vector3 directionToPreviousCart = (previousCartPosition - currentLeader.transform.position).normalized;
            float dotWithPreviousCart = Vector3.Dot(movementDirection, directionToPreviousCart);

            // **Lider değiştiyse, eski liderin yönünü dikkate alarak daha kesin yön analizi yap**
            if (_isLeaderChanged)
            {
                // **Önceki liderin yönüne göre yeni hareket yönünü değerlendir**
                Vector3 previousLeaderDirection = -previousPathDirection;
                float dotWithPreviousLeader = Vector3.Dot(previousLeaderDirection, movementDirection);

                isMovingBackwards = dotWithPreviousLeader < 0;
                _isLeaderChanged = false;
            }
            else
            {
                // **Normal durumda, mevcut hareket yönü ile önceki path yönünü karşılaştır**
                float dot = Vector3.Dot(previousPathDirection, movementDirection);
                isMovingBackwards = dot < 0;
            }

            // **Lider, bir önceki vagonun olduğu yöne doğru hareket ediyorsa geri gidiyor demektir**
            if (dotWithPreviousCart > 0.8f)
            {
                isMovingBackwards = true;
            }
            else if (dotWithPreviousCart < -0.8f)
            {
                isMovingBackwards = false;
            }

            // **Trenin diğer yöne hareket etmesini sağla**
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


        private void CheckLastCartMovement()
        {
            // if (carts.Count < 2) return;
            // if (!isMovingBackwards) return;
            // if (!isTrainMoving) return;
            // var checkDistance = 0.5f;
            // var lastCart = carts[^1];
            // var secondLastCart = carts[^2];
            //
            //
            // var lastCartTransform = lastCart.transform;
            // var direction = -lastCartTransform.forward;
            //
            // if (Physics.SphereCast(lastCart.transform.position + new Vector3(0f, 0.5f, 0f), 0.5f, direction,
            //         out var hit,
            //         checkDistance))
            // {
            //     if (hit.collider.CompareTag("BackwardsEnd"))
            //     {
            //         if (hit.collider.TryGetComponent(out BackwardsEnd backwardsEnd))
            //         {
            //             if (backwardsEnd.GetTrainMovement() == this)
            //             {
            //                 var lastCartDistance = Vector3.Distance(lastCart.transform.position,
            //                     secondLastCart.transform.position);
            //                 hasMorePathToMove = false;
            //                 if (lastCartDistance < cartSpacing * 0.9f)
            //                 {
            //                     canMoveBackwards = false;
            //                     isTrainMoving = false;
            //                     lastCartPrevPosition = lastCart.transform.position;
            //                     return;
            //                 }
            //
            //                 canMoveBackwards = false;
            //                 isTrainMoving = false;
            //                 lastCartPrevPosition = lastCart.transform.position;
            //                 return;
            //             }
            //
            //             lastCartPrevPosition = lastCart.transform.position;
            //             canMoveBackwards = true;
            //             return;
            //         }
            //     }
            // }
            //
            // if (Physics.SphereCast(lastCart.transform.position + new Vector3(0f, 0.5f, 0f), 0.5f, -direction,
            //         out var hit2,
            //         checkDistance))
            // {
            //     if (hit2.collider.CompareTag("BackwardsEnd"))
            //     {
            //         if (hit2.collider.TryGetComponent(out BackwardsEnd backwardsEnd))
            //         {
            //             if (backwardsEnd.GetTrainMovement() == this)
            //             {
            //                 var lastCartDistance = Vector3.Distance(lastCart.transform.position,
            //                     secondLastCart.transform.position);
            //                 hasMorePathToMove = false;
            //                 if (lastCartDistance < cartSpacing * 0.9f)
            //                 {
            //                     canMoveBackwards = false;
            //                     isTrainMoving = false;
            //                     lastCartPrevPosition = lastCart.transform.position;
            //                     return;
            //                 }
            //
            //                 canMoveBackwards = false;
            //                 isTrainMoving = false;
            //                 lastCartPrevPosition = lastCart.transform.position;
            //                 return;
            //             }
            //
            //             lastCartPrevPosition = lastCart.transform.position;
            //             canMoveBackwards = true;
            //             return;
            //         }
            //     }
            // }
            //
            //
            // canMoveBackwards = true;
            // lastCartPrevPosition = lastCart.transform.position;
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

            if (trainPath.Count <= 500) return;
            trainPath.RemoveAt(0);
            backwardsEndObject.transform.position = trainPath[0];
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

            direction.y = 0;
            var targetRotation = Quaternion.LookRotation(direction);
                
            var angleDifference = Quaternion.Angle(follower.transform.rotation, targetRotation);

            if (angleDifference > 90f)
            {
                targetRotation.eulerAngles = new Vector3(targetRotation.eulerAngles.x,
                    targetRotation.eulerAngles.y - 180f, targetRotation.eulerAngles.z);
            }

            follower.transform.rotation = Quaternion.Slerp(
                follower.transform.rotation,
                targetRotation,
                speed * Time.fixedDeltaTime
            );
        }


        public void MakeLeader(CartScript selectedCart)
        {
            _isLeaderChanging = true;
            if (!carts.Contains(selectedCart)) return;
            if (selectedCart == carts[0])
            {
                currentLeader = carts[0];
                previousLeader = currentLeader;
                currentLastCart = carts[^1];
                backwardsEndObject.transform.position = currentLastCart.transform.position;
                _isLeaderChanging = false;
                return;
            }

            if (trainPath.Count < 2)
            {
                carts.Reverse();
                currentLeader = carts[0];
                currentLastCart = carts[^1];
                trainPath.Clear();
                HandlePathInitial();
                backwardsEndObject.transform.position = currentLastCart.transform.position;
                _isLeaderChanging = false;
                return;
            }


            previousLeader = carts[0];
            carts.Reverse();
            currentLeader = carts[0];
            currentLastCart = carts[^1];
            trainPath.Clear();
            HandlePathInitial();
            backwardsEndObject.transform.position = currentLastCart.transform.position;
            _isLeaderChanged = !_isLeaderChanged;
            _isLeaderChanging = false;
        }

        public void GetOutFromExit(ExitBarrierScript exitBarrierScript)
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

        public void StopMovement()
        {
            isMovingBackwards = false;
            isTrainMoving = false;
        }

        public void HandleBackCollision()
        {
            isTrainMoving = false;
            canMoveBackwards = false;
        }

        public void HandleFrontCollision()
        {
            isTrainMoving = false;
            canMoveForward = false;
        }

        public void HandleBackwardsMovement()
        { 
            
            if (carts.Count < 2) return;
            if (!isMovingBackwards) return;
            if (!isTrainMoving) return;
            var checkDistance = 0.5f;
            var lastCart = carts[^1];
            var secondLastCart = carts[^2];

            var lastCartTransform = lastCart.transform;
            var direction = -lastCartTransform.forward;
            var lastCartDistance = Vector3.Distance(lastCart.transform.position,
                secondLastCart.transform.position);
            hasMorePathToMove = false;
            if (lastCartDistance < cartSpacing * 0.9f)
            {
                canMoveBackwards = false;
                isTrainMoving = false;
                lastCartPrevPosition = lastCart.transform.position;
                return;
            }

            canMoveBackwards = false;
            isTrainMoving = false;
            lastCartPrevPosition = lastCart.transform.position;
        }
    }
}