using FluffyUnderware.Curvy;
using FluffyUnderware.Curvy.Controllers;
using TrainPuller.Scripts.Runtime.Models;
using UnityEngine;

public class CustomConnectedControlPointsSelector : ConnectedControlPointsSelector
{
    public CartFollower cartFollower;
    public override CurvySplineSegment SelectConnectedControlPoint(
        SplineController caller,
        CurvyConnection connection,
        CurvySplineSegment currentControlPoint)
    {
        if (connection == null || currentControlPoint == null)
            return currentControlPoint;

        CurvySplineSegment bestSegment;
        if (cartFollower.followingCart.Spline == cartFollower.thisCart.Spline)
        {
            bestSegment = cartFollower.previousSegments[^1];
            cartFollower.previousSegments.Remove(bestSegment);
        }
        else
        {
            cartFollower.previousSegments.Add(currentControlPoint);
            bestSegment = cartFollower.trainMovement.leaderChosenSplineSegment;
        }
        
        return bestSegment;
    }
}