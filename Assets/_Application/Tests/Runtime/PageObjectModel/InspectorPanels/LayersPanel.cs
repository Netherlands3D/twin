using Netherlands3D.E2ETesting.PageObjectModel;
using Netherlands3D.E2ETesting.UI;
using Netherlands3D.Twin.Layers;
using Netherlands3D.Twin.Layers.UI.HierarchyInspector;
using UnityEngine;
using UnityEngine.UI;

namespace Netherlands3D.Twin.Tests.PageObjectModel.InspectorPanels
{
    public class LayersPanel : InspectorPanel<LayerUIManager, LayersPanel>
    {
        public class LayerListItemElement : Element<GameObject, LayerListItemElement>
        {
            public ToggleElement Visibility;
            private Element<LayerUI> LayerUiData;
            private LayerData LayerData;

            protected override void Setup()
            {
                Visibility = ToggleElement.For(GameObject("ParentRow/EnableToggle")?.Component<Toggle>());
                LayerUiData = Component<LayerUI>();
                LayerData = LayerUiData?.Value.Layer;
            }

            public override bool IsActive => base.IsActive && LayerData.ActiveInHierarchy;
        }

        public LayerListItemElement Maaiveld { get; private set; }

        protected override void Setup()
        {
            Maaiveld = LayerListItemElement.For(GameObject("Layers/Maaiveld")?.Value);
        }
    }
}