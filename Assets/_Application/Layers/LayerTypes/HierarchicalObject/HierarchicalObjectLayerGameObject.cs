using System;
using System.Collections.Generic;
using Netherlands3D.Coordinates;
using Netherlands3D.LayerStyles;
using Netherlands3D.Services;
using Netherlands3D.Twin.FloatingOrigin;
using Netherlands3D.Twin.Layers.ExtensionMethods;
using Netherlands3D.Twin.Layers.LayerTypes.HierarchicalObject.Properties;
using Netherlands3D.Twin.Layers.LayerTypes.Polygons;
using Netherlands3D.Twin.Layers.LayerTypes.Polygons.Properties;
using Netherlands3D.Twin.Layers.Properties;
using Netherlands3D.Twin.Samplers;
using Netherlands3D.Twin.UI;
using Netherlands3D.Twin.Utility;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.EventSystems;

namespace Netherlands3D.Twin.Layers.LayerTypes.HierarchicalObject
{
    [RequireComponent(typeof(WorldTransform))]
    public class HierarchicalObjectLayerGameObject : LayerGameObject, IPointerClickHandler, IVisualizationWithPropertyData //, ILayerWithPropertyPanels
    {
        public override BoundingBox Bounds => CalculateWorldBoundsFromRenderers();
        public bool DebugBoundingBox = false;

        private int snappingCullingMask = 0;

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

        [SerializeField] private UnityEvent<GameObject> objectCreated = new();

        private Coordinate previousCoordinate;
        private Quaternion previousRotation;
        private Vector3 previousScale;
        public WorldTransform WorldTransform { get; private set; }

        [SerializeField] private string scaleUnitCharacter = "%";
        
        protected override void OnLayerInitialize()
        {
            snappingCullingMask = (1 << LayerMask.NameToLayer("Terrain")) | (1 << LayerMask.NameToLayer("Buildings"));
            WorldTransform = GetComponent<WorldTransform>();
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
            if (newScale != transform.localScale)
                transform.localScale = newScale;
        }

        public void SnapToGround()
        {
            Vector3 currentPosition = transform.position;
            BoundingBox bounds = Bounds;
            Vector3 boundsCenter = bounds.Center.ToUnity();

            var context = new SnapContext
            {
                HeightExtent = bounds.Size.ToUnity().y * 0.5f,
                PivotOffset = boundsCenter.y - currentPosition.y,
                PreviousPosition = currentPosition,
                Target = this,
                Raycaster = ServiceLocator.GetService<OpticalRaycaster>(),
                cullingMask = snappingCullingMask
            };

            context.SnapFromPosition(new Vector3(currentPosition.x, boundsCenter.y, currentPosition.z));
        }

        private struct SnapContext
        {
            public float HeightExtent;
            public float PivotOffset;
            public Vector3 PreviousPosition;
            public OpticalRaycaster Raycaster;
            public HierarchicalObjectLayerGameObject Target;
            public int cullingMask;

            public void SnapFromPosition(Vector3 position)
            {
                Raycaster.GetWorldPointFromDirectionAsync(
                    position,
                    Vector3.down,
                    OnRaycastDown,
                    cullingMask
                );
            }

            private void OnRaycastDown(Vector3 worldPos, bool hit)
            {
                if (hit)
                {
                    Coordinate target = new Coordinate(worldPos + Vector3.up * (HeightExtent - PivotOffset));
                    Target.UpdatePosition(target);
                }
                else
                {
                    Raycaster.GetWorldPointFromDirectionAsync(
                        PreviousPosition,
                        Vector3.up,
                        OnRaycastUp,
                        cullingMask
                    );
                }
            }

            private void OnRaycastUp(Vector3 worldPos, bool hit)
            {
                if (hit)
                {
                    Coordinate target = new Coordinate(worldPos + Vector3.up * (-HeightExtent - PivotOffset));
                    Target.UpdatePosition(target);
                }
                else
                {
                    Coordinate target = new Coordinate(PreviousPosition);
                    HeightMap heightMap = ServiceLocator.GetService<HeightMap>();
                    float height = heightMap.GetHeight(target);
                    target.height = height;
                    Target.UpdatePosition(target);
                }
            }
        }

        public virtual void LoadProperties(List<LayerPropertyData> properties)
        {
            var transformPropertyData = properties.Get<TransformLayerPropertyData>();
            if (transformPropertyData == null)
            {
                transformPropertyData = new TransformLayerPropertyData(new Coordinate(transform.position),
                        transform.eulerAngles,
                        transform.localScale,
                        scaleUnitCharacter);
                LayerData.SetProperty(transformPropertyData);
            }

            var toggleScatterPropertyData = properties.Get<ToggleScatterPropertyData>();
            if (toggleScatterPropertyData == null)
            {
                toggleScatterPropertyData = new ToggleScatterPropertyData() { AllowScatter = true };
                LayerData.SetProperty(toggleScatterPropertyData);
            }

            var stylingPropertyData = properties.Get<StylingPropertyData>();
            if (stylingPropertyData == null)
            {
                stylingPropertyData = new StylingPropertyData();
                LayerData.SetProperty(stylingPropertyData);
            }

            SetTransformPropertyData(transformPropertyData);
                        
            toggleScatterPropertyData.AllowScatter = LayerData.ParentLayer.HasProperty<PolygonSelectionLayerPropertyData>();
        }

        private void SetTransformPropertyData(TransformLayerPropertyData transformProperty)
        {
            UpdatePosition(transformProperty.Position);
            UpdateRotation(transformProperty.EulerRotation);
            UpdateScale(transformProperty.LocalScale);
        }

