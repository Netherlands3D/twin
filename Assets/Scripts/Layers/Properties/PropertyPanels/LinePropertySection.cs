using Netherlands3D.ObjectLibrary;
using UnityEngine;
using UnityEngine.UI;

namespace Netherlands3D.Twin.Layers.Properties
{
    public class LinePropertySection : MonoBehaviour
    {
        [SerializeField] private Slider widthSlider;

        private void OnEnable()
        {
            widthSlider.onValueChanged.AddListener(LineWidthChange);
        }
        
        private void OnDisable()
        {
            widthSlider.onValueChanged.RemoveListener(LineWidthChange);
        }

        private void LineWidthChange(float newValue)
        {
            Debug.Log("Update line");
        }
    }
}