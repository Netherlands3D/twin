using UnityEngine;
using UnityEngine.UI;

namespace Netherlands3D.Twin
{
    public class QualitySettingsUI : MonoBehaviour
    {
        [SerializeField] private Toggle lowQualityToggle;
        [SerializeField] private Toggle mediumQualityToggle;
        [SerializeField] private Toggle highQualityToggle;
        
        private void OnEnable()
        {
            var currentQuality = (GraphicsQualityLevel)UnityEngine.QualitySettings.GetQualityLevel();
            
            lowQualityToggle.isOn = currentQuality == GraphicsQualityLevel.Low;
            mediumQualityToggle.isOn = currentQuality == GraphicsQualityLevel.Medium;
            highQualityToggle.isOn = currentQuality == GraphicsQualityLevel.High;
            
            lowQualityToggle.onValueChanged.AddListener(SetGraphicsQualityToLow);
            mediumQualityToggle.onValueChanged.AddListener(SetGraphicsQualityToMedium);
            highQualityToggle.onValueChanged.AddListener(SetGraphicsQualityToHigh);
            
        }

        private void OnDisable()
        {
            lowQualityToggle.onValueChanged.RemoveListener(SetGraphicsQualityToLow);
            mediumQualityToggle.onValueChanged.RemoveListener(SetGraphicsQualityToMedium);
            highQualityToggle.onValueChanged.RemoveListener(SetGraphicsQualityToHigh);
        }

        private void SetGraphicsQualityToLow(bool isOn)
        {
            if(isOn)
                QualitySettings.SetGraphicsQuality(GraphicsQualityLevel.Low, true);
        }
        
        private void SetGraphicsQualityToMedium(bool isOn)
        {
            if(isOn)
                QualitySettings.SetGraphicsQuality(GraphicsQualityLevel.Medium, true);
        }
        
        private void SetGraphicsQualityToHigh(bool isOn)
        {
            if(isOn)
                QualitySettings.SetGraphicsQuality(GraphicsQualityLevel.High, true);
        }
    }
}