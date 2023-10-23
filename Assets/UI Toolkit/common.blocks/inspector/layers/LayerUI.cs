using System.Collections;
using System.Collections.Generic;
using UnityEditor.ProjectWindowCallback;
using UnityEngine;
using UnityEngine.UIElements;

namespace Netherlands3D.Twin.UI.Inpector.Layers
{
    public class LayerUI : VisualElement
    {
        [UnityEngine.Scripting.Preserve]
        public new class UxmlFactory : UxmlFactory<LayerUI> { }
        
        public VisualElement parentRow;
        public Toggle enableToggle;
        public Toggle foldoutToggle;
        public Button colorButton;
        public Image icon;
        public Label textLabel;
        public VisualElement contentContainer;
        
        private bool isExpanded = true;
        public bool IsExpanded
        {
            get { return isExpanded; }
            set
            {
                isExpanded = value;
                contentContainer.style.display = isExpanded ? DisplayStyle.Flex : DisplayStyle.None;
            }
        }
        
        public LayerUI()
        {
            parentRow = new VisualElement();
            parentRow.name = "layer";
            parentRow.AddToClassList("layerRow");
            hierarchy.Add(parentRow);
            
            contentContainer = new VisualElement();
            contentContainer.name = "unity-content";
            hierarchy.Add(contentContainer);

            enableToggle = new Toggle();
            enableToggle.name = "enableToggle";
            parentRow.Add(enableToggle);
            
            foldoutToggle = new Toggle();
            foldoutToggle.name = "foldoutToggle";
            parentRow.Add(foldoutToggle);
            foldoutToggle.RegisterValueChangedCallback(evt => IsExpanded = evt.newValue);

            colorButton = new Button();
            colorButton.name = "colorButton";
            parentRow.Add(colorButton);

            icon = new Image();
            icon.name = "icon";
            parentRow.Add(icon);
            
            textLabel = new Label();
            textLabel.name = "textLabel";
            textLabel.text = "Layer";
            parentRow.Add(textLabel);
        }
    }
}
