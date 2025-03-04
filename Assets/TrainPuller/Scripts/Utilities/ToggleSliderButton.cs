using UnityEngine;
using UnityEngine.UI;

namespace BusJamClone.Scripts.Utilities
{
    public class ToggleSliderButton : MonoBehaviour
    {
        [SerializeField] private Slider slider;

        public void ToggleSlider()
        {
            slider.value = slider.value == 0 ? 1 : 0;
        }
   
    }
}
