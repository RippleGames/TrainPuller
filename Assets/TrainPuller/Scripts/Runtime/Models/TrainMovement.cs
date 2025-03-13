using System.Collections.Generic;
using UnityEngine;
using FluffyUnderware.Curvy;
using TemplateProject.Scripts.Data;
using TrainPuller.Scripts.Runtime.Models;

public class TrainMovement : MonoBehaviour
{
    public LevelData.GridColorType cartsColor;
    [SerializeField] private Camera mainCam;
    public List<CartScript> carts;
    [SerializeField] private LevelContainer levelContainer;
    public float cartSpacing;

    void Start()
    {
        if (mainCam == null)
            mainCam = Camera.main;
        if (levelContainer == null)
            levelContainer = FindObjectOfType<LevelContainer>();
    }

    void Update()
    {
        UpdateFollowers();
    }

    private void UpdateFollowers()
    {
        if (carts == null || carts.Count < 2)
            return;

        CartScript leaderCart = carts[0];
        Vector3 previousPos = leaderCart.transform.position;

        for (int i = 1; i < carts.Count; i++)
        {
            Vector3 followerPos = carts[i].transform.position;

            // İstenen pozisyonu, bir önceki cart'tan cartSpacing kadar geride olacak şekilde hesapla.
            Vector3 direction = (followerPos - previousPos).normalized;
            Vector3 targetPos = previousPos - direction * cartSpacing;

            // En yakın spline'ı bul ve ona hizala.
            CurvySpline bestSpline = null;
            float bestDistance = Mathf.Infinity;
            float bestT = 0f;

            foreach (var spline in levelContainer.splines)
            {
                float t = spline.GetNearestPointTF(targetPos);
                Vector3 splinePos = spline.Interpolate(t);
                float distance = Vector3.Distance(targetPos, splinePos);
                if (distance < bestDistance)
                {
                    bestDistance = distance;
                    bestSpline = spline;
                    bestT = t;
                }
            }

            if (bestSpline != null)
            {
                Vector3 splineAlignedPos = bestSpline.Interpolate(bestT);
                Vector3 targetTangent = bestSpline.GetTangent(bestT);
                Quaternion targetRot = Quaternion.LookRotation(targetTangent, Vector3.up);

                carts[i].transform.position = new Vector3(splineAlignedPos.x, followerPos.y, splineAlignedPos.z);
                carts[i].transform.rotation = targetRot;
            }

            previousPos = carts[i].transform.position;
        }
    }

    public void MakeLeader(CartScript selectedCart)
    {
        if (carts.Count == 0 || carts[0] == selectedCart)
            return;
        if (!carts.Contains(selectedCart))
            return;

        carts.Remove(selectedCart);
        carts.Reverse();
        carts.Insert(0, selectedCart);
    }
}