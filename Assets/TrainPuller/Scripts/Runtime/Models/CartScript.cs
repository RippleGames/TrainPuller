using System.Collections.Generic;
using FluffyUnderware.Curvy.Controllers;
using TemplateProject.Scripts.Data;
using UnityEngine;

namespace TrainPuller.Scripts.Runtime.Models
{
   public class CartScript : MonoBehaviour
   {
      [SerializeField] private TrainMovement trainMovement;
      [SerializeField] private SplineController cartSplineController;
      [SerializeField] private List<Renderer> cartRenderers;
      [SerializeField] private GameColors colors;
      public Vector3 DraggingDirection { get; set; }
      private bool isMovingForward = true;

      public void SetMovementDirection(bool forward)
      {
         isMovingForward = forward;
      }

      public bool IsMovingForward()
      {
         return isMovingForward;
      }
      public void SetTrainMovementScript(TrainMovement movement)
      {
         trainMovement = movement;
      }
      
      public void SetCartProperties(LevelData.GridColorType colorType)
      {

         var currentMaterial = colors.activeMaterials[(int)colorType];
         foreach (var renderer in cartRenderers)
         {

            renderer.sharedMaterial = currentMaterial;

         }

      }

      public TrainMovement GetTrainMovement()
      {
         return trainMovement;
      }

      public SplineController GetSplineController()
      {
         return cartSplineController;
      }
   }
}
