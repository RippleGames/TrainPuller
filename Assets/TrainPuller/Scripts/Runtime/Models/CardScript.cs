using System.Collections.Generic;
using TemplateProject.Scripts.Data;
using UnityEngine;

namespace TrainPuller.Scripts.Runtime.Models
{
    public class CardScript : MonoBehaviour
    {
        [SerializeField] private List<Renderer> renderers;
        [SerializeField] private GameColors gameColors;
        [SerializeField] private LevelData.GridColorType cardColor;
        public void SetCardProperties(LevelData.GridColorType colorType)
        {
            foreach (var cardRenderer in renderers)
            {
                cardRenderer.sharedMaterial = gameColors.activeMaterials[(int)colorType];
                cardColor = colorType;
            }
        }
        
        public LevelData.GridColorType GetCartColor()
        {
            return cardColor;
        }
    }
}
