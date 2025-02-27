using Netherlands3D.E2ETesting.PageObjectModel;
using Netherlands3D.E2ETesting.UI;
using Netherlands3D.Twin.Layers.UI.HierarchyInspector;
using UnityEngine;
using UnityEngine.UI;

namespace Netherlands3D.Twin.Tests.PageObjectModel.InspectorPanels
{
    public class LayersPanel : InspectorPanel<LayerUIManager>
    {
        public class LayerListItemElement : Element<GameObject>
        {
            public ToggleElement Visibility;
            public Element<LayerUI> LayerUI;

            public LayerListItemElement(GameObject value) : base(value)
            {
                Visibility = new(GameObject("ParentRow/EnableToggle").Component<Toggle>().Value);
                LayerUI = Component<LayerUI>();
            }

            public override bool IsActive => base.IsActive && LayerUI.Value.Layer.ActiveInHierarchy;
        }

        public LayerListItemElement Maaiveld { get; }
        
        public LayersPanel(LayerUIManager value) : base(value)
        {
            Maaiveld = new LayerListItemElement(GameObject("Layers/Maaiveld").Value);
        }
    }
}