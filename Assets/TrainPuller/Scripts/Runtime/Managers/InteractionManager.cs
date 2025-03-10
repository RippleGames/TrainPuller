using System.Collections.Generic;
using FluffyUnderware.Curvy;
using TemplateProject.Scripts.Data;
using TemplateProject.Scripts.Runtime.Managers;
using TemplateProject.Scripts.Runtime.Models;
using TrainPuller.Scripts.Runtime.Models;
using UnityEngine;

namespace TrainPuller.Scripts.Runtime.Managers
{
    public class InteractionManager : MonoBehaviour
    {
        [Header("Cached References")] private Camera _mainCam;
        [AudioClipName] public string popSound;
        [SerializeField] private CartScript currentlySelectedCart;

        [Header("Parameters")] public LayerMask trainCartLayer;

        [Header("Flags")] public bool isHolding;

        private void Start()
        {
            AssignMainCam();
        }

        private void AssignMainCam()
        {
            _mainCam = Camera.main;
        }

        private void Update()
        {
            // if (!ShouldProcessInput()) return;

            if (Input.GetMouseButtonDown(0) && !currentlySelectedCart && !isHolding)
            {
                ProcessRaycastInteraction();
                isHolding = true;
            }

            if (Input.GetMouseButtonUp(0))
            {
                isHolding = false;
                currentlySelectedCart = null;
            }

            if (currentlySelectedCart && isHolding)
            {
                MoveObjectAlongSpline();
            }
        }

        private void MoveObjectAlongSpline()
        {
            var ray = Camera.main.ScreenPointToRay(Input.mousePosition);
            if (Physics.Raycast(ray, out RaycastHit hit))
            {
                var mover = currentlySelectedCart.GetSplineController();
                var targetAbsPos = mover.Spline.GetNearestPointTF(hit.point) * mover.Spline.Length;
                var currentAbsPos = mover.AbsolutePosition;
                var maxAbsPos = mover.Spline.Length;

                var isMovingForward = targetAbsPos >= currentAbsPos;

                CurvySplineSegment closestCP = null;
                var closestDistance = float.MaxValue;

                foreach (var cp in mover.Spline.ControlPointsList)
                {
                    var distanceToCP = Mathf.Abs(cp.Distance - targetAbsPos);
                    if (distanceToCP < closestDistance)
                    {
                        closestDistance = distanceToCP;
                        closestCP = cp;
                    }
                }

                if (closestCP && closestCP.Connection && closestDistance < 0.5f)
                {
                    var newCp = mover.ConnectionCustomSelector.SelectConnectedControlPoint(mover, closestCP.Connection,
                        closestCP);
                    if (newCp && newCp != closestCP)
                    {
                        mover.AbsolutePosition = newCp.Distance;
                        return;
                    }
                }

                mover.GetComponent<CartScript>().SetMovementDirection(isMovingForward);

                mover.AbsolutePosition = Mathf.Lerp(currentAbsPos, targetAbsPos, Time.deltaTime * 10f);
            }
        }


        private bool ShouldProcessInput()
        {
            return LevelManager.instance.isGamePlayable && !LevelManager.instance.isLevelFailed;
        }

        void ProcessRaycastInteraction()
        {
            var ray = _mainCam.ScreenPointToRay(Input.mousePosition);

            if (!TryRayCast(ray, out var hitInfo, trainCartLayer)) return;
            if (!hitInfo.transform || !hitInfo.transform.CompareTag("TrainCart")) return;
            TrySelectCart(hitInfo);
        }

        private bool TryRayCast(Ray ray, out RaycastHit hitInfo, LayerMask layer)
        {
            return Physics.Raycast(ray, out hitInfo, Mathf.Infinity, layer);
        }

        private void TrySelectCart(RaycastHit hitInfo)
        {
            if (hitInfo.transform.gameObject.TryGetComponent(out CartScript cartScript))
            {
                var trainMovement = cartScript.GetTrainMovement();
                if (trainMovement.carts[0] == cartScript.GetSplineController() ||
                    trainMovement.carts[^1] == cartScript.GetSplineController())
                {
                    trainMovement.MakeLeader(cartScript.GetSplineController());
                    currentlySelectedCart = cartScript;
                }
            }
        }
    }
}