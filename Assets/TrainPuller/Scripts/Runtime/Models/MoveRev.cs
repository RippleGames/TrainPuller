using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using FluffyUnderware.Curvy;
using FluffyUnderware.Curvy.Controllers;
using System.Linq;
using DG.Tweening.Plugins.Core.PathCore;

public class MoveRev : MonoBehaviour
{
    public CurvySpline _spline;
    public GameObject _train;
    public Transform sphereTransform;
    public SplineController splineController;

    private float _currentTF = 0f;

    public float tfOffset = 0.1f;

    // Start is called before the first frame update
    // Update is called once per frame
    void Update()
    {
        HandleControl();
    }

    private void HandleControl()
    {
        _spline = splineController.Spline;
        float targetTF = _spline.GetNearestPointTF(sphereTransform.position, out Vector3 nearestPoint, Space.World);
        _currentTF = splineController.RelativePosition;

        var v1 = new Vector3(sphereTransform.position.x, 0, sphereTransform.position.z);
        var v2 = new Vector3(splineController.transform.position.x, 0, splineController.transform.position.z);
        splineController.Speed = Vector3.Distance(v1, v2) < 0.25f ? 0f : 5f;


        if (_currentTF < targetTF)
        {
            splineController.MovementDirection = MovementDirection.Forward; // Pozitif hız
        }
        else if (_currentTF > targetTF)
        {
            splineController.MovementDirection = MovementDirection.Backward; // Negatif hız
        }
    }

    public void SwitchSpline(CurvySplineMoveEventArgs curvySplineMoveEventArgs)
    {
        CurvySplineSegment segment = curvySplineMoveEventArgs.ControlPoint.GetComponent<CurvySplineSegment>();
        if (segment.Connection == null)
        {
            return;
        }

        int closestIndex = segment.Connection.ControlPointsList
            .Select((item, index) => new
                { Index = index, Distance = Vector3.Distance(sphereTransform.position, item.transform.position) })
            .OrderBy(x => x.Distance)
            .First().Index;


        print(segment.Connection.ControlPointsList[closestIndex]);

        float targetTF = segment.Connection.ControlPointsList[closestIndex].Spline
            .GetNearestPointTF(splineController.transform.position, out Vector3 nearestPoint, Space.World);
        _currentTF = targetTF;
        splineController.SwitchTo(segment.Connection.ControlPointsList[closestIndex].Spline, targetTF, 0.1f);
    }

    public void SwitchSplineOnSwtich(CurvySplineMoveEventArgs curvySplineMoveEventArgs)
    {
        _spline.TFToSegment(_currentTF);
    }
}