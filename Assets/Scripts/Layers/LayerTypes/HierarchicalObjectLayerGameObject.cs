using System;
using System.Collections.Generic;
using System.Linq;
using Netherlands3D.Coordinates;
using Netherlands3D.Twin.Layers.LayerTypes;
using Netherlands3D.Twin.Layers.Properties;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.EventSystems;

namespace Netherlands3D.Twin.Layers
{
    public class HierarchicalObjectLayerGameObject : LayerGameObject, IPointerClickHandler, ILayerWithPropertyPanels, ILayerWithPropertyData
    {
        private ToggleScatterPropertySectionInstantiator toggleScatterPropertySectionInstantiator;
        [SerializeField] private UnityEvent<GameObject> objectCreated = new();
        private List<IPropertySectionInstantiator> propertySections = new();
        private TransformLayerPropertyData transformPropertyData = new();
        private Vector3 previousPosition;
        private Quaternion previousRotation;
        private Vector3 previousScale;
        
        LayerPropertyData ILayerWithPropertyData.PropertyData => transformPropertyData;

        protected void Awake()
        {
            transformPropertyData.Position = new Coordinate(CoordinateSystem.Unity, transform.position.x, transform.position.y, transform.position.z);
            transformPropertyData.EulerRotation = transform.eulerAngles;
            transformPropertyData.LocalScale = transform.localScale;

            propertySections = GetComponents<IPropertySectionInstantiator>().ToList();
            toggleScatterPropertySectionInstantiator = GetComponent<ToggleScatterPropertySectionInstantiator>();
        }

        protected override void OnEnable()
        {
            base.OnEnable();
            ClickNothingPlane.ClickedOnNothing.AddListener(OnMouseClickNothing);
            transformPropertyData.OnPositionChanged.AddListener(UpdatePosition);
            transformPropertyData.OnRotationChanged.AddListener(UpdateRotation);
            transformPropertyData.OnScaleChanged.AddListener(UpdateScale);
        }

        protected override void OnDisable()
        {
            base.OnDisable();
            transformPropertyData.OnPositionChanged.RemoveListener(UpdatePosition);
            transformPropertyData.OnRotationChanged.RemoveListener(UpdateRotation);
            transformPropertyData.OnScaleChanged.RemoveListener(UpdateScale);
        }

        private void UpdatePosition(Coordinate newPosition)
        {
            if (newPosition.ToUnity() != transform.position)
                transform.position = newPosition.ToUnity();
        }

        private void UpdateRotation(Vector3 newAngles)
        {
            if (newAngles != transform.eulerAngles)
                transform.eulerAngles = newAngles;
        }

        private void UpdateScale(Vector3 newScale)
        {
            if (newScale != transform.localScale)
                transform.localScale = newScale;
        }
        
        public void LoadProperties(List<LayerPropertyData> properties)
        {
            var transformProperty = (TransformLayerPropertyData)properties.FirstOrDefault(p => p is TransformLayerPropertyData);
            if (transformProperty != null)
            {
                this.transformPropertyData = transformProperty; //take existing TransformProperty to overwrite the unlinked one of this class

                UpdatePosition(this.transformPropertyData.Position);
                UpdateRotation(this.transformPropertyData.EulerRotation);
                UpdateScale(this.transformPropertyData.LocalScale);
            }
        }

        protected override void Start()
        {
            base.Start();
            previousPosition = transform.position;
            previousRotation = transform.rotation;
            previousScale = transform.localScale;

            objectCreated.Invoke(gameObject);
        }

        private void Update()
        {
            // We cannot user transform.hasChanged, because this flag is not correctly set when adjusting this transform using runtimeTransformHandles, instead we have to compare the values directly
            // Check for position change
            if (transform.position != previousPosition)
            {
                var rdCoordinate = new Coordinate(CoordinateSystem.Unity, transform.position.x, transform.position.y, transform.position.z);
                transformPropertyData.Position = rdCoordinate;
                previousPosition = transform.position;
            }

            // Check for rotation change
            if (transform.rotation != previousRotation)
            {
                transformPropertyData.EulerRotation = transform.eulerAngles;
                previousRotation = transform.rotation;
            }

            // Check for scale change
            if (transform.localScale != previousScale)
            {
                transformPropertyData.LocalScale = transform.localScale;
                previousScale = transform.localScale;
            }
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