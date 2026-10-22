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
        private bool labelsVisible = true;
        
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
        
        [UxmlAttribute("labels-visible")]
        public bool LabelsVisible
        {
            get => labelsVisible;
            set
            {
                labelsVisible = value;
                xField.LabelVisible = value;
                yField.LabelVisible = value;
                zField.LabelVisible = value;
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