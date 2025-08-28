using System;
using System.Collections.Generic;
using System.Linq;
using Netherlands3D.Coordinates;
using Netherlands3D.Services;
using Netherlands3D.Twin.FloatingOrigin;
using Netherlands3D.Twin.Layers.ExtensionMethods;
using Netherlands3D.Twin.Layers.LayerTypes.CartesianTiles;
using Netherlands3D.Twin.Layers.LayerTypes.HierarchicalObject.Properties;
using Netherlands3D.Twin.Layers.LayerTypes.Polygons;
using Netherlands3D.Twin.Layers.LayerTypes.Polygons.Properties;
using Netherlands3D.Twin.Layers.Properties;
using Netherlands3D.Twin.Projects;
using Netherlands3D.Twin.UI;
using Netherlands3D.Twin.Utility;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.EventSystems;

namespace Netherlands3D.Twin.Layers.LayerTypes.HierarchicalObject
{
    [RequireComponent(typeof(WorldTransform))]
    public class HierarchicalObjectLayerGameObject : LayerGameObject, IPointerClickHandler, ILayerWithPropertyPanels, ILayerWithPropertyData
    {
        public override BoundingBox Bounds => CalculateWorldBoundsFromRenderers();

        private BoundingBox CalculateWorldBoundsFromRenderers()
        {
            var renderers = GetComponentsInChildren<Renderer>(); //needs to be optimized if we call this function every frame.
            if (renderers.Length == 0)
            {
                return null;
            }

            var combinedBounds = renderers[0].bounds;

            for (var i = 1; i < renderers.Length; i++)
            {
                var renderer = renderers[i];
                combinedBounds.Encapsulate(renderer.bounds);
            }

            var bl = new Coordinate(combinedBounds.min);
            var tr = new Coordinate(combinedBounds.max);
            return new BoundingBox(bl, tr);
        }

        private ToggleScatterPropertySectionInstantiator toggleScatterPropertySectionInstantiator;
        [SerializeField] private UnityEvent<GameObject> objectCreated = new();
        private List<IPropertySectionInstantiator> propertySections = new();
        protected TransformLayerPropertyData TransformPropertyData => LayerData.GetProperty<TransformLayerPropertyData>();
        private Coordinate previousCoordinate;
        private Quaternion previousRotation;
        private Vector3 previousScale;
        public WorldTransform WorldTransform { get; private set; }

        LayerPropertyData ILayerWithPropertyData.PropertyData => TransformPropertyData;
        public bool TransformIsSetFromProperty { get; private set; } = false;

        protected override void OnLayerInitialize()
        {
            WorldTransform = GetComponent<WorldTransform>();

            InitializePropertyData();

            propertySections = GetComponents<IPropertySectionInstantiator>().ToList();
            toggleScatterPropertySectionInstantiator = GetComponent<ToggleScatterPropertySectionInstantiator>();
        }

        protected virtual void InitializePropertyData()
        {
            LayerData.SetProperty(
                new TransformLayerPropertyData(
                    new Coordinate(transform.position), 
                    transform.eulerAngles, 
                    transform.localScale
                )
            );
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

        protected override void OnLayerReady()
        {
            WorldTransform.RecalculatePositionAndRotation();
            previousCoordinate = WorldTransform.Coordinate;
            previousRotation = WorldTransform.Rotation;
            previousScale = transform.localScale;
            
            objectCreated.Invoke(gameObject);

            //listen to property changes in start and OnDestroy because the object should still update its transform even when disabled
            TransformPropertyData.OnPositionChanged.AddListener(UpdatePosition);
            TransformPropertyData.OnRotationChanged.AddListener(UpdateRotation);
            TransformPropertyData.OnScaleChanged.AddListener(UpdateScale);
        }

        protected override void OnDestroy()
        {
            base.OnDestroy();
            TransformPropertyData?.OnPositionChanged.RemoveListener(UpdatePosition);
            TransformPropertyData?.OnRotationChanged.RemoveListener(UpdateRotation);
            TransformPropertyData?.OnScaleChanged.RemoveListener(UpdateScale);
        }

        private void UpdatePosition(Coordinate newPosition)
        {
            WorldTransform.MoveToCoordinate(newPosition);
        }

        private void UpdateRotation(Vector3 newAngles)
        {
            WorldTransform.SetRotation(Quaternion.Euler(newAngles));
        }

        private void UpdateScale(Vector3 newScale)
        {
            if (newScale == transform.localScale) return;
            
            transform.localScale = newScale;
        }

        public virtual void LoadProperties(List<LayerPropertyData> properties)
        {
            var transformProperty = properties.Get<TransformLayerPropertyData>();
            if (transformProperty == null) return;

            if (TransformPropertyData != null) //unsubscribe events from previous property object, resubscribe to new object at the end of this if block
            {
                TransformPropertyData.OnPositionChanged.RemoveListener(UpdatePosition);
                TransformPropertyData.OnRotationChanged.RemoveListener(UpdateRotation);
                TransformPropertyData.OnScaleChanged.RemoveListener(UpdateScale);
            }

            LayerData.SetProperty(transformProperty);

            UpdatePosition(transformProperty.Position);
            UpdateRotation(transformProperty.EulerRotation);
            UpdateScale(transformProperty.LocalScale);
            TransformIsSetFromProperty = true;

            transformProperty.OnPositionChanged.AddListener(UpdatePosition);
            transformProperty.OnRotationChanged.AddListener(UpdateRotation);
            transformProperty.OnScaleChanged.AddListener(UpdateScale);
        }
        
        protected virtual void Update()
        {
            //Position and rotation changes are handled by the WorldTransform, but should be updated in the project data
            //todo: add a == and != operator to Coordinate.cs to avoid having to do this
            if(Math.Abs(WorldTransform.Coordinate.value1 - previousCoordinate.value1) > 0.0001d ||
               Math.Abs(WorldTransform.Coordinate.value2 - previousCoordinate.value2) > 0.0001d ||
               Math.Abs(WorldTransform.Coordinate.value3 - previousCoordinate.value3) > 0.0001d)
            {
                TransformPropertyData.Position = WorldTransform.Coordinate;
                previousCoordinate = WorldTransform.Coordinate;
            }
            
            if (WorldTransform.Rotation != previousRotation)
            {
                TransformPropertyData.EulerRotation = WorldTransform.Rotation.eulerAngles;
                previousRotation = WorldTransform.Rotation;
            }
            
            // Check for scale change
            if (transform.localScale != previousScale)
            {
                TransformPropertyData.LocalScale = transform.localScale;
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
            if (!LayerData.IsSelected) return;

            LayerData.DeselectLayer();
        }

        public void OnPointerClick(PointerEventData eventData)
        {
            LayerData.SelectLayer(true);
        }

        public override void OnSelect()
        {
            var transformInterfaceToggle = ServiceLocator.GetService<TransformHandleInterfaceToggle>();

            if (!transformInterfaceToggle)
            {
                Debug.LogError("Transform handles interface toggles not found, cannot set transform target");
                return;
            }

            transformInterfaceToggle.SetTransformTarget(gameObject);
        }

        public override void OnDeselect()
        {
            ClearTransformHandles();
        }

        protected void ClearTransformHandles()
        {
            var transformInterfaceToggle = ServiceLocator.GetService<TransformHandleInterfaceToggle>();

            if (transformInterfaceToggle)
                transformInterfaceToggle.ClearTransformTarget();
        }

        public List<IPropertySectionInstantiator> GetPropertySections()
        {
            return propertySections;
        }

        public override void OnProxyTransformParentChanged()
        {
            // TODO: Is this a valid scenario - or worthy of an error message?
            if (!toggleScatterPropertySectionInstantiator) return;
            if (!toggleScatterPropertySectionInstantiator.PropertySection) return;

            toggleScatterPropertySectionInstantiator.PropertySection.TogglePropertyToggle();
        }

        public static ObjectScatterLayerGameObject ConvertToScatterLayer(HierarchicalObjectLayerGameObject objectLayerGameObject)
        {
            // TODO: Use LayerSpawner or App.Layers to replace the gameobject
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

        public override void ApplyStyling()
        {
            // Dynamically create a list of Layer features because a different set of renderers could be present after
            // an import or replacement.
            var features = CreateFeaturesByType<MeshRenderer>();
            
            // Apply style to the features that was discovered
            foreach (var feature in features)
            {
                HierarchicalObjectTileLayerStyler.Apply(this, GetStyling(feature), feature);
            }
            
            base.ApplyStyling();
        }
    }
}