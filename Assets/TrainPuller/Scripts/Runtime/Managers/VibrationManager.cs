using Lofelt.NiceVibrations;
using TrainPuller.Scripts.Runtime.Managers;
using UnityEngine;

namespace TemplateProject.Scripts.Runtime.Managers
{
    public class VibrationManager : MonoBehaviour
    {
        public static VibrationManager instance;
        
        [Header("Parameters")]
        private float intensity, sharpness;
        
        private void Awake()
        {
            DontDestroyOnLoad(this);
            MakeSingleton();
        }

        private void MakeSingleton()
        {
            if (instance == null)
            {
                instance = this;
            }
            else
            {
                Destroy(gameObject);
            }
        }

        public void Light()
        {
            if (!GameplayManager.instance.GetVibration())
                return;
            HapticPatterns.PlayPreset(HapticPatterns.PresetType.LightImpact);
        }

        public void Medium()
        {
            if (!GameplayManager.instance.GetVibration())
                return;
            HapticPatterns.PlayPreset(HapticPatterns.PresetType.MediumImpact);
        }

        public void Heavy()
        {
            if (!GameplayManager.instance.GetVibration())
                return;
            HapticPatterns.PlayPreset(HapticPatterns.PresetType.HeavyImpact);
        }

        public void Win()
        {
            if (!GameplayManager.instance.GetVibration())
                return;
            HapticPatterns.PlayPreset(HapticPatterns.PresetType.Success);
        }
        
        public void Fail()
        {
            if (!GameplayManager.instance.GetVibration())
                return;
            HapticPatterns.PlayPreset(HapticPatterns.PresetType.Failure);
        }
    }
}