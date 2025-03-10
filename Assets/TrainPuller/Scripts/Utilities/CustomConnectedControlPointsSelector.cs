using System.Linq;
using FluffyUnderware.Curvy;
using FluffyUnderware.Curvy.Controllers;
using TrainPuller.Scripts.Runtime.Models;
using UnityEngine;

public class CustomConnectedControlPointsSelector : ConnectedControlPointsSelector
{
    public override CurvySplineSegment SelectConnectedControlPoint(SplineController caller, CurvyConnection connection, CurvySplineSegment currentControlPoint)
    {

        return connection.ControlPointsList.Last();         

    }
}