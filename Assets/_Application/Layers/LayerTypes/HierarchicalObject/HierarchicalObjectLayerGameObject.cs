using System.Collections.Generic;
using System.Linq;
using Netherlands3D.Coordinates;
using Netherlands3D.LayerStyles;
using Netherlands3D.LayerStyles.ExtensionMethods;
using Netherlands3D.Twin.FloatingOrigin;
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
        protected TransformLayerPropertyData transformPropertyData;
        private Coordinate previousCoordinate;
        private Quaternion previousRotation;
        private Vector3 previousScale;
        protected WorldTransform worldTransform;

        LayerPropertyData ILayerWithPropertyData.PropertyData => transformPropertyData;
        public bool TransformIsSetFromProperty { get; private set; } = false;

        protected virtual void Awake()
        {
            transformPropertyData = InitializePropertyData();

            propertySections = GetComponents<IPropertySectionInstantiator>().ToList();
            toggleScatterPropertySectionInstantiator = GetComponent<ToggleScatterPropertySectionInstantiator>();

            worldTransform = GetComponent<WorldTransform>();
        }

        protected virtual TransformLayerPropertyData InitializePropertyData()
        {
            return new TransformLayerPropertyData(new Coordinate(transform.position), transform.eulerAngles, transform.localScale);
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
            worldTransform.RecalculatePositionAndRotation();
            previousCoordinate = worldTransform.Coordinate;
            previousRotation = worldTransform.Rotation;
            previousScale = transform.localScale;
            
            objectCreated.Invoke(gameObject);

            //listen to property changes in start and OnDestroy because the object should still update its transform even when disabled
            transformPropertyData.OnPositionChanged.AddListener(UpdatePosition);
            transformPropertyData.OnRotationChanged.AddListener(UpdateRotation);
            transformPropertyData.OnScaleChanged.AddListener(UpdateScale);
        }

        protected override void OnDestroy()
        {
            base.OnDestroy();
            transformPropertyData.OnPositionChanged.RemoveListener(UpdatePosition);
            transformPropertyData.OnRotationChanged.RemoveListener(UpdateRotation);
            transformPropertyData.OnScaleChanged.RemoveListener(UpdateScale);
        }

        private void UpdatePosition(Coordinate newPosition)
        {
            worldTransform.MoveToCoordinate(newPosition);
        }

        private void UpdateRotation(Vector3 newAngles)
        {
            worldTransform.SetRotation(Quaternion.Euler(newAngles));
        }

        private void UpdateScale(Vector3 newScale)
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
        
        protected virtual void Update()
        {
            //Position and rotation changes are handled by the WorldTransform, but should be updated in the project data
            //todo: add a == and != operator to Coordinate.cs to avoid having to do this
            if(worldTransform.Coordinate.value1 != previousCoordinate.value1 ||
               worldTransform.Coordinate.value2 != previousCoordinate.value2 ||
               worldTransform.Coordinate.value3 != previousCoordinate.value3)
            {
                transformPropertyData.Position = worldTransform.Coordinate;
                previousCoordinate = worldTransform.Coordinate;
            }
            
            if (worldTransform.Rotation != previousRotation)
            {
                transformPropertyData.EulerRotation = worldTransform.Rotation.eulerAngles;
                previousRotation = worldTransform.Rotation;
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

            if (!transformInterfaceToggle)
            {
                Debug.LogError("Transform handles interface toggles not found, cannot set transform target");
            }
            else
            {
                transformInterfaceToggle.SetTransformTarget(gameObject);
            }
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

        public override void ApplyStyling()
        {
            var features = GetFeatures<MeshRenderer>();
            foreach (var feature in features)
            {
                ApplyStyling(feature);
            }
        }

        /// <summary>
        /// Finds the styling for this feature and applies it.
        ///
        /// It is expected that the features for a HierarchicalObjectLayerGameObject are meshRenderers, if they are not
        /// we do not know how to style that and we ignore that feature.
        /// </summary>
        private void ApplyStyling(LayerFeature feature)
        {
            if (feature.Geometry is not MeshRenderer meshRenderer) return;

            var symbolizer = GetStyling(feature);
            var fillColor = symbolizer.GetFillColor();

            // Keep the original material color if fill color is not set (null)
            if (!fillColor.HasValue) return;

            LayerData.Color = fillColor.Value;
            meshRenderer.SetUrpLitColorOptimized(fillColor.Value);
        }

        /// <summary>
        /// Will add additional attributes to a newly created feature.
        ///
        /// For this class, we only have a few:
        ///
        /// * "materials" (array of strings) - only provided when the feature contains a meshrenderer
        /// </summary>
        protected override LayerFeature AddAttributesToLayerFeature(LayerFeature feature)
        {
            if (feature.Geometry is not MeshRenderer meshRenderer) return feature;

            feature.Attributes.Add("materials", meshRenderer.materials.Select(material => material.name));

            return feature;
        }
    }
}