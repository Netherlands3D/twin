using System.Collections.Generic;
using System.Linq;
using Netherlands3D.Twin.Layers.LayerTypes;
using Netherlands3D.Twin.Layers.Properties;
using Netherlands3D.Twin.UI.LayerInspector;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.EventSystems;

namespace Netherlands3D.Twin.Layers
{
    public class HierarchicalObjectLayer : ReferencedLayer, IPointerClickHandler, ILayerWithProperties
    {
        private ToggleScatterPropertySectionInstantiator toggleScatterPropertySectionInstantiator;
        [SerializeField] private UnityEvent<GameObject> objectCreated = new();
        private List<IPropertySectionInstantiator> propertySections = new();

        public override bool IsActiveInScene
        {
            get => gameObject.activeSelf;
            set
            {
                gameObject.SetActive(value);
                ReferencedProxy.UI?.MarkLayerUIAsDirty();
            }
        }

        private void OnEnable()
        {
            ClickNothingPlane.ClickedOnNothing.AddListener(OnMouseClickNothing);
        }

        protected override void Awake()
        {
            propertySections = GetComponents<IPropertySectionInstantiator>().ToList();
            toggleScatterPropertySectionInstantiator = GetComponent<ToggleScatterPropertySectionInstantiator>();
            base.Awake();
        }

        private void Start()
        {
            objectCreated.Invoke(gameObject);
        }

        private void OnMouseClickNothing()
        {
            if (ReferencedProxy.UI.IsSelected)
            {
                ReferencedProxy.UI.Deselect();
            }
        }

        public void OnPointerClick(PointerEventData eventData)
        {
            if (ReferencedProxy.UI == null) return;

            ReferencedProxy.UI.Select(true);
        }

        public override void OnSelect()
        {
            var transformInterfaceToggle = FindAnyObjectByType<TransformHandleInterfaceToggle>(FindObjectsInactive.Include); //todo remove FindObjectOfType

            if (transformInterfaceToggle)
                transformInterfaceToggle.SetTransformTarget(gameObject);
        }

        public override void OnDeselect()
        {
            var transformInterfaceToggle = FindAnyObjectByType<TransformHandleInterfaceToggle>(FindObjectsInactive.Include);

            if (transformInterfaceToggle)
                transformInterfaceToggle.ClearTransformTarget();
        }

        public List<IPropertySectionInstantiator> GetPropertySections()
        {
            return propertySections;
        }

        public override void OnProxyTransformParentChanged()
        {
            // if (ReferencedProxy.ParentLayer is PolygonSelectionLayer)
            //     ConvertToScatterLayer(this);

            if (toggleScatterPropertySectionInstantiator.PropertySection != null)
                toggleScatterPropertySectionInstantiator.PropertySection?.TogglePropertyToggle();
        }

        public static ObjectScatterLayer ConvertToScatterLayer(HierarchicalObjectLayer objectLayer)
        {
            print("converting to scatter layer");
            var scatterLayer = new GameObject(objectLayer.name + "_Scatter");
            var layerComponent = scatterLayer.AddComponent<ObjectScatterLayer>();

            layerComponent.Initialize(objectLayer.gameObject, objectLayer.ReferencedProxy.ParentLayer as PolygonSelectionLayer, objectLayer.ReferencedProxy.ActiveSelf, UnparentDirectChildren(objectLayer.ReferencedProxy));

            Destroy(objectLayer); //destroy the component, not the gameObject, because we need to save the original GameObject to allow us to convert back 
            return layerComponent;
        }

        private static List<LayerNL3DBase> UnparentDirectChildren(LayerNL3DBase layer)
        {
            var list = new List<LayerNL3DBase>();
            foreach (var child in layer.ChildrenLayers)
            {
                if (child.Depth == layer.Depth + 1)
                {
                    list.Add(child);
                }
            }

            foreach (var directChild in list)
            {
                directChild.SetParent(null);
            }

            return list;
        }
    }
}