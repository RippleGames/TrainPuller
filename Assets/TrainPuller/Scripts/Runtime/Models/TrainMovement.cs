using System.Collections.Generic;
using FluffyUnderware.Curvy.Controllers;
using UnityEngine;

namespace TrainPuller.Scripts.Runtime.Models
{
    public class TrainMovement : MonoBehaviour
    {
        public float cartSpacing = 1f; // Spacing between carts

        public List<SplineController> carts = new List<SplineController>();

        void Update()
        {
            if (carts.Count == 0) return;

            for (int i = 1; i < carts.Count; i++)
            {
                var leader = carts[i - 1]; // Previous cart
                var follower = carts[i];

                float leaderAbsPos = leader.AbsolutePosition;
                float spacingAbs = cartSpacing; // Spacing in world units

                // Compute the desired absolute position
                float desiredAbsPos = (leaderAbsPos - spacingAbs + leader.Spline.Length) % leader.Spline.Length;

                // Set the follower’s absolute position
                follower.AbsolutePosition = desiredAbsPos;
            }
        }
    }
}