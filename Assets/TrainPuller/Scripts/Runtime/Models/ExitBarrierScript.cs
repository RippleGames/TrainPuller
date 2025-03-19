using System.Collections.Generic;
using TemplateProject.Scripts.Data;
using UnityEngine;

namespace TrainPuller.Scripts.Runtime.Models
{
    public class ExitBarrierScript : MonoBehaviour
    {

        [SerializeField] private List<Renderer> renderers;
        [SerializeField] private GameColors gameColors;
        [SerializeField] private Transform exitPathEnd;

        public void SetBarrierProperties(LevelData.GridColorType colorType)
        {
            foreach (var renderer in renderers)
            {
                renderer.sharedMaterial = gameColors.activeMaterials[(int)colorType];
            }
        }

        public Transform GetPathEnd()
        {
            return exitPathEnd;
        }
    }
}
