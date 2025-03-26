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
        [SerializeField] private Outline outline;
        public void SetCardProperties(LevelData.GridColorType colorType)
        {
            foreach (var cardRenderer in renderers)
            {
                cardRenderer.sharedMaterial = gameColors.activeMaterials[(int)colorType];
                cardColor = colorType;
            }
        }
        
        public LevelData.GridColorType GetCardColor()
        {
            return cardColor;
        }

        public void SetOutlineColor(Color color)
        {
            outline.OutlineColor = color;
        }
    }
}