        protected override void RegisterEventListeners()
        {
            base.RegisterEventListeners();
            var transformPropertyData = LayerData.GetProperty<TransformLayerPropertyData>();
            transformPropertyData.OnPositionChanged.AddListener(UpdatePosition);
            transformPropertyData.OnRotationChanged.AddListener(UpdateRotation);
            transformPropertyData.OnScaleChanged.AddListener(UpdateScale);

            var toggleScatterPropertyData = LayerData.GetProperty<ToggleScatterPropertyData>();
            toggleScatterPropertyData.IsScatteredChanged.AddListener(ConvertToScatterLayer);
        }

        protected override void UnregisterEventListeners()
        {
            base.UnregisterEventListeners();
            var transformPropertyData = LayerData.GetProperty<TransformLayerPropertyData>();
            transformPropertyData?.OnPositionChanged.RemoveListener(UpdatePosition);
            transformPropertyData?.OnRotationChanged.RemoveListener(UpdateRotation);
            transformPropertyData?.OnScaleChanged.RemoveListener(UpdateScale);

            var toggleScatterPropertyData = LayerData.GetProperty<ToggleScatterPropertyData>();
            toggleScatterPropertyData.IsScatteredChanged.RemoveListener(ConvertToScatterLayer);
        }

        protected virtual void Update()
        {
            //Position and rotation changes are handled by the WorldTransform, but should be updated in the project data
            //todo: add a == and != operator to Coordinate.cs to avoid having to do this
            if (Math.Abs(WorldTransform.Coordinate.value1 - previousCoordinate.value1) > 0.0001d ||
                Math.Abs(WorldTransform.Coordinate.value2 - previousCoordinate.value2) > 0.0001d ||
                Math.Abs(WorldTransform.Coordinate.value3 - previousCoordinate.value3) > 0.0001d)
            {
                LayerData.GetProperty<TransformLayerPropertyData>().Position = WorldTransform.Coordinate;
                previousCoordinate = WorldTransform.Coordinate;
            }

            if (WorldTransform.Rotation != previousRotation)
            {
                LayerData.GetProperty<TransformLayerPropertyData>().EulerRotation = WorldTransform.Rotation.eulerAngles;
                previousRotation = WorldTransform.Rotation;
            }

            // Check for scale change
            if (transform.localScale != previousScale)
            {
                LayerData.GetProperty<TransformLayerPropertyData>().LocalScale = transform.localScale;
                previousScale = transform.localScale;
            }

            //enable this to debug the exact bounds in worldspace, based on the Bounds (3d)
            if (DebugBoundingBox)
                Bounds.Debug(Color.magenta);
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
            var transformInterfaceToggle = ServiceLocator.GetService<TransformHandleInterfaceToggle>();

            if (!transformInterfaceToggle)
            {
                Debug.LogError("Transform handles interface toggles not found, cannot set transform target");
            }
            else
            {
                transformInterfaceToggle.SetTransformTarget(gameObject);
                transformInterfaceToggle.SnapTarget.AddListener(SnapToGround);
            }
        }

        public override void OnDeselect()
        {
            ClearTransformHandles();
        }

        protected void ClearTransformHandles()
        {
            var transformInterfaceToggle = ServiceLocator.GetService<TransformHandleInterfaceToggle>();

            if (transformInterfaceToggle)
            {
                transformInterfaceToggle.ClearTransformTarget();
                transformInterfaceToggle.SnapTarget.RemoveListener(SnapToGround);
            }
        }

        public override void OnLayerDataParentChanged()
        {
            LayerData.LayerProperties.Get<ToggleScatterPropertyData>().AllowScatter = LayerData.ParentLayer.HasProperty<PolygonSelectionLayerPropertyData>();
        }

        public override void ApplyStyling()
        {
            // Dynamically create a list of Layer features because a different set of renderers could be present after
            // an import or replacement.
            var features = CreateFeaturesByType<MeshRenderer>();

           
            // Apply style to the features that was discovered
            foreach (var feature in features)
            {
                if (feature.Geometry is not MeshRenderer meshRenderer) return;

                Symbolizer styling = GetStyling(feature);
                var fillColor = styling.GetFillColor();

                // Keep the original material color if fill color is not set (null)
                if (!fillColor.HasValue) return;

                LayerData.Color = fillColor.Value;
                var block = new MaterialPropertyBlock();
                for (int m = 0; m <= meshRenderer.sharedMaterials.Length - 1; m++)
                {
                    meshRenderer.GetPropertyBlock(block, m);
                    block.SetColor(BaseColorID, fillColor.Value);
                    meshRenderer.SetPropertyBlock(block, m);
                }
            }

            base.ApplyStyling();
        }

        private static readonly int BaseColorID = Shader.PropertyToID("_BaseColor");
      

        private void ConvertToScatterLayer(bool isScattered)
        {
            if (!isScattered)
                return;
            
            var existingScatterPropertyData = LayerData.GetProperty<ScatterGenerationSettingsPropertyData>();
            if (existingScatterPropertyData == null)
            {
                existingScatterPropertyData = new ScatterGenerationSettingsPropertyData(LayerData.PrefabIdentifier);
                LayerData.SetProperty(existingScatterPropertyData);
            }
            
            LayerData.DeselectLayer(); //remove any transform interaction that might be present

            App.Layers.VisualizeAs(LayerData, ObjectScatterLayerGameObject.ScatterBasePrefabID);
        }
    }
}