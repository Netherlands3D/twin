using System.Collections.Generic;
using System.Linq;
using Netherlands3D.Coordinates;
using Netherlands3D.Twin.Layers.LayerTypes.Polygons;
using Netherlands3D.Twin.Layers.Properties;
using Netherlands3D.Twin.Projects;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.EventSystems;

namespace Netherlands3D.Twin.Layers.LayerTypes
{
    public class HierarchicalObjectLayerGameObject : LayerGameObject, IPointerClickHandler, ILayerWithPropertyPanels, ILayerWithPropertyData
    {
        private ToggleScatterPropertySectionInstantiator toggleScatterPropertySectionInstantiator;
        [SerializeField] private UnityEvent<GameObject> objectCreated = new();
        private List<IPropertySectionInstantiator> propertySections = new();
        protected TransformLayerPropertyData transformPropertyData;
        private Vector3 previousPosition;
        private Quaternion previousRotation;
        private Vector3 previousScale;

        LayerPropertyData ILayerWithPropertyData.PropertyData => transformPropertyData;
        public bool TransformIsSetFromProperty { get; private set; } = false;

        protected void Awake()
        {
            transformPropertyData = new TransformLayerPropertyData(new Coordinate(transform.position), transform.eulerAngles, transform.localScale);
         
            propertySections = GetComponents<IPropertySectionInstantiator>().ToList();
            toggleScatterPropertySectionInstantiator = GetComponent<ToggleScatterPropertySectionInstantiator>();
        }

        protected override void OnEnable()
        {
            base.OnEnable();
            ClickNothingPlane.ClickedOnNothing.AddListener(OnMouseClickNothing);
        }

        protected override void OnDisable()
        {
            base.OnDisable();
            ClickNothingPlane.ClickedOnNothing.RemoveListener(OnMouseClickNothing);
        }

        protected override void Start()
        {
            base.Start();
            previousPosition = transform.position;
            previousRotation = transform.rotation;
            previousScale = transform.localScale;

            objectCreated.Invoke(gameObject);

            //listen to property changes in start and OnDestroy because the object should still update its transform even when disabled
            transformPropertyData.OnPositionChanged.AddListener(UpdatePosition);
            transformPropertyData.OnRotationChanged.AddListener(UpdateRotation);
            transformPropertyData.OnScaleChanged.AddListener(UpdateScale);
        }

        protected void OnDestroy()
        {
            transformPropertyData.OnPositionChanged.RemoveListener(UpdatePosition);
            transformPropertyData.OnRotationChanged.RemoveListener(UpdateRotation);
            transformPropertyData.OnScaleChanged.RemoveListener(UpdateScale);
        }

        protected virtual void UpdatePosition(Coordinate newPosition)
        {
            if (newPosition.ToUnity() != transform.position)
                transform.position = newPosition.ToUnity();
        }

        protected void UpdateRotation(Vector3 newAngles)
        {
            if (newAngles != transform.eulerAngles)
                transform.eulerAngles = newAngles;
        }

        protected void UpdateScale(Vector3 newScale)
        {
            if (newScale != transform.localScale)
                transform.localScale = newScale;
        }

        public virtual void LoadProperties(List<LayerPropertyData> properties)
        {
            var transformProperty = (TransformLayerPropertyData)properties.FirstOrDefault(p => p is TransformLayerPropertyData);
            if (transformProperty != null)
            {
                if (transformPropertyData != null) //unsubscribe events from previous property object, resubscribe to new object at the end of this if block
                {
                    transformPropertyData.OnPositionChanged.RemoveListener(UpdatePosition);
                    transformPropertyData.OnRotationChanged.RemoveListener(UpdateRotation);
                    transformPropertyData.OnScaleChanged.RemoveListener(UpdateScale);
                }

                this.transformPropertyData = transformProperty; //take existing TransformProperty to overwrite the unlinked one of this class

                UpdatePosition(this.transformPropertyData.Position);
                UpdateRotation(this.transformPropertyData.EulerRotation);
                UpdateScale(this.transformPropertyData.LocalScale);
                TransformIsSetFromProperty = true;
                
                transformPropertyData.OnPositionChanged.AddListener(UpdatePosition);
                transformPropertyData.OnRotationChanged.AddListener(UpdateRotation);
                transformPropertyData.OnScaleChanged.AddListener(UpdateScale);
            }
        }

        private void Update()
        {
            // We cannot use transform.hasChanged, because this flag is not correctly set when adjusting this transform using runtimeTransformHandles, instead we have to compare the values directly
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
            var scatterPrefab = ProjectData.Current.PrefabLibrary.GetPrefabById(ObjectScatterLayerGameObject.ScatterBasePrefabID);
            var scatterLayer = Instantiate(scatterPrefab) as ObjectScatterLayerGameObject;
            scatterLayer.Name = objectLayerGameObject.Name + "_Scatter";
            scatterLayer.Initialize(objectLayerGameObject, objectLayerGameObject.LayerData.ParentLayer as PolygonSelectionLayer);
            for (var i = objectLayerGameObject.LayerData.ChildrenLayers.Count - 1; i >= 0; i--) //go in reverse to avoid a collectionWasModifiedError
            {
                var child = objectLayerGameObject.LayerData.ChildrenLayers[i];
                child.SetParent(scatterLayer.LayerData, 0);
            }

            objectLayerGameObject.LayerData.DestroyLayer();
            return scatterLayer;
        }
    }
}