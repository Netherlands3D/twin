using System.Collections.Generic;
using System.Linq;
using Netherlands3D.Twin.Layers.LayerTypes;
using Netherlands3D.Twin.Layers.Properties;
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

        protected void Awake()
        {
            propertySections = GetComponents<IPropertySectionInstantiator>().ToList();
            toggleScatterPropertySectionInstantiator = GetComponent<ToggleScatterPropertySectionInstantiator>();
        }

        protected override void OnEnable()
        {
            base.OnEnable();
            ClickNothingPlane.ClickedOnNothing.AddListener(OnMouseClickNothing);
        }

        private void Start()
        {
            objectCreated.Invoke(gameObject);
        }

        public override void OnLayerActiveInHierarchyChanged(bool isActive)
        {
            if (!isActive && ReferencedProxy.IsSelected)
            {
                ReferencedProxy.DeselectLayer();
            }

            gameObject.SetActive(isActive);
        }

        private void OnMouseClickNothing()
        {
            if (ReferencedProxy.IsSelected)
            {
                ReferencedProxy.DeselectLayer();
            }
        }

        public void OnPointerClick(PointerEventData eventData)
        {
            ReferencedProxy.SelectLayer(true);
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
            if (toggleScatterPropertySectionInstantiator.PropertySection != null)
                toggleScatterPropertySectionInstantiator.PropertySection?.TogglePropertyToggle();
        }

        public static ObjectScatterLayer ConvertToScatterLayer(HierarchicalObjectLayer objectLayer)
        {
            print("converting to scatter layer");
            var scatterLayer = new GameObject(objectLayer.Name + "_Scatter");
            var layerComponent = scatterLayer.AddComponent<ObjectScatterLayer>();

            var originalGameObject = objectLayer.gameObject;
            objectLayer.ReferencedProxy.KeepReferenceOnDestroy = true;
            Destroy(objectLayer); //destroy the component, not the gameObject, because we need to save the original GameObject to allow us to convert back 
            layerComponent.Initialize(originalGameObject, objectLayer.ReferencedProxy.ParentLayer as PolygonSelectionLayer, UnparentDirectChildren(objectLayer.ReferencedProxy));

            return layerComponent;
        }

        private static List<LayerNL3DBase> UnparentDirectChildren(LayerNL3DBase layer)
        {
            var list = new List<LayerNL3DBase>();
            foreach (var child in layer.ChildrenLayers)
            {
                list.Add(child);
            }

            foreach (var directChild in list)
            {
                directChild.SetParent(null);
            }

            return list;
        }
    }
}