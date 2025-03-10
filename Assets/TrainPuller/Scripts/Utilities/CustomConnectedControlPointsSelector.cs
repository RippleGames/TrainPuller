using FluffyUnderware.Curvy;
using FluffyUnderware.Curvy.Controllers;
using TrainPuller.Scripts.Runtime.Models;
using UnityEngine;

public class CustomConnectedControlPointsSelector : ConnectedControlPointsSelector
{
    public override CurvySplineSegment SelectConnectedControlPoint(
        SplineController caller,
        CurvyConnection connection,
        CurvySplineSegment currentControlPoint)
    {
        if (connection == null || currentControlPoint == null)
            return currentControlPoint;

        CartScript cart = caller.GetComponent<CartScript>();
        if (cart == null)
            return currentControlPoint;

        Vector3 dragDir = cart.DraggingDirection;
        if (dragDir == Vector3.zero)
            return currentControlPoint;

        CurvySplineSegment bestMatch = null;
        float smallestAngle = float.MaxValue;
        Vector3 currentPos = currentControlPoint.transform.position;

        foreach (var cp in connection.ControlPointsList)
        {
            if (cp == currentControlPoint)
                continue;

            Vector3 candidateDir = (cp.transform.position - currentPos).normalized;
            float angle = Vector3.Angle(dragDir, candidateDir);
            // Debug to check computed angles:
            Debug.Log($"Comparing candidate {cp.name}: Angle = {angle}");
            if (angle < smallestAngle)
            {
                smallestAngle = angle;
                bestMatch = cp;
            }
        }

        Debug.Log($"Selector: Chosen {bestMatch?.name} with angle {smallestAngle}");
        return bestMatch != null ? bestMatch : currentControlPoint;
    }
}