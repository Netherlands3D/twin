using UnityEngine.UI;
using TMPro;
using UnityEngine;

namespace Netherlands3D.Functionalities.Indicators.UI
{
    public class LegendItem : MonoBehaviour
    {
        [SerializeField] private TMP_Text label;
        [SerializeField] private Image colorLabel;
        
        public void Set(string label, Color color)
        {
            this.label.text = label;
            this.colorLabel.color = color;
        }
    }
}
