using System;
using System.Collections.Generic;
using Netherlands3D.UI.ExtensionMethods;
using UnityEngine.UIElements;

namespace Netherlands3D.UI.Components
{
    [UxmlElement]
    public partial class ToolbarScenario : VisualElement
    {
        private const string ScenarioButtonClassName =
            "toolbar-scenario__button";

        private readonly ToggleButtonGroup buttonGroup;
        private bool isUpdating;

       /* Raised when the user changes the selected scenario.
        The index refers to the order supplied through SetScenarios.A null value means that no scenario is selected.*/
        public event Action<int?> SelectionChanged;

        public ToolbarScenario()
        {
            this.CloneComponentTree("Components");
            this.AddComponentStylesheet("Components");

            buttonGroup =
                this.Q<ToggleButtonGroup>("ScenarioButtonGroup");

            RegisterCallback<AttachToPanelEvent>(OnAttachToPanel);
            RegisterCallback<DetachFromPanelEvent>(OnDetachFromPanel);

            SetVisible(false);
        }

        private void OnAttachToPanel(AttachToPanelEvent _)
        {
            buttonGroup.RegisterValueChangedCallback(
                OnButtonGroupValueChanged
            );
        }

        private void OnDetachFromPanel(DetachFromPanelEvent _)
        {
            buttonGroup.UnregisterValueChangedCallback(
                OnButtonGroupValueChanged
            );
        }

        /* Rebuilds the dynamic scenario buttons. This method only receives display labels and contains no layer logic.*/
        public void SetScenarios(IReadOnlyList<string> labels, int? selectedIndex)
        {
            isUpdating = true;

            try
            {
                buttonGroup.Clear();

                for (var i = 0; i < labels.Count; i++)
                {
                    var button = new Button
                    {
                        name = $"ScenarioButton-{i}",
                        LabelText = labels[i],
                        ShowIcon = Button.ButtonStyle.Normal
                    };

                    button.AddToClassList(
                        ScenarioButtonClassName
                    );

                    buttonGroup.Add(button);
                }

                SetSelectionWithoutNotify(selectedIndex);
                SetVisible(labels.Count > 0);
            }
            finally
            {
                isUpdating = false;
            }
        }

        /*Updates only the checked state without rebuilding the buttons.*/
        public void SetSelectionWithoutNotify(int? selectedIndex)
        {
            var state = new ToggleButtonGroupState(
                0,
                buttonGroup.childCount
            );

            if (selectedIndex.HasValue &&
                selectedIndex.Value >= 0 &&
                selectedIndex.Value < state.length)
            {
                state[selectedIndex.Value] = true;
            }

            buttonGroup.SetValueWithoutNotify(state);
        }

        private void OnButtonGroupValueChanged(
            ChangeEvent<ToggleButtonGroupState> evt
        )
        {
            if (isUpdating)
                return;

            SelectionChanged?.Invoke(
                GetSelectedIndex(evt.newValue)
            );
        }

        private static int? GetSelectedIndex(
            ToggleButtonGroupState state
        )
        {
            for (var i = 0; i < state.length; i++)
            {
                if (state[i])
                    return i;
            }

            return null;
        }

        private void SetVisible(bool visible)
        {
            style.display = visible
                ? DisplayStyle.Flex
                : DisplayStyle.None;
        }
    }
}