using System.Globalization;
using Netherlands3D.UI.ExtensionMethods;
using UnityEngine.UIElements;

namespace Netherlands3D.UI.Components
{
    [UxmlElement]
    public partial class MemoryStats : VisualElement
    {
        private int systemValue;
        private Label systemValueLabel;
        private Label SystemValueLabel => systemValueLabel ??= this.Q<Label>("SystemValueLabel");

        [UxmlAttribute("system-value")]
        public int SystemValue
        {
            get => systemValue;
            set
            {
                systemValue = value;
                SystemValueLabel.text = $"{value} MB";
            }
        }

        private float managedValue;
        private Label managedValueLabel;
        private Label ManagedValueLabel =>
            managedValueLabel ??= this.Q<Label>("ManagedValueLabel");

        [UxmlAttribute("managed-value")]
        public float ManagedValue
        {
            get => managedValue;
            set
            {
                managedValue = value;
                ManagedValueLabel.text =
                    $"{value.ToString("F2", CultureInfo.InvariantCulture)} MB";
            }
        }

        public MemoryStats()
        {
            this.CloneComponentTree("Components");
            this.AddComponentStylesheet("Components");
        }
    }
}