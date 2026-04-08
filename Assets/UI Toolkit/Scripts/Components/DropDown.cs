using Netherlands3D.UI.ExtensionMethods;
using System.Collections.Generic;
using Netherlands3D.UI_Toolkit.Scripts;
using UnityEngine.Events;
using UnityEngine.UIElements;

namespace Netherlands3D.UI.Components
{
    [UxmlElement]
    public partial class DropDown : DropdownField
    {
        public UnityEvent<int> DropDownValueChanged = new();
        
        private Icon icon;
        private Icon Icon => icon ??= this.Q<Icon>();
        
        private VisualElement rootSettings;
        private VisualElement popup;
        
        private List<IconImage> valueIcons;
        
        private EventCallback<PointerUpEvent> pointerConsumeCallback = null;
        
        public DropDown()
        {
            this.CloneComponentTree("Components");
            this.AddComponentStylesheet("Components");
            
            RegisterCallback<AttachToPanelEvent>(evt =>
            {
                //find the panel settings root not the ui root
                VisualElement root = this;
                while (root.parent != null)
                    root = root.parent;

                rootSettings = root;
                
                Icon.pickingMode = PickingMode.Ignore;
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
            Icon icon = new Icon();
            icon.pickingMode = PickingMode.Ignore;
            icon.Image = valueIcons[index];
            item.Add(icon);
        }

        public void SetValue(int index)
        {
            value = choices[index];
            Icon.Image = valueIcons[index]; 
        }

        private void SetPopupPosition(GeometryChangedEvent evt)
        {
            float width = contentContainer.resolvedStyle.width;
            popup.style.width = width;
            float left = contentContainer.worldBound.x;
            float top = contentContainer.worldBound.yMax;
            popup.style.left = left;
            popup.style.top = top;
        }

        public void SetValueIcons(List<IconImage> values)
        {
            valueIcons = values;
        }
    }
}