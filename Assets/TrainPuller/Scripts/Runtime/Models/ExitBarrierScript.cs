using System.Collections;
using System.Collections.Generic;
using TemplateProject.Scripts.Data;
using UnityEngine;

public class ExitBarrierScript : MonoBehaviour
{

    [SerializeField] private List<Renderer> renderers;
    [SerializeField] private GameColors gameColors;

    public void SetBarrierProperties(LevelData.GridColorType colorType)
    {
        foreach (var renderer in renderers)
        {
            renderer.sharedMaterial = gameColors.activeMaterials[(int)colorType];
        }
    }
}
