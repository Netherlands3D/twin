using Netherlands3D.UI.ExtensionMethods;
using System.Collections.Generic;
using System.Linq;
using Netherlands3D.UI_Toolkit.Scripts;
using UnityEngine;
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
            
            RegisterCallback<MouseDownEvent>(evt =>
            {
                schedule.Execute(() =>
                {
                    //check if the popuparea is present, if not there is no popup at all
                    if (rootSettings.childCount > 1)
                    {
                        VisualElement popupArea = rootSettings.ElementAt(1);
                        popupArea.RemoveFromClassList("unity-base-dropdown");
                        
                        popup = popupArea.ElementAt(0);
                        popup.UnregisterCallback<GeometryChangedEvent>(SetPopupPosition);
                        popup.RegisterCallback<GeometryChangedEvent>(SetPopupPosition);
                        popup.RemoveFromClassList("unity-base-dropdown__container-outer");
                        var styleSheet = Resources.Load<StyleSheet>($"UI/Components/DropDown-style");
                        popup.styleSheets.Add(styleSheet);
                        popup.AddToClassList("dropdown-popup-container");
                        
                        List<VisualElement> items = popup.Query<VisualElement>(className: "unity-base-dropdown__item").ToList();
                        items.ForEach(i =>
                        {
                            i.AddToClassList("dropdown-popup-item");
                            if(i == items.First())
                                i.AddToClassList("dropdown-popup-item__first-item");
                            else if(i == items.Last())
                                i.AddToClassList("dropdown-popup-item__last-item");
                            else
                                i.AddToClassList("dropdown-popup-item__middle-item");
                            
                            i.Clear();
                            //i.Q<Label>().text = "";
                            Icon icon = new Icon();
                            icon.pickingMode = PickingMode.Ignore;
                            icon.Image = IconImage.KeyTokenCode;
                            
                            float height = contentContainer.resolvedStyle.height;
                            i.style.height = height;
                            i.Add(icon);
                        });
                    }
                }); 
            });

            this.RegisterValueChangedCallback(evt =>
            {
                Debug.Log(rootSettings.childCount);
            });
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
    }
}