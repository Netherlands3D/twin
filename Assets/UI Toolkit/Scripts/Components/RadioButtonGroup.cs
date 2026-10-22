using Netherlands3D.UI.ExtensionMethods;
using System.Collections.Generic;
using System.Linq;
using UnityEngine.UIElements;

namespace Netherlands3D.UI.Components
{
    [UxmlElement]
    public partial class RadioButtonGroup : UnityEngine.UIElements.RadioButtonGroup
    {
        public RadioButtonGroup()
        {
            this.AddComponentStylesheet("Components");
            AddToClassList("radio-group");

            if (string.IsNullOrEmpty(label))
                label = "Header label";

            // Default 1 choice, only when none set
            if (choices == null || choices.Count() == 0)
                choices = new List<string> { "Label" };

            RegisterCallback<AttachToPanelEvent>(_ =>
            {
                ApplyTextClasses();
                RegisterCallback<GeometryChangedEvent>(__ => ApplyTextClasses());
            });
        }

        private void ApplyTextClasses()
        {
            // Header label
            var header = this.Q<Label>(className: "unity-base-field__label");
            header?.AddToClassList("text-header");

            // Choice labels: labels within the radio buttons
            var radioButtons = this.Query<VisualElement>(className: "unity-radio-button").ToList();
            foreach (var rb in radioButtons)
            {
                var choiceLabel = rb.Q<Label>(className: "unity-label");
                choiceLabel?.AddToClassList("text-base");
            }
        }
    }
}