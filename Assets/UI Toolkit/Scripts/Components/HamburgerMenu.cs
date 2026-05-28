using System;
using System.Collections.Generic;
using Netherlands3D.UI.ExtensionMethods;
using UnityEngine.Events;
using UnityEngine.UIElements;

namespace Netherlands3D.UI.Components
{
    [UxmlElement]
    public partial class HamburgerMenu : Foldout
    {
        private const string ExpandedClassName = "is-expanded";
        private const string LastButtonClassName = "is-last";

        private VisualElement HeaderInput => this.Q<VisualElement>(className: "unity-toggle__input");
        private Label HeaderLabel => this.Q<Label>(className: "unity-label");
        private VisualElement Checkmark => this.Q<VisualElement>(className: "unity-foldout__checkmark");
        private VisualElement ButtonGroup => this.Q<VisualElement>("ButtonGroup");

        private Button OpenProjectButton => this.Q<Button>("Open");
        private Button SaveProjectButton => this.Q<Button>("Save");
        private Button SettingsButton => this.Q<Button>("Settings");
        private Button HelpButton => this.Q<Button>("Help");

        // public event Action OnOpenProjectSelected;
        // public event Action OnSaveProjectSelected;
        // public event Action OnSettingsSelected;
        // public event Action OnHelpSelected;

        public UnityEvent<ToolType> OnToolSelected = new();
        
        
        [UxmlAttribute("text")]
        public string ProjectTitle
        {
            get => text;
            set => text = value;
        }

        [UxmlAttribute("expanded")]
        public bool Expanded
        {
            get => value;
            set => this.value = value;
        }

        public HamburgerMenu()
        {
            this.CloneComponentTree("Components");
            this.AddComponentStylesheet("Components");

            if (string.IsNullOrEmpty(text))
                text = "Project titel";

            SetValueWithoutNotify(false);

            RegisterCallback<ChangeEvent<bool>>(OnFoldoutValueChanged);

            // OpenProjectButton.RegisterCallback<ClickEvent>(_ => OnOpenProjectSelected?.Invoke());
            // SaveProjectButton.RegisterCallback<ClickEvent>(_ => OnSaveProjectSelected?.Invoke());
            // SettingsButton.RegisterCallback<ClickEvent>(_ => OnSettingsSelected?.Invoke());
            // HelpButton.RegisterCallback<ClickEvent>(_ => OnHelpSelected?.Invoke());
            
            OpenProjectButton.RegisterCallback<ClickEvent>(_ => OnToolSelected.Invoke(ToolType.OpenProject));
            SaveProjectButton.RegisterCallback<ClickEvent>(_ => OnToolSelected.Invoke(ToolType.SaveProject));
            SettingsButton.RegisterCallback<ClickEvent>(_ => OnToolSelected.Invoke(ToolType.Settings));
            HelpButton.RegisterCallback<ClickEvent>(_ => OnToolSelected.Invoke(ToolType.Help));

            RegisterCallback<AttachToPanelEvent>(_ =>
            {
                MoveButtonGroupIntoContent();
                ReorderHeaderChildren();

                UpdateExpandedClass(value);

                // Mark last button (for override border radius)
                schedule.Execute(UpdateLastButtonClass).ExecuteLater(0);
            });
        }

        public void Open()
        {
            value = true;
        }

        public void Close()
        {
            value = false;
        }

        private void OnFoldoutValueChanged(ChangeEvent<bool> evt)
        {
            if (evt.target != this) return;

            UpdateExpandedClass(evt.newValue);
            schedule.Execute(UpdateLastButtonClass).ExecuteLater(0);
        }

        private void UpdateExpandedClass(bool isExpanded)
        {
            EnableInClassList(ExpandedClassName, isExpanded);
        }

        // Move UXML button group to contentContainer inside the foldout
        private void MoveButtonGroupIntoContent()
        {
            var group = ButtonGroup;
            if (group == null) return;

            if (group.parent == contentContainer) return;

            group.RemoveFromHierarchy();
            contentContainer.Add(group);
        }

        // Move checkmark (hamburger button) and label to the front
        private void ReorderHeaderChildren()
        {
            var input = HeaderInput;
            if (input == null) return;

            var check = Checkmark;
            var label = HeaderLabel;
            if (check == null || label == null) return;

            if (check.parent != input) input.Add(check);
            if (label.parent != input) input.Add(label);

            input.Insert(0, check);
            input.Insert(1, label);
        }

        private void UpdateLastButtonClass()
        {
            var group = ButtonGroup;
            if (group == null) return;

            var buttons = new List<VisualElement>();
            group.Query<VisualElement>(className: "button").ToList(buttons);

            if (buttons.Count == 0) return;

            for (int i = 0; i < buttons.Count; i++)
                buttons[i].RemoveFromClassList(LastButtonClassName);

            buttons[buttons.Count - 1].AddToClassList(LastButtonClassName);
        }
    }
}