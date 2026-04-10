using Netherlands3D.UI.ExtensionMethods;
using RuntimeHandle;
using UnityEngine.UIElements;

namespace Netherlands3D.UI.Components
{
    [UxmlElement]
    public partial class XYZField : VisualElement
    {
        public NumberField xField;
        public NumberField yField;
        public NumberField zField;

        private NumberFieldStyle numberFieldStyle = NumberFieldStyle.Default;
        private int decimalCount = 0;
        
        [UxmlAttribute("number-field-style")]
        public NumberFieldStyle Style
        {
            get => numberFieldStyle;
            set
            {
                numberFieldStyle = value;
                xField.Style = numberFieldStyle;
                yField.Style = numberFieldStyle;
                zField.Style = numberFieldStyle;
            }
        }
        
        
        [UxmlAttribute("decimal-count")]
        public int DecimalCount
        {
            get => decimalCount;
            set
            {
                decimalCount = value;
                xField.DecimalCount = decimalCount;
                yField.DecimalCount = decimalCount;
                zField.DecimalCount = decimalCount;
            }
        }        
        public XYZField()
        {            
            this.CloneComponentTree("Components");
            this.AddComponentStylesheet("Components");
            this.xField = this.Q<NumberField>("X");
            this.yField = this.Q<NumberField>("Y");
            this.zField = this.Q<NumberField>("Z");
        }

        public void EnableAxes(HandleAxes enabledAxes)
        {
            xField.SetEnabled(enabledAxes.HasFlag(HandleAxes.X));
            yField.SetEnabled(enabledAxes.HasFlag(HandleAxes.Y));
            zField.SetEnabled(enabledAxes.HasFlag(HandleAxes.Z));
        }
    }
}