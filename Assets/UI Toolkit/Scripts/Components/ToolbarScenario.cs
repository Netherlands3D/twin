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
        private const string ScenarioToggleClassName = "toolbar-scenario__toggle";

        private readonly VisualElement toggleGroup;
        private readonly Dictionary<FolderPropertyData, Toggle> togglesByKey = new();

        private Toggle activeToggle;
        public UnityEvent<FolderPropertyData> SelectionChanged = new();

        public ToolbarScenario()
        {
            this.CloneComponentTree("Components");
            this.AddComponentStylesheet("Components");

            toggleGroup = this.Q<VisualElement>("ScenarioToggleGroup");

            SetVisible(false);
        }

        public void InsertFolder(FolderPropertyData key, int index, string label, bool isScenario)
        {
            var toggle = new Toggle
            {
                name = $"ScenarioToggle-{togglesByKey.Count}",
                LabelText = label,
                userData = key
            };

            toggle.AddToClassList(ScenarioToggleClassName);

            var clampedIndex = Mathf.Clamp(index, 0, toggleGroup.childCount);
            toggleGroup.Insert(clampedIndex, toggle);
            togglesByKey.Add(key, toggle);
            ApplyScenarioVisibility(toggle, isScenario);
            toggle.RegisterValueChangedCallback(OnValueChanged);
            SetVisible(HasAnyVisibleToggle());
        }

        private void OnValueChanged(ChangeEvent<bool> evt)
        {
            var toggle  = evt.target as Toggle;
            var key = toggle.userData as FolderPropertyData;

            // if (activeToggle == toggle)
            // {
            //     if(evt.newValue)
            //         SelectionChanged.Invoke(key); 
            //     else
            //         SelectionChanged.Invoke(null);
            //     return;
            // }
        
            activeToggle?.SetValueWithoutNotify(false);
            if(evt.newValue)
            {
                activeToggle = toggle;
                SelectionChanged.Invoke(key);
            }
            else
                SelectionChanged.Invoke(null);
        }

        public void RemoveFolder(FolderPropertyData key)
        {
            if (!togglesByKey.TryGetValue(key, out var toggle))
                return;

            var wasSelected = toggle.value;

            toggleGroup.Remove(toggle);
            togglesByKey.Remove(key);

            if (wasSelected)
                SelectionChanged.Invoke(null);

            SetVisible(HasAnyVisibleToggle());
        }

        public void SetFolderIndex(FolderPropertyData key, int newIndex)
        {
            var toggle = togglesByKey[key];
            toggleGroup.Remove(toggle);
            var clampedIndex = Mathf.Clamp(newIndex, 0, toggleGroup.childCount);
            toggleGroup.Insert(clampedIndex, toggle);
        }
        
        public void SetFolderName(FolderPropertyData key, string newName)
        {
            var toggle = togglesByKey[key];
            toggle.LabelText = newName;
        }

        public void SetScenarioVisible(FolderPropertyData key, bool isScenario)
        {
            if (!togglesByKey.TryGetValue(key, out var toggle))
                return;

            var wasSelected = toggle.value;
            ApplyScenarioVisibility(toggle, isScenario);

            // A toggle that just stopped being a scenario can't stay selected.
            if (!isScenario && wasSelected)
            {
                SetSelectedFolderWithoutNotify(null);
                SelectionChanged.Invoke(null);
            }

            SetVisible(HasAnyVisibleToggle());
        }

        public void SetSelectedFolderWithoutNotify(FolderPropertyData key)
        {
            if(activeToggle != null)
                activeToggle.SetValueWithoutNotify(false);
            
            if (key != null && togglesByKey.TryGetValue(key, out var toggle))
            {
                toggle.SetValueWithoutNotify(true);
                activeToggle = toggle;
            }
        }

        private static void ApplyScenarioVisibility(Toggle toggle, bool isScenario)
        {
            toggle.EnableInClassList(UtilityClassConstants.HIDDEN, !isScenario);
        }
        

        private bool HasAnyVisibleToggle()
        {
            return togglesByKey.Values.Any(b => !b.ClassListContains(UtilityClassConstants.HIDDEN));
        }

        private void SetVisible(bool visible)
        {
            EnableInClassList(UtilityClassConstants.HIDDEN, !visible);
        }
    }
}