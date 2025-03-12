using FluffyUnderware.Curvy;
using FluffyUnderware.Curvy.Controllers;
using System.Linq;
using TrainPuller.Scripts.Runtime.Models;
using UnityEngine;

public class CustomSelectorRev : ConnectedControlPointsSelector
{
    public Transform ballTransform;
    public TrainMovement trainMovement;
    public override CurvySplineSegment SelectConnectedControlPoint(
         SplineController caller,
         CurvyConnection connection,
         CurvySplineSegment currentControlPoint)
    {
        if (connection == null || currentControlPoint == null)
            return currentControlPoint;


        CurvySplineSegment bestMatch = null;
        float minDistance = float.MaxValue;
        Vector3 ballPos = ballTransform.position;

        foreach (var controlPoint in connection.ControlPointsList)
        {
            if (controlPoint.Spline == caller)
                continue;

            CurvySplineSegment prevSegment = controlPoint.Spline.GetPreviousControlPoint(controlPoint);
            CurvySplineSegment nextSegment = controlPoint.Spline.GetNextControlPoint(controlPoint);

            if (prevSegment != null)
            {
                float distPrev = Vector3.Distance(ballPos, prevSegment.transform.position);
                if (distPrev < minDistance)
                {
                    minDistance = distPrev;
                    bestMatch = controlPoint;
                }
            }

            if (nextSegment != null)
            {
                float distNext = Vector3.Distance(ballPos, nextSegment.transform.position);
                if (distNext < minDistance)
                {
                    minDistance = distNext;
                    bestMatch = controlPoint;
                }
            }
        }


        Debug.Log($"Selector: Chosen {bestMatch?.name} with angle");
        trainMovement.leaderChosenSplineSegment = bestMatch != null ? bestMatch : currentControlPoint;
        return bestMatch != null ? bestMatch : currentControlPoint;
    }
}
