using Netherlands3D.CartesianTiles;
using Netherlands3D.ObjectLibrary;
using UnityEngine;
using UnityEngine.UI;

namespace Netherlands3D.Twin.Layers.Properties
{
    public class SensorPropertySection : MonoBehaviour
    {
        private SensorDataController controller;
        private SensorProjectionLayer projectionLayer;
        [SerializeField] private Slider startTimeSlider;
        [SerializeField] private Slider endTimeSlider;
        [SerializeField] private Slider minSlider;
        [SerializeField] private Slider maxSlider;
        [SerializeField] private ColorPicker minimumColorPicker;
        [SerializeField] private ColorPicker maximumColorPicker;

        public SensorDataController Controller
        {
            get
            {                
                return controller;
            }            
            set
            {
                controller = value;
                if (projectionLayer == null)
                    projectionLayer = controller.gameObject.GetComponent<SensorProjectionLayer>();
                startTimeSlider.value = controller.StartTimeSeconds / (3600 * 24);
                endTimeSlider.value = controller.EndTimeSeconds / (3600 * 24);
                minSlider.value = controller.Minimum;
                maxSlider.value = controller.Maximum;
                minimumColorPicker.color = controller.MinColor;
                maximumColorPicker.color = controller.MaxColor;
            }
        }

        private void OnEnable()
        {          
            startTimeSlider.onValueChanged.AddListener(HandleStartTimeSeconds);
            endTimeSlider.onValueChanged.AddListener(HandleEndTimeSeconds);
            minSlider.onValueChanged.AddListener(HandleMinimum);
            maxSlider.onValueChanged.AddListener(HandleMaximum);
            minimumColorPicker.onColorChanged += HandleMinimumColor;
            maximumColorPicker.onColorChanged += HandleMaximumColor;
        }

        private void OnDisable()
        {
            startTimeSlider.onValueChanged.RemoveListener(HandleStartTimeSeconds);
            endTimeSlider.onValueChanged.RemoveListener(HandleEndTimeSeconds);
            minSlider.onValueChanged.RemoveListener(HandleMinimum);
            maxSlider.onValueChanged.RemoveListener(HandleMaximum);
            minimumColorPicker.onColorChanged -= HandleMinimumColor;
            maximumColorPicker.onColorChanged -= HandleMaximumColor;
        }

        private void HandleStartTimeSeconds(float newValue)
        {
            controller.StartTimeSeconds = (int)newValue * 3600 * 24;
            projectionLayer.SetVisibleTilesDirty();
        }

        private void HandleEndTimeSeconds(float newValue)
        {
            controller.EndTimeSeconds = (int)newValue * 3600 * 24;
            projectionLayer.SetVisibleTilesDirty();
        }

        private void HandleMinimum(float newValue) 
        {
            controller.Minimum = newValue;
            projectionLayer.SetVisibleTilesDirty();
        }

        private void HandleMaximum(float newValue) 
        {
            controller.Maximum = newValue;
            projectionLayer.SetVisibleTilesDirty();
        }

        private void HandleMinimumColor(Color newValue) 
        {
            if (controller)
            {
                controller.MinColor = newValue;
                projectionLayer.SetVisibleTilesDirty();
            }
        }

        private void HandleMaximumColor(Color newValue) 
        {
            if (controller)
            {
                controller.MaxColor = newValue;
                projectionLayer.SetVisibleTilesDirty();
            }
        }
    }
}