using Netherlands3D.Core;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

namespace Netherlands3D.Twin.Interface
{
    public class CameraHeightSlider : MonoBehaviour
    {
        private Slider slider;
        [SerializeField] private TextMeshProUGUI heightText;

        [SerializeField] private string textSuffix = "m NAP";
        [SerializeField] private float minimumHeight = -30f;
        [SerializeField] private float maximumHeight = 1500f;

        private float heightInNAP;
        private Camera mainCamera;
        private Transform cameraTransform;

        private void Awake()
        {
            mainCamera = Camera.main;
            cameraTransform = mainCamera.transform;
            slider = GetComponent<Slider>();
            slider.navigation = new Navigation()
            {
                mode = Navigation.Mode.None
            };
        }

        public void HeightSliderChanged(float sliderValue)
        {
            var newHeight = Mathf.Lerp(minimumHeight, maximumHeight, sliderValue);
            
            var position = cameraTransform.position;
            cameraTransform.position = new Vector3(position.x, newHeight, position.z);
        }

        void LateUpdate()
        {
            heightInNAP = Mathf.Round(cameraTransform.position.y);
            heightText.text = heightInNAP + textSuffix;

            slider.normalizedValue = Mathf.InverseLerp(minimumHeight, maximumHeight, cameraTransform.position.y);
        }
    }
}