using System.Collections.Generic;
using TemplateProject.Scripts.Data;
using UnityEngine;

namespace TrainPuller.Scripts.Runtime.Models
{
   public class CartScript : MonoBehaviour
   {
      [SerializeField] private List<Renderer> cartRenderers;
      [SerializeField] private GameColors colors;
      
      public void SetCartProperties(LevelData.GridColorType colorType)
      {

         var currentMaterial = colors.activeMaterials[(int)colorType];
         foreach (var renderer in cartRenderers)
         {

            renderer.sharedMaterial = currentMaterial;

         }

      }
   }
}
