using Netherlands3D.UI.ExtensionMethods;
using System.Collections.Generic;
using System.Linq;
using Netherlands3D.UI_Toolkit.Scripts;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UIElements;

namespace Netherlands3D.UI.Components
{
    [UxmlElement]
    public partial class DropDown : DropdownField
    {
       
        // Query and cache icon component
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
                    //check if the popuparea is present, if not there is no popup at all
                    if (rootSettings.childCount > 1)
                    {
                        VisualElement popupArea = rootSettings.ElementAt(1);
                        popupArea.RemoveFromClassList("unity-base-dropdown");
                        
                        //we need to block and consume the first pointer up or the popup will close immediately
                        ConsumePointer(evt, popupArea);
                        
                        popup = popupArea.ElementAt(0);
                        popup.UnregisterCallback<GeometryChangedEvent>(SetPopupPosition);
                        popup.RegisterCallback<GeometryChangedEvent>(SetPopupPosition);
                        popup.RemoveFromClassList("unity-base-dropdown__container-outer");
                        var styleSheet = Resources.Load<StyleSheet>($"UI/Components/DropDown-style");
                        popup.styleSheets.Add(styleSheet);
                        popup.AddToClassList("dropdown-popup-container");
                        
                        List<VisualElement> items = popup.Query<VisualElement>(className: "unity-base-dropdown__item").ToList();
                        for (int i = 0; i < items.Count; i++)
                        {
                            VisualElement item = items[i];
                            item.AddToClassList("dropdown-popup-item");
                            if(item == items.First())
                                item.AddToClassList("dropdown-popup-item__first-item");
                            else if(item == items.Last())
                                item.AddToClassList("dropdown-popup-item__last-item");
                            else
                                item.AddToClassList("dropdown-popup-item__middle-item");
                            
                            item.Clear();
                            Icon icon = new Icon();
                            icon.pickingMode = PickingMode.Ignore;
                            icon.Image = valueIcons[i];
                            
                            float height = contentContainer.resolvedStyle.height;
                            item.style.height = height;
                            item.Add(icon);
                        };
                    }
                }); 
            });

            this.RegisterValueChangedCallback(evt =>
            {
                //update the main icon after the choice change
                SetValue(index);
            });
        }

        private void ConsumePointer(PointerDownEvent evt, VisualElement area)
        {
            pointerConsumeCallback = (evt) =>
            {
                evt.StopImmediatePropagation();
                area.UnregisterCallback(pointerConsumeCallback, TrickleDown.TrickleDown);
            };
            area.RegisterCallback(pointerConsumeCallback, TrickleDown.TrickleDown);
        }

        public void SetValue(int index)
        {
            Icon.Image = valueIcons[index]; 
        }

        private void SetPopupPosition(GeometryChangedEvent evt)
        {
            float width = contentContainer.resolvedStyle.width;
            popup.style.width = width;
            float left = contentContainer.worldBound.x;
            float top = contentContainer.worldBound.y;
            popup.style.left = left;
            popup.style.top = top;
        }

        public void SetValueIcons(List<IconImage> values)
        {
            valueIcons = values;
        }
    }
}