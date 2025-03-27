using System.Collections;
using System.Collections.Generic;
using System.Linq;
using DG.Tweening;
using TemplateProject.Scripts.Data;
using TemplateProject.Scripts.Runtime.Managers;
using TrainPuller.Scripts.Data;
using TrainPuller.Scripts.Runtime.Managers;
using UnityEngine;

namespace TrainPuller.Scripts.Runtime.Models
{
    public class TrainMovement : MonoBehaviour
    {
        public float speed = 5f;
        public float cartSpacing = 1f;
        public List<CartScript> carts = new List<CartScript>();
        public List<GridBase> cartCells = new List<GridBase>();
        public TrainContainer trainContainer;
        public InteractionManager interactionManager;
        public LevelData.GridColorType cartsColor;

        [SerializeField] public CartScript currentLeader;

        // public CartScript previousLeader;
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
        private bool _isLeaderChanged;
        private bool _isLeaderChanging;

        private float _positionThreshold = 0.01f;

        // public GameObject backwardsEndPrefab;
        // public GameObject backwardsEndObject;
        private bool hasMorePathToMove = true;
        public bool isMovingToExit;
        public Color outlineColor = Color.black;

        private void Start()
        {
            SetupCarts();
        }

        private void SetupCarts()
        {
            speed = 50f;
            var levelContainer = FindObjectOfType<LevelContainer>();
            interactionManager = FindObjectOfType<InteractionManager>();
            foreach (var cart in carts)
            {
                cart.gridBases = levelContainer.GetGridBases();
            }

            carts[0].transform.rotation = Quaternion.Euler(new Vector3(carts[0].transform.eulerAngles.x,
                carts[0].transform.eulerAngles.y - 180f, carts[0].transform.eulerAngles.z));
            HandlePathInitial();


            // var backwardsEnd = Instantiate(backwardsEndPrefab, carts[^1].transform.position, Quaternion.identity,
            //         transform)
            //     .GetComponent<BackwardsEnd>();
            // backwardsEndObject = backwardsEnd.gameObject;
            // backwardsEnd.SetTrainMovement(this);
        }

        private void HandlePathInitial()
        {
            trainPath.Add(carts[0].transform.position);
            for (int i = 0; i < carts.Count - 1; i++)
            {
                var startCart = carts[i];
                var endCart = carts[i + 1];

                var direction = (endCart.transform.position - startCart.transform.position).normalized;
                var distance = Vector3.Distance(startCart.transform.position, endCart.transform.position);

                for (float d = 0; d <= distance; d += 0.1f)
                {
                    trainPath.Add(startCart.transform.position + direction * d);
                }
            }

            trainPath.Add(carts[^1].transform.position);
            trainPath.Reverse();
        }


        private void Update()
        {
            if (carts.Count == 0) return;
            if (!currentLeader) return;
            if (!trainContainer.isAllFull && !isMovingToExit)
            {
                if (!interactionManager) return;
                if (!interactionManager.GetCurrentlySelectedCart()) return;
                if (interactionManager.GetCurrentlySelectedCart() != currentLeader)
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
        }


        private void DetectMovementDirection()
        {
            if (trainContainer.isAllFull && isMovingToExit)
            {
                isMovingBackwards = false;
                canMoveForward = true;
                canMoveBackwards = true;
                isTrainMoving = true;
                return;
            }

            if (_isLeaderChanging) return;
            if (!currentLeader || carts.Count < 2) return;
            if (trainPath.Count < 2) return;
            if(!interactionManager) return;
            
            var targetPosition = interactionManager.GetProjectedMousePositionOnTrail(true);
            var movementDirection = (targetPosition - currentLeader.transform.position).normalized;

            if (movementDirection.magnitude < 0.01f) return;

            var lastPosition = trainPath[^1];
            var secondLastPosition = trainPath[^2];

            var previousPathDirection = (lastPosition - secondLastPosition).normalized;

            var previousCart = carts[carts.IndexOf(currentLeader) + 1];
            var previousCartPosition = previousCart.transform.position;
            var directionToPreviousCart = (previousCartPosition - currentLeader.transform.position).normalized;
            var dotWithPreviousCart = Vector3.Dot(movementDirection, directionToPreviousCart);

            var dotWithTrainPath = Vector3.Dot(previousPathDirection, movementDirection);

            var isMouseBehindLeader = Vector3.Dot(movementDirection, -currentLeader.transform.forward) > 0.5f;


            if (dotWithTrainPath < 0 || isMouseBehindLeader)
            {
                isMovingBackwards = true;
            }
            else
            {
                isMovingBackwards = dotWithTrainPath < 0;
            }


            isMovingBackwards = dotWithPreviousCart switch
            {
                >= 0f => true,
                < -0f => false,
                _ => isMovingBackwards
            };

            if (isMovingBackwards)
            {
                canMoveBackwards = false;
                isTrainMoving = false;
                foreach (var cart in carts)
                {
                    cart.isMoving = false;
                }
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
            // backwardsEndObject.transform.position = trainPath[0];
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
                // previousLeader = currentLeader;
                currentLastCart = carts[^1];
                // backwardsEndObject.transform.position = currentLastCart.transform.position;
                _isLeaderChanging = false;
                interactionManager.dragPath.Clear();
                return;
            }

            if (trainPath.Count < 2)
            {
                carts.Reverse();
                trainContainer.InverseSlotList();
                currentLeader = carts[0];
                currentLastCart = carts[^1];
                trainPath.Clear();
                HandlePathInitial();             
                interactionManager.dragPath.Clear();
                // backwardsEndObject.transform.position = currentLastCart.transform.position;
                _isLeaderChanging = false;
                return;
            }


            // previousLeader = carts[0];
            carts.Reverse();
            trainContainer.InverseSlotList();
            currentLeader = carts[0];
            currentLastCart = carts[^1];
            trainPath.Clear();
            HandlePathInitial();
            interactionManager.dragPath.Clear();
            // backwardsEndObject.transform.position = currentLastCart.transform.position;
            _isLeaderChanged = !_isLeaderChanged;
            _isLeaderChanging = false;
        }

        public void GetOutFromExit(ExitBarrierScript exitBarrierScript)
        {
            if(isMovingToExit) return;
            interactionManager.HandleExit();
            transform.SetParent(null);
            isTrainMoving = true;
            canMoveBackwards = true;
            canMoveForward = true;
            isMovingToExit = true;
            currentLeader.gameObject.transform.DOMove(exitBarrierScript.GetPathEnd().position, 2f).OnComplete(() =>
            {
                transform.gameObject.SetActive(false);
                GameplayManager.instance.RemoveTrain(this);
            });
            var rotation = Quaternion.LookRotation(exitBarrierScript.GetPathEnd().position);

            var yAngle = rotation.eulerAngles.y;
            if (yAngle > 180f)
                yAngle -= 360f;

            var closestAngle = yAngle switch
            {
                >= -135f and < -45f => -90f,
                >= -45f and < 45f => 0f,
                >= 45f and < 135f => 90f,
                _ => 180f
            };

            rotation = Quaternion.Euler(new Vector3(0f, closestAngle, 0f));
            currentLeader.gameObject.transform.DORotateQuaternion(rotation, 0.15f);
        }

        public void StopMovement()
        {
            isMovingBackwards = false;
            isTrainMoving = false;
            canMoveBackwards = true;
            canMoveForward = true;
            foreach (var cart in carts)
            {
                cart.isMoving = false;
            }
        }

        public void HandleBackCollision()
        {
            isTrainMoving = false;
            canMoveBackwards = false;
        }

        public void HandleFrontCollision()
        {
            if (VibrationManager.instance)
            {
                VibrationManager.instance.Heavy();
            }

            StartCoroutine(ShakeTrain());
            isTrainMoving = false;
            canMoveForward = false;
        }

        private IEnumerator ShakeTrain()
        {
            var newList = new List<CartScript>();
            newList.AddRange(carts);
            foreach (var cart in newList)
            {
                cart.transform.DOShakeRotation(0.15f, new Vector3(0f, 0f, 15f), 4, 90f, true,
                    ShakeRandomnessMode.Harmonic);
                yield return new WaitForSeconds(0.05f);
            }
            
        }

        public void HandleBackwardsMovement()
        {
            if (carts.Count < 2) return;
            if (!isMovingBackwards) return;
            if (!isTrainMoving) return;
            var lastCart = carts[^1];
            var secondLastCart = carts[^2];

            var lastCartDistance = Vector3.Distance(lastCart.transform.position,
                secondLastCart.transform.position);
            hasMorePathToMove = false;
            if (lastCartDistance < cartSpacing * 0.9f)
            {
                canMoveBackwards = false;
                isTrainMoving = false;
                return;
            }

            canMoveBackwards = false;
            isTrainMoving = false;
        }

        public void ChangeOutlineColor(Color color)
        {
            outlineColor = color;
            foreach (var cart in carts)
            {
                cart.ChangeOutlineColor(color);
            }

            foreach (var card in trainContainer.GetTakenCards())
            {
                card.SetOutlineColor(color);
            }
        }

        public Color GetOutlineColor()
        {
            return outlineColor;
        }

        public void TryBlastConfetti()
        {
            StartCoroutine(BlastCartConfetti());
        }

        private IEnumerator BlastCartConfetti()
        {
            var newCartList = new List<CartScript>();
            newCartList.AddRange(carts);
            foreach (var cart in newCartList)
            {
                cart.CloseCartCover();
                yield return new WaitForSeconds(0.1f);
                cart.BlastConfetti();
                yield return new WaitForSeconds(0.1f);
            }
        }

        public void TryDoScaleEffect()
        {
            foreach (var cart in carts)
            {
                var oldScale = cart.transform.localScale;
                cart.transform.DOScale(oldScale * 1.2f, 0.15f).OnComplete(() =>
                {
                    cart.transform.DOScale(oldScale, 0.15f);
                });
            }
        }

        public void HandleCartPositioning()
        {
            var tempCartDict = new Dictionary<CartScript, int>();

            foreach (var cart in carts)
            {
                var adjacentCartCount = GetAdjacentCartCount(cart);
                tempCartDict.Add(cart, adjacentCartCount);
            }

            var headCart = tempCartDict.FirstOrDefault(x => x.Value == 1);
            ArrangeCarts(headCart.Key);
        }

        private void ArrangeCarts(CartScript headCart)
        {
            var newList = new List<CartScript>();
            newList.Add(headCart);
            var currentlyCheckingCart = headCart;
            carts.Remove(headCart);
            while (carts.Count > 0)
            {
                var currentlyCheckingCartGridCell = currentlyCheckingCart.currentGridCell;
                foreach (var cart in carts)
                {
                    if (newList.Contains(cart)) continue;
                    var gridCell = cart.currentGridCell;
                    if (Mathf.Abs(currentlyCheckingCartGridCell.x - gridCell.x) == 1 &&
                        currentlyCheckingCartGridCell.y == gridCell.y)
                    {
                        newList.Add(cart);
                        currentlyCheckingCart = cart;
                        carts.Remove(cart);
                        break;
                    }

                    if (Mathf.Abs(currentlyCheckingCartGridCell.y - gridCell.y) == 1 &&
                        currentlyCheckingCartGridCell.x == gridCell.x)
                    {
                        newList.Add(cart);
                        currentlyCheckingCart = cart;
                        carts.Remove(cart);
                        break;
                    }
                }
            }

            carts.Clear();
            carts.AddRange(newList);
        }

        private int GetAdjacentCartCount(CartScript checkingCart)
        {
            var count = 0;
            var checkingGridCell = checkingCart.currentGridCell;
            foreach (var cart in carts)
            {
                if (cart == checkingCart) continue;
                var comparingCartGridCell = cart.currentGridCell;
                if (Mathf.Abs(checkingGridCell.x - comparingCartGridCell.x) == 1 &&
                    checkingGridCell.y == comparingCartGridCell.y)
                {
                    count++;
                    continue;
                }

                if (Mathf.Abs(checkingGridCell.y - comparingCartGridCell.y) == 1 &&
                    checkingGridCell.x == comparingCartGridCell.x)
                {
                    count++;
                }
            }

            return count;
        }
    }
}