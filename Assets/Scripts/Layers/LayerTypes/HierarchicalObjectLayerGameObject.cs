using System.Collections.Generic;
using System.Linq;
using Netherlands3D.Twin.Layers.LayerTypes;
using Netherlands3D.Twin.Layers.Properties;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.EventSystems;

namespace Netherlands3D.Twin.Layers
{
    public class HierarchicalObjectLayerGameObject : LayerGameObject, IPointerClickHandler, ILayerWithProperties
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
            if (!isActive && LayerData.IsSelected)
            {
                LayerData.DeselectLayer();
            }

            gameObject.SetActive(isActive);
        }

        private void OnMouseClickNothing()
        {
            if (LayerData.IsSelected)
            {
                LayerData.DeselectLayer();
            }
        }

        public void OnPointerClick(PointerEventData eventData)
        {
            LayerData.SelectLayer(true);
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

        public static ObjectScatterLayerGameObject ConvertToScatterLayer(HierarchicalObjectLayerGameObject objectLayerGameObject)
        {
            print("converting to scatter layer");
            var scatterLayer = new GameObject(objectLayerGameObject.Name + "_Scatter");
            var layerComponent = scatterLayer.AddComponent<ObjectScatterLayerGameObject>();

            var originalGameObject = objectLayerGameObject.gameObject;
            objectLayerGameObject.LayerData.KeepReferenceOnDestroy = true;
            Destroy(objectLayerGameObject); //destroy the component, not the gameObject, because we need to save the original GameObject to allow us to convert back 
            layerComponent.Initialize(originalGameObject, objectLayerGameObject.LayerData.ParentLayer as PolygonSelectionLayer, UnparentDirectChildren(objectLayerGameObject.LayerData));

            return layerComponent;
        }

        private static List<LayerData> UnparentDirectChildren(LayerData layer)
        {
            var list = new List<LayerData>();
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