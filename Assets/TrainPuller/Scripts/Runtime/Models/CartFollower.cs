using System.Collections.Generic;
using FluffyUnderware.Curvy;
using FluffyUnderware.Curvy.Controllers;
using UnityEngine;

namespace TrainPuller.Scripts.Runtime.Models
{
    public class CartFollower : MonoBehaviour
    {
        public TrainMovement trainMovement;
        public SplineController followingCart;
        public SplineController thisCart;
        public float followingDistance = 1f;
        public List<CurvySplineSegment> previousSegments;

        private void Update()
        {
            FollowCart();
        }

        private void FollowCart()
        {
            if (!followingCart) return;
            // var v1 = new Vector3(transform.position.x, 0, transform.position.z);
            // var v2 = new Vector3(followingCart.transform.position.x, 0, followingCart.transform.position.z);
            //
            // if (thisCart.Spline != followingCart.Spline)
            // {
            //     if (followingCart.Speed != 0)
            //     {
            //         thisCart.MovementDirection = followingCart.MovementDirection;
            //         thisCart.Speed = followingCart.Speed;
            //         
            //     }else if (followingCart.Speed == 0 && Vector3.Distance(v1, v2) > followingDistance)
            //     {
            //         thisCart.Speed = followingCart.Speed;
            //     }
            //
            //     return;
            // }

            // if (Vector3.Distance(v1, v2) <= followingDistance)
            // {
            //     thisCart.Speed = 0;
            //     return;
            // }
            //
            // if (Vector3.Distance(v1, v2) > followingDistance)
            // {
            //     thisCart.Speed = followingCart.Speed;
            // }

            thisCart.MovementDirection = followingCart.MovementDirection;
            thisCart.Speed = followingCart.Speed;
        }
    }
}