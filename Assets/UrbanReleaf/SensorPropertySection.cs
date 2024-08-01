using Netherlands3D.ObjectLibrary;
using UnityEngine;
using UnityEngine.UI;

namespace Netherlands3D.Twin.Layers.Properties
{
    public class SensorPropertySection : MonoBehaviour
    {
        private SensorDataController controller;
        [SerializeField] private Slider timeSlider;
        [SerializeField] private Slider observationSlider;

        public SensorDataController Controller
        {
            get => controller;
            set
            {
                controller = value;
                timeSlider.value = controller.TimeSeconds;
                observationSlider.value = controller.Observations;
            }
        }

        private void OnEnable()
        {
            timeSlider.onValueChanged.AddListener(HandleTimeSeconds);
            observationSlider.onValueChanged.AddListener(HandleObservationLimit);
        }

        private void OnDisable()
        {
            timeSlider.onValueChanged.RemoveListener(HandleTimeSeconds);
            observationSlider.onValueChanged.RemoveListener(HandleObservationLimit);
        }

        private void HandleTimeSeconds(float newValue)
        {
            controller.TimeSeconds = (int)newValue;
        }

        private void HandleObservationLimit(float newValue)
        {
            controller.Observations = (int)newValue;
        }
    }
}