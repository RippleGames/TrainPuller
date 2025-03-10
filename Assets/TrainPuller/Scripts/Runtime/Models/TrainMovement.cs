using System.Collections.Generic;
using FluffyUnderware.Curvy;
using FluffyUnderware.Curvy.Controllers;
using TemplateProject.Scripts.Data;
using UnityEngine;

namespace TrainPuller.Scripts.Runtime.Models
{
    public class TrainMovement : MonoBehaviour
    {
        public float cartSpacing = 1f;
        public List<SplineController> carts = new List<SplineController>();
        public LevelData.GridColorType cartsColor;
        private int movementDirection = 1; // 1 = forward, -1 = backward

        private Dictionary<CurvySpline, float> splineOffsets = new Dictionary<CurvySpline, float>();

        private void Start()
        {
            CalculateSplineOffsets();
        }

        private void Update()
        {
            if (carts.Count == 0) return;

            for (var i = 1; i < carts.Count; i++)
            {
                var leader = carts[i - 1];
                var follower = carts[i];

                float leaderAbsPos = GetGlobalPosition(leader);
                float spacingAbs = cartSpacing;

                float desiredAbsPos = leaderAbsPos - (spacingAbs * movementDirection);

                SetGlobalPosition(follower, desiredAbsPos);
            }
        }

        private void CalculateSplineOffsets()
        {
            splineOffsets.Clear();
            float totalOffset = 0f;

            foreach (CurvySpline spline in FindObjectsOfType<CurvySpline>())
            {
                splineOffsets[spline] = totalOffset;
                totalOffset += spline.Length;
            }
        }

        private float GetGlobalPosition(SplineController cart)
        {
            if (!splineOffsets.ContainsKey(cart.Spline))
                CalculateSplineOffsets();

            float splineOffset = splineOffsets[cart.Spline];
            return cart.AbsolutePosition + splineOffset;
        }

        private void SetGlobalPosition(SplineController cart, float globalPos)
        {
            // Calculate overall minimum offset and maximum global position.
            float minOffset = float.MaxValue;
            float maxGlobal = 0f;
            foreach (var splineEntry in splineOffsets)
            {
                float offset = splineEntry.Value;
                minOffset = Mathf.Min(minOffset, offset);
                maxGlobal = Mathf.Max(maxGlobal, offset + splineEntry.Key.Length);
            }
    
            // Clamp globalPos to the valid overall range.
            globalPos = Mathf.Clamp(globalPos, minOffset, maxGlobal - 0.0001f); // subtract a small epsilon
    
            // Find the spline corresponding to the clamped global position.
            foreach (var splineEntry in splineOffsets)
            {
                CurvySpline spline = splineEntry.Key;
                float offset = splineEntry.Value;
                if (globalPos >= offset && globalPos < offset + spline.Length)
                {
                    cart.Spline = spline;
                    cart.AbsolutePosition = globalPos - offset;
                   
                    return;
                }
            }

            Debug.LogError("Global position out of bounds!");
        }


        public void MakeLeader(SplineController selectedCart)
        {
            if (carts.Count == 0 || carts[0] == selectedCart) return;
            if (!carts.Contains(selectedCart)) return;

            // Determine new direction BEFORE reversing the list
            movementDirection *= -1; // Flip direction

            // Reorder list
            carts.Remove(selectedCart);
            carts.Reverse();
            carts.Insert(0, selectedCart);
        }
    }
}