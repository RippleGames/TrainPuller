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
        private Vector3 lastHitPoint = Vector3.zero;
        private Vector3 lastMousePos = Vector3.zero;
        [SerializeField] private GameObject sphere;
        [SerializeField] private LevelContainer levelContainer;

        [Header("Parameters")] public LayerMask trainCartLayer;

        [Header("Flags")] public bool isHolding;

        private void Start()
        {
            AssignMainCam();
            AssignLevelContainer();
        }

        private void AssignLevelContainer()
        {
            if (!levelContainer)
            {
                levelContainer = FindObjectOfType<LevelContainer>();
            }
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
                // MoveObjectAlongSpline();
                MoveSphereToMousePosition();
            }
        }

        private void MoveSphereToMousePosition()
        {
            Ray ray = _mainCam.ScreenPointToRay(Input.mousePosition);

            if (Physics.Raycast(ray, out RaycastHit hit))
            {
                Vector3 mouseWorldPoint = hit.point;
                Vector3 closestSplinePoint = Vector3.zero;
                Quaternion closestSplineRotation = Quaternion.identity;
                float closestDistance = Mathf.Infinity;

                if (!sphere)
                {
                    sphere = currentlySelectedCart.gameObject;
                }
                foreach (var spline in levelContainer.splines)
                {
                    // Get the normalized parameter (t) closest to the mouse point
                    float t = spline.GetNearestPointTF(mouseWorldPoint);

                    // Get the spline position at t
                    Vector3 splinePos = spline.Interpolate(t);

                    // Compute distance between the mouse point and the spline position
                    float distance = Vector3.Distance(mouseWorldPoint, splinePos);
                    if (distance < closestDistance)
                    {
                        closestDistance = distance;
                        closestSplinePoint = splinePos;

                        // Compute the tangent at t.
                        // If your spline has a method like GetTangent or InterpolateTangent, use it:
                        Vector3 tangent = Vector3.zero;
                        if ( /* your spline provides a tangent method */ false)
                        {
                            // Example: tangent = spline.InterpolateTangent(t);
                        }
                        else
                        {
                            // Fallback: approximate the tangent using a small offset
                            float deltaT = 0.001f;
                            Vector3 pointAhead = spline.Interpolate(t + deltaT);
                            tangent = (pointAhead - splinePos).normalized;
                        }

                        // Determine the up vector. If your spline provides an up vector, use it; otherwise, default to Vector3.up.
                        Vector3 up = Vector3.up;
                        // For example, if available: up = spline.InterpolateUp(t);

                        // Create the rotation from the tangent and up vector
                        closestSplineRotation = Quaternion.LookRotation(tangent, up);
                    }
                }

                // Update sphere's transform to follow the spline's position and orientation
                sphere.transform.position = closestSplinePoint;
                sphere.transform.rotation = closestSplineRotation;
            }
        }


        private void MoveObjectAlongSpline()
        {
            Vector3 currentMousePos = Input.mousePosition;
            Vector3 screenDragDir = Vector3.zero;
            if (lastMousePos != Vector3.zero)
            {
                screenDragDir = (currentMousePos - lastMousePos).normalized;
            }

            lastMousePos = currentMousePos;

            // Convert screen space direction to world space.
            // Here we use the camera's transform; you might want to adjust this depending on your scene.
            Vector3 dragDir = Camera.main.transform.TransformDirection(screenDragDir);

            // Update CartScript's drag direction
            var mover = currentlySelectedCart.GetSplineController();
            var cart = mover.GetComponent<CartScript>();
            if (cart != null)
            {
                // cart.DraggingDirection = dragDir;
            }

            // Continue with your raycast to determine target position
            var ray = Camera.main.ScreenPointToRay(currentMousePos);
            if (Physics.Raycast(ray, out RaycastHit hit))
            {
                var targetAbsPos = mover.Spline.GetNearestPointTF(hit.point) * mover.Spline.Length;
                var currentAbsPos = mover.AbsolutePosition;
                var isMovingForward = targetAbsPos >= currentAbsPos;

                // Determine closest control point on the spline
                CurvySplineSegment closestCP = null;
                float closestDistance = float.MaxValue;
                foreach (var cp in mover.Spline.ControlPointsList)
                {
                    float distanceToCP = Mathf.Abs(cp.Distance - targetAbsPos);
                    if (distanceToCP < closestDistance)
                    {
                        closestDistance = distanceToCP;
                        closestCP = cp;
                    }
                }

                // if (closestCP && closestCP.Connection && closestDistance < 0.5f)
                // {
                //     var newCp = mover.ConnectionCustomSelector.SelectConnectedControlPoint(mover, closestCP.Connection,
                //         closestCP);
                //     if (newCp && newCp != closestCP)
                //     {
                //         if (newCp.Spline != mover.Spline)
                //         {
                //             mover.Play();
                //
                //             float destinationTf = newCp.Distance / newCp.Spline.Length;
                //             mover.SwitchTo(newCp.Spline, destinationTf, 0.5f);
                //         }
                //         else
                //         {
                //             mover.AbsolutePosition = newCp.Distance;
                //         }
                //
                //         return;
                //     }
                // }


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
                if (trainMovement.carts[0] == cartScript ||
                    trainMovement.carts[^1] == cartScript)
                {
                    trainMovement.MakeLeader(cartScript);
                    currentlySelectedCart = cartScript;
                }
            }
        }
    }
}