using Netherlands3D.Windmills;
using UnityEngine;
using UnityEngine.UI;

namespace Netherlands3D.Twin.Layers.Properties
{
    public class WindmillPropertySection : MonoBehaviour
    {
        private Windmill windmill;
        [SerializeField] private Slider axisHeightSlider;
        [SerializeField] private Slider rotorDiameterSlider;

        public Windmill Windmill
        {
            get => windmill;
            set
            {
                windmill = value;
                axisHeightSlider.value = windmill.AxisHeight;
                rotorDiameterSlider.value = windmill.RotorDiameter;
            }
        }

        private void OnEnable()
        {
            axisHeightSlider.onValueChanged.AddListener(HandleAxisHeightChange);
            rotorDiameterSlider.onValueChanged.AddListener(HandleRotorDiameterChange);
        }
        
        private void OnDisable()
        {
            axisHeightSlider.onValueChanged.RemoveListener(HandleAxisHeightChange);
            rotorDiameterSlider.onValueChanged.RemoveListener(HandleRotorDiameterChange);
        }

        private void HandleAxisHeightChange(float newValue)
        {
            windmill.AxisHeight = newValue;
        }

        private void HandleRotorDiameterChange(float newValue)
        {
            windmill.RotorDiameter = newValue;
        }
    }
}