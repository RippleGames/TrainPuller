using UnityEngine;

namespace TemplateProject.Scripts.Data
{
    [CreateAssetMenu(fileName = "Game Color", menuName = "Game Color")]
    public class GameColors : ScriptableObject
    {

        public Color[] activeColors;
        public Material[] activeMaterials;

   
    
    }
}