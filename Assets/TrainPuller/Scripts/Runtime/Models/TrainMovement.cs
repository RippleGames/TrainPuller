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
        public CurvySplineSegment leaderChosenSplineSegment;


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