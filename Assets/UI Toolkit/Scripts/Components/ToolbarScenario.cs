using System;
using System.Collections.Generic;
using System.Linq;
using Netherlands3D.Twin.Layers.Properties;
using Netherlands3D.UI_Toolkit;
using Netherlands3D.UI.ExtensionMethods;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UIElements;

namespace Netherlands3D.UI.Components
{
    [UxmlElement]
    public partial class ToolbarScenario : VisualElement
    {
        private const string ScenarioButtonClassName = "toolbar-scenario__button";

        private readonly ToggleButtonGroup buttonGroup;
        private readonly Dictionary<FolderPropertyData, Button> buttonsByKey = new();
        
        public UnityEvent<FolderPropertyData> SelectionChanged = new();

        public ToolbarScenario()
        {
            this.CloneComponentTree("Components");
            this.AddComponentStylesheet("Components");

            buttonGroup = this.Q<ToggleButtonGroup>("ScenarioButtonGroup");
            buttonGroup.allowEmptySelection = true;

            RegisterCallback<AttachToPanelEvent>(OnAttachToPanel);
            RegisterCallback<DetachFromPanelEvent>(OnDetachFromPanel);

            SetVisible(false);
        }

        private void OnAttachToPanel(AttachToPanelEvent _)
        {
            buttonGroup.RegisterValueChangedCallback(OnButtonGroupValueChanged);
        }

        private void OnDetachFromPanel(DetachFromPanelEvent _)
        {
            buttonGroup.UnregisterValueChangedCallback(OnButtonGroupValueChanged);
        }

        public void InsertFolder(FolderPropertyData key, int index, string label, bool isScenario)
        {
            var button = new Button
            {
                name = $"ScenarioButton-{buttonsByKey.Count}",
                LabelText = label,
                ShowIcon = Button.ButtonStyle.Normal,
                userData = key
            };

            button.AddToClassList(ScenarioButtonClassName);

            var clampedIndex = Mathf.Clamp(index, 0, buttonGroup.childCount);
            buttonGroup.Insert(clampedIndex, button);
            buttonsByKey.Add(key, button);

            ApplyScenarioVisibility(button, isScenario);
            SetVisible(HasAnyVisibleButton());
        }

        /*Removes the button for a folder that no longer exists in the
          hierarchy at all (not just one that stopped being a scenario -
          use SetScenarioVisible for that case).*/
        public void RemoveFolder(FolderPropertyData key)
        {
            if (!buttonsByKey.TryGetValue(key, out var button))
                return;

            var wasSelected = IsSelected(button);

            buttonGroup.Remove(button);
            buttonsByKey.Remove(key);

            if (wasSelected)
                SelectionChanged.Invoke(null);

            SetVisible(HasAnyVisibleButton());
        }

        public void SetFolderIndex(FolderPropertyData key, int newIndex)
        {
            var button = buttonsByKey[key];
            buttonGroup.Remove(button);
            var clampedIndex = Mathf.Clamp(newIndex, 0, buttonGroup.childCount);
            buttonGroup.Insert(clampedIndex, button);
        }

        /*Toggles one button's visibility/selectability in place - no
          rebuild, no reordering, no effect on any other button.*/
        public void SetScenarioVisible(FolderPropertyData key, bool isScenario)
        {
            if (!buttonsByKey.TryGetValue(key, out var button))
                return;

            var wasSelected = IsSelected(button);
            ApplyScenarioVisibility(button, isScenario);

            // A button that just stopped being a scenario can't stay selected.
            if (!isScenario && wasSelected)
            {
                SetSelectedFolderWithoutNotify(null);
                SelectionChanged.Invoke(null);
            }

            SetVisible(HasAnyVisibleButton());
        }

        /*Reflects a selection that originated elsewhere (e.g. ActiveSelf
          changed externally) without re-notifying - use this from
          ScenarioManager rather than SelectionChanged.Invoke to avoid
          feedback loops.*/
        public void SetSelectedFolderWithoutNotify(FolderPropertyData key)
        {
            var state = new ToggleButtonGroupState(0, buttonGroup.childCount);

            if (key != null && buttonsByKey.TryGetValue(key, out var button))
            {
                var index = buttonGroup.IndexOf(button);
                if (index >= 0 && index < state.length)
                    state[index] = true;
            }

            buttonGroup.SetValueWithoutNotify(state);
        }

        private static void ApplyScenarioVisibility(Button button, bool isScenario)
        {
            button.SetEnabled(isScenario);
            // button.EnableInClassList(UtilityClassConstants.HIDDEN, !isScenario);
        }

        private bool IsSelected(Button button)
        {
            var index = buttonGroup.IndexOf(button);
            var value = buttonGroup.value;
            return index >= 0 && index < value.length && value[index];
        }

        private bool HasAnyVisibleButton()
        {
            return buttonsByKey.Values.Any(b => !b.ClassListContains(UtilityClassConstants.HIDDEN));
        }

        private void OnButtonGroupValueChanged(ChangeEvent<ToggleButtonGroupState> evt)
        {
            SelectionChanged.Invoke(GetSelectedKey(evt.newValue));
        }

        private FolderPropertyData GetSelectedKey(ToggleButtonGroupState state)
        {
            var index = 0;
            foreach (var child in buttonGroup.Children())
            {
                if (index < state.length && state[index] && child is Button button)
                    return button.userData as FolderPropertyData;
                index++;
            }

            return null;
        }

        private void SetVisible(bool visible)
        {
            EnableInClassList(UtilityClassConstants.HIDDEN, !visible);
        }
    }
}