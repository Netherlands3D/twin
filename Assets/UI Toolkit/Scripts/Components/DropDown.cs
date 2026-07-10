using Netherlands3D.UI.ExtensionMethods;
using System.Collections.Generic;
using Netherlands3D.UI_Toolkit.Scripts;
using UnityEngine.Events;
using UnityEngine.UIElements;
using Netherlands3D.UI_Toolkit;

namespace Netherlands3D.UI.Components
{
    [UxmlElement]
    public partial class DropDown : DropdownField
    {
        public UnityEvent<int> DropDownValueChanged = new();
        
        private Icon icon;
        private Label label;

        private VisualElement rootSettings;
        private VisualElement popup;
        
        private List<IconImage> valueIcons;

        public enum DropDownStyle
        {
            Icons,
            Text
        }

        [UxmlAttribute("dropdown-style")]
        public DropDownStyle Style
        {
            get => dropDownStyle;
            set => dropDownStyle = value;
        }

        private DropDownStyle dropDownStyle = DropDownStyle.Icons;

        public DropDown()
        {
            this.CloneComponentTree("Components");
            this.AddComponentStylesheet("Components");

            label = this.Q<Label>();
            icon = this.Q<Icon>();

            RegisterCallback<AttachToPanelEvent>(evt =>
            {
                //find the panel settings root not the ui root
                VisualElement root = this;
                while (root.parent != null)
                    root = root.parent;

                rootSettings = root;
                
                icon.pickingMode = PickingMode.Ignore;
                label.pickingMode = PickingMode.Ignore;

                label.EnableInClassList(UtilityClassConstants.HIDDEN, dropDownStyle != DropDownStyle.Text);
                icon.EnableInClassList(UtilityClassConstants.HIDDEN, dropDownStyle != DropDownStyle.Icons);
            });
            
            RegisterCallback<PointerDownEvent>(evt =>
            {
                //wait a frame to have the popup instantiated
                schedule.Execute(() =>
                {
                    VisualElement popupArea = rootSettings.Query<VisualElement>(className: "unity-base-dropdown");
                    //check if the popuparea is present, if not there is no popup at all
                    if (popupArea != null)
                    {
                        popup = rootSettings.Query<VisualElement>(className: "unity-base-dropdown__container-outer");
                        popup.RegisterCallback<GeometryChangedEvent>(SetPopupPosition);
                        popup.AddComponentStylesheetByType(GetType());
                        
                        List<VisualElement> items = popup.Query<VisualElement>(className: "unity-base-dropdown__item").ToList();
                        for (int i = 0; i < items.Count; i++)
                        {
                            VisualElement item = items[i];
                            CreateDropdownItem(item, i, items.Count);
                        };
                    }
                }); 
            });

            this.RegisterValueChangedCallback(evt =>
            {
                //update the main icon after the choice change
                SetValue(index);
                DropDownValueChanged.Invoke(index);
            });
        }
        
        private void CreateDropdownItem(VisualElement item, int index, int total)
        {
            if(index == 0)
                item.AddToClassList("dropdown-popup-item__first-item");
            else if(index == total - 1)
                item.AddToClassList("dropdown-popup-item__last-item");
                
            item.Clear();
            switch(dropDownStyle)
            {
                case DropDownStyle.Icons:
                    Icon icon = new Icon();
                    icon.pickingMode = PickingMode.Ignore;
                    icon.Image = valueIcons[index];
                    item.Add(icon);
                    break;
                case DropDownStyle.Text:
                    Label text = new Label(choices[index]);
                    text.pickingMode = PickingMode.Ignore;
                    item.Add(text);
                    break;
            }           
        }

        public void SetValue(int index)
        {
            value = choices[index];
            if (dropDownStyle == DropDownStyle.Icons)
            {
                icon.Image = valueIcons[index];
            }
            else if (dropDownStyle == DropDownStyle.Text)
            {
                label.text = choices[index];
            }
        }

        private void SetPopupPosition(GeometryChangedEvent evt)
        {
            float width = contentContainer.resolvedStyle.width;
            popup.style.width = width;
            float left = contentContainer.worldBound.x;
            float top = contentContainer.worldBound.yMax;
            float border = contentContainer.resolvedStyle.borderBottomWidth;
            popup.style.top = top - border;
            popup.style.left = left;
        }

        public void SetValueIcons(List<IconImage> values)
        {
            valueIcons = values;
        }
    }
}