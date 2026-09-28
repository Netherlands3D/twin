using System.Collections.Generic;
using GeoJSON.Net.Feature;
using Netherlands3D.Coordinates;
using Netherlands3D.Functionalities.ObjectInformation;
using Netherlands3D.SelectionTools;
using Netherlands3D.Services;
using Netherlands3D.Twin.Cameras;
using Netherlands3D.Twin.FloatingOrigin;
using Netherlands3D.UI_Toolkit;
using Netherlands3D.UI_Toolkit.Scripts;
using Netherlands3D.UI.Components;
using Netherlands3D.UI.Panels;
using UnityEngine;
using UnityEngine.UIElements;

namespace Netherlands3D.Twin.Layers.LayerTypes.GeoJsonLayers
{
    public class FeatureAnnotationVisualisations : IFeatureVisualisation<List<Coordinate>>
    {
        public struct Annotation
        {
            public string Title { get; set; }
            public string AnnotationText { get; set; }
            public string ImageUrl { get; set; }
            public string ImageCaption { get; set; }
            public string Color { get; set; }
        }
        
        private Annotation annotation;
        public Feature Feature { get; set; }
        public List<List<Coordinate>> Data => pointCollection;

        private List<List<Coordinate>> pointCollection = new();
        public Bounds tiledBounds;
        public Bounds trueBounds;
        private Vector3 boundsPadding;

        private float boundsRoundingCeiling = 1000;
        public float BoundsRoundingCeiling { get => boundsRoundingCeiling; set => boundsRoundingCeiling = value; }
        
        private CameraService cameraService;
        private WorldUIService worldUIService;
        private AppRootBehaviour appRootBehaviour;
        private SelectionService selectionService;
        
        private WorldAnnotation worldTextElement; 
        private FloatingElement floatingElement;
        private const float MaxPixelDistanceOffset = 100;
        private const float worldSpaceOffset = 10;
        private bool isVisible = false;

        public FeatureAnnotationVisualisations(Annotation annotation)
        {
            this.annotation = annotation;
            worldUIService  = ServiceLocator.GetService<WorldUIService>();
            cameraService = App.Cameras;
            appRootBehaviour = App.UIRoot;
            selectionService = App.Selection;
            Origin.current.onPostShift.AddListener(OnOriginShifted);
            
            InitializeWorldUI();
        }
        
        private void InitializeWorldUI()
        {
            floatingElement = new FloatingElement();
            worldUIService.AddToFloatingElementsContent(floatingElement);
            worldTextElement = new WorldAnnotation(annotation.Title);
            worldTextElement.SetSnappingSide(WorldAnnotation.SnappingSide.Above);
            worldTextElement.SetReadOnly(true);
            worldTextElement.SetImage(annotation.ImageUrl);
            if (!string.IsNullOrEmpty(annotation.Color) && HexColorUtility.ParseHexColor(annotation.Color, out var parsedColor))
                worldTextElement.SetColor(parsedColor);
            
            floatingElement.Add(worldTextElement);
            
            worldTextElement.RegisterCallback<PointerDownEvent>(OnClickAnnotation);

            SetVisible(true);
        }
        
        
        private void OnClickAnnotation(PointerDownEvent e)
        {
            selectionService.SelectGeoJsonFeatureAtPosition(trueBounds.center);
        }
        
        public void CalculateBounds()
        {
            // Create combined rounded bounds of all lines
            foreach (var pointCollection in pointCollection)
            {
                for (var i = 0; i < pointCollection.Count; i++)
                {
                    var coordinate = pointCollection[i];
                    if (i == 0)
                        trueBounds = new Bounds(coordinate.ToUnity(), Vector3.zero);
                    else
                        trueBounds.Encapsulate(coordinate.ToUnity());
                }
            }
            trueBounds.Expand(boundsPadding);
            tiledBounds = new Bounds(trueBounds.center, trueBounds.size);
            
            // Expand bounds to ceiling to steps
            tiledBounds.size = new Vector3(
                Mathf.Ceil(tiledBounds.size.x / BoundsRoundingCeiling) * BoundsRoundingCeiling,
                Mathf.Ceil(tiledBounds.size.y / BoundsRoundingCeiling) * BoundsRoundingCeiling,
                Mathf.Ceil(tiledBounds.size.z / BoundsRoundingCeiling) * BoundsRoundingCeiling
            );
            tiledBounds.center = new Vector3(
                Mathf.Round(tiledBounds.center.x / BoundsRoundingCeiling) * BoundsRoundingCeiling,
                Mathf.Round(tiledBounds.center.y / BoundsRoundingCeiling) * BoundsRoundingCeiling,
                Mathf.Round(tiledBounds.center.z / BoundsRoundingCeiling) * BoundsRoundingCeiling
            );
        }

        private void OnOriginShifted(Coordinate from, Coordinate to)
        {
            CalculateBounds();
        }

        public void SetBoundsPadding(Vector3 padding)
        {
            boundsPadding = padding; 
        }
        
        public void SetVisible(bool visible)
        {
            if(this.isVisible == visible)
                return;
            
            this.isVisible = visible;
            worldTextElement.EnableInClassList(UtilityClassConstants.HIDDEN, !visible);
        }
        
        public void Update()
        {
            var screenPos =  cameraService.ActiveCamera.WorldToScreenPoint(trueBounds.center);
            Vector2 panelPos = appRootBehaviour.GetUIPositionFromScreenPosition(screenPos);
            var localPos = worldUIService.FloatingElementsContent.WorldToLocal(panelPos);
            floatingElement.SetPosition(localPos);

            var offsetScreenPos = cameraService.ActiveCamera.WorldToScreenPoint(trueBounds.center + cameraService.ActiveCamera.transform.right * worldSpaceOffset);
            float dist = Mathf.Abs(offsetScreenPos.x - screenPos.x);
            float t = Mathf.InverseLerp(1500f, 0f, cameraService.ActiveCamera.transform.position.y);
            float pixelOffset = Mathf.Min(dist * t, MaxPixelDistanceOffset);
            worldTextElement.SetLabelOffset(pixelOffset);
        }
        
        public void Dispose()
        {
            Origin.current.onPostShift.RemoveListener(OnOriginShifted);
            worldUIService.RemoveFromFloatingElementsContent(floatingElement);
            floatingElement = null;
        }
    }
}