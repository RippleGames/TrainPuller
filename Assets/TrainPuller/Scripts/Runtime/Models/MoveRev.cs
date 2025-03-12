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
    public float tfSpeed = 3.5f; // Saniyede deðiþecek tf miktarý
    // Start is called before the first frame update
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        _spline = splineController.Spline;
        float targetTF = _spline.GetNearestPointTF(sphereTransform.position, out Vector3 nearestPoint, Space.World);
        _currentTF = splineController.RelativePosition;

        if (_currentTF < targetTF)
        {
            splineController.MovementDirection = MovementDirection.Forward; // Pozitif hýz
        }
        else if (_currentTF > targetTF)
        {
            splineController.MovementDirection = MovementDirection.Backward; // Negatif hýz
        }

    }

    public void SwitchSpline(CurvySplineMoveEventArgs curvySplineMoveEventArgs)
    {
        CurvySplineSegment segment = curvySplineMoveEventArgs.ControlPoint.GetComponent<CurvySplineSegment>();
        if ( segment.Connection==null)
        {
            return;
        }

        int closestIndex = segment.Connection.ControlPointsList
         .Select((item, index) => new { Index = index, Distance = Vector3.Distance(sphereTransform.position, item.transform.position) })
         .OrderBy(x => x.Distance)
         .First().Index;


        print(segment.Connection.ControlPointsList[closestIndex]);

        float targetTF = segment.Connection.ControlPointsList[closestIndex].Spline.GetNearestPointTF(splineController.transform.position, out Vector3 nearestPoint, Space.World);
        _currentTF = targetTF;
        splineController.SwitchTo(segment.Connection.ControlPointsList[closestIndex].Spline, targetTF, 0.1f);
      


    }

    public void SwitchSplineOnSwtich(CurvySplineMoveEventArgs curvySplineMoveEventArgs)
    {
        _spline.TFToSegment(_currentTF);
    }
}
