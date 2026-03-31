using Netherlands3D.UI_Toolkit.Scripts;
using Netherlands3D.UI.ExtensionMethods;
using System.Collections.Generic;
using System.Linq;
using Netherlands3D.Twin.Layers.LayerTypes;
using UnityEngine;
using UnityEngine.UIElements;

namespace Netherlands3D.UI.Components
{
    [UxmlElement]
    public partial class DropDown : DropdownField
    {
       
        private VisualElement rootSettings;
        private VisualElement popup;
        
        public DropDown()
        {
            this.CloneComponentTree("Components");
            this.AddComponentStylesheet("Components");
            
            this.RegisterCallback<AttachToPanelEvent>(evt =>
            {
                //find the panel settings root not the ui root
                VisualElement root = this;
                while (root.parent != null)
                    root = root.parent;

                rootSettings = root;
            });
            
            RegisterCallback<MouseDownEvent>(evt =>
            {
                schedule.Execute(() =>
                {
                    //check if the popuparea is present, if not there is no popup at all
                    if (rootSettings.childCount > 1)
                    {
                        VisualElement popupArea = rootSettings.ElementAt(1);
                        popup = popupArea.ElementAt(0);
                        var styleSheet = Resources.Load<StyleSheet>($"UI/Components/DropDown-style");
                        popup.styleSheets.Add(styleSheet);
                        popup.AddToClassList("dropdown-popup-container");

                        popup.Query<Scroller>().ToList().ForEach(scroller => scroller.RemoveFromHierarchy());
                    }
                }); 
            });

            this.RegisterValueChangedCallback(evt =>
            {
                Debug.Log(rootSettings.childCount);
            });
        }
        
        VisualElement GetRoot(VisualElement ve)
        {
            while (ve.parent != null)
                ve = ve.parent;

            return ve;
        }
    }
}