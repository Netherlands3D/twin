using System;
using System.Collections.Generic;
using Netherlands3D.Coordinates;
using Netherlands3D.Minimap;
using Netherlands3D.Services;
using Netherlands3D.Twin.Cameras;
using Netherlands3D.UI.Components;
using Netherlands3D.UI.ExtensionMethods;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UIElements;

namespace Netherlands3D.UI.Panels
{
    [UxmlElement]
    public partial class WMTSPanel : VisualElement
    {
        [Tooltip("The start indexLayer of the map")]
        private int layerStartIndex = 6;

        private VisualElement tileContainer;
        private MinimapConfig minimapConfig;
        private Vector2RD topRight;
        private Vector2RD bottomLeft;

        private bool moveCameraToClickedLocation = true;
        private Camera cameraMoveTarget;

        private int layerIndex;
        private float tileSize = 256;
        private float baseTileSize;
        private float startMeterInPixels;
        private double tileSizeInMeters;
        private double divide;
        private double pixelInMeters = 0.00028;
        private double scaleDenominator = 12288000;
        private double mapSizeInMeters;
        private Vector2 boundsInMeters;
        private Vector2 tileOffset;
        private Vector2 layerTilesOffset;
        private Vector2RD minimapTopLeft = new Vector2RD(-285401.92, 903401.92);

        private readonly Dictionary<int, Dictionary<Vector2, WMTSTile>> tileLayers = new();
        private bool initialized;

        private Vector2 currentCenterLocalPosition;
        public UnityEvent TilesChanged = new();

        public WMTSPanel()
        {
            this.CloneComponentTree("Panels");
            this.AddComponentStylesheet("Panels");

            pickingMode = PickingMode.Ignore; // MapViewport is the fixed viewport that handles all pointer input, this element moves and scales, so we don't handle input here.
            tileContainer = this.Q<VisualElement>("TileContainer");
        }

        public void Initialize(MinimapConfig config, Vector2RD bottomLeft, Vector2RD topRight, int layerStartIndex = 6)
        {
            this.minimapConfig = config;
            this.bottomLeft = bottomLeft;
            this.topRight = topRight;
            this.layerStartIndex = layerStartIndex;

            layerIndex = layerStartIndex;

            // Use config values
            tileSize = minimapConfig.TileMatrixSet.TileSize;
            pixelInMeters = minimapConfig.TileMatrixSet.PixelInMeters;
            scaleDenominator = minimapConfig.TileMatrixSet.ScaleDenominator;

            // Coverage of our application bounds
            boundsInMeters.x = (float)topRight.x - (float)bottomLeft.x;
            boundsInMeters.y = (float)topRight.y - (float)bottomLeft.y;

            baseTileSize = tileSize;

            // Calculate map width in meters based on zoomlevel 0 setting values
            mapSizeInMeters = baseTileSize * pixelInMeters * scaleDenominator;

            DetermineTopLeftOrigin();
            CalculateGridScaling();
            ActivateMapLayer();

            // Calculate base meters in pixels to do calculations converting local coordinates to meters
            startMeterInPixels = (float)tileSizeInMeters / baseTileSize;

            var cameraService = ServiceLocator.GetService<CameraService>();
            cameraMoveTarget = cameraService.ActiveCamera;
            cameraService.OnSwitchCamera.AddListener(SetCamera);

            parent?.RegisterCallback<GeometryChangedEvent>(OnViewportGeometryChanged);
            RegisterCallback<DetachFromPanelEvent>(OnDetachFromPanel);
            cameraService.OnPositionChanged.AddListener(OnCameraPositionChanged);

            Clamp();
            UpdateLayerTiles();

            initialized = true;
        }

        private void OnCameraPositionChanged(Vector3 newWorldPosition)
        {
            MoveToPosition(newWorldPosition);
        }

        private void OnViewportGeometryChanged(GeometryChangedEvent evt)
        {
            CenterOnLocalPosition(currentCenterLocalPosition);
        }

        private void OnDetachFromPanel(DetachFromPanelEvent evt)
        {
            var cameraService = ServiceLocator.GetService<CameraService>();
            cameraService.OnSwitchCamera.RemoveListener(SetCamera);
            parent?.UnregisterCallback<GeometryChangedEvent>(OnViewportGeometryChanged);
        }

        private void SetCamera(Camera camera)
        {
            cameraMoveTarget = camera;
        }

        private void Clamp()
        {
            if (parent == null) return;

            var viewportSize = new Vector2(parent.resolvedStyle.width, parent.resolvedStyle.height);

            var maxPositionXInUnits = -(boundsInMeters.x / startMeterInPixels) * transform.scale.x;
            var maxPositionYInUnits = -(boundsInMeters.y / startMeterInPixels) * transform.scale.x;

            var xPadding = viewportSize.x * 0.5f;
            var yPadding = viewportSize.y * 0.5f;

            var position = transform.position;

            position.x = Mathf.Clamp(position.x, maxPositionXInUnits + viewportSize.x - xPadding, xPadding);
            position.y = Mathf.Clamp(position.y, maxPositionYInUnits + viewportSize.y - yPadding, yPadding);

            transform.position = position;
        }

        public void ClickedMap(Vector2 panelPosition)
        {
            if (!initialized || cameraMoveTarget == null) return;

            //WorldToLocal accounts for this element's own pan/zoom transform,
            //equivalent to the old transform.InverseTransformPoint.
            Vector2 localClickPosition = this.WorldToLocal(panelPosition);

            var meterX = localClickPosition.x * startMeterInPixels;
            // Y-FLIP: local Y grows downward here, so this no longer needs negating
            // relative to the old RectTransform-based version.
            var meterY = -localClickPosition.y * startMeterInPixels;

            var rdCoordinate = new Coordinate(
                CoordinateSystem.RDNAP,
                bottomLeft.x + meterX,
                (float)topRight.y + meterY,
                0.0d
            );

            if (!rdCoordinate.IsValid()) return;

            Vector3 unityCoordinate = rdCoordinate.ToUnity();
            unityCoordinate.y = cameraMoveTarget.transform.position.y;

            if (moveCameraToClickedLocation)
            {
                cameraMoveTarget.transform.position = unityCoordinate;
            }
        }

        public Vector2 DeterminePositionOnMap(Coordinate sourceRDPosition)
        {
            var meterX = sourceRDPosition.easting - (float)bottomLeft.x;
            var meterY = sourceRDPosition.northing - (float)topRight.y;

            var pixelX = meterX / startMeterInPixels;
            var pixelY = -(meterY / startMeterInPixels);

            return new Vector2((float)pixelX, (float)pixelY);
        }

        public Vector2 LocalToViewport(Vector2 localPosition)
        {
            return (Vector2)transform.position + localPosition * transform.scale.x;
        }
        
        public void Zoom(int viewerZoom)
        {
            if (!initialized) return;

            tileSize = baseTileSize / Mathf.Pow(2, viewerZoom);
            layerIndex = layerStartIndex + viewerZoom;

            CalculateGridScaling();
            ActivateMapLayer();

            Clamp();
            UpdateLayerTiles();
            
            currentCenterLocalPosition = CalculateCurrentCenterLocalPosition();
        }

        public void Pan(Vector2 delta)
        {
            if (!initialized) return;

            transform.position += (Vector3)delta;
            Clamp();
            UpdateLayerTiles();

            currentCenterLocalPosition = CalculateCurrentCenterLocalPosition();
        }

        private void ActivateMapLayer()
        {
            RemoveOtherLayers();

            if (!tileLayers.ContainsKey(layerIndex))
            {
                tileLayers.Add(layerIndex, new Dictionary<Vector2, WMTSTile>());
            }
        }

        private void DetermineTopLeftOrigin()
        {
            switch (minimapConfig.TileMatrixSet.minimapOriginAlignment)
            {
                case TileMatrixSet.OriginAlignment.BottomLeft:
                    minimapTopLeft.x = minimapConfig.TileMatrixSet.Origin.x;
                    minimapTopLeft.y = minimapConfig.TileMatrixSet.Origin.y + mapSizeInMeters;
                    break;
                default:
                    minimapTopLeft.x = minimapConfig.TileMatrixSet.Origin.x;
                    minimapTopLeft.y = minimapConfig.TileMatrixSet.Origin.y;
                    break;
            }
        }

        private void CalculateGridScaling()
        {
            divide = Mathf.Pow(2, layerIndex);
            tileSizeInMeters = mapSizeInMeters / divide;

            //The tile 0,0 its top left does not align with our region top left. So here we determine the offset.
            double offsetXd = (bottomLeft.x - minimapTopLeft.x) / tileSizeInMeters;
            double offsetYd = (minimapTopLeft.y - topRight.y) / tileSizeInMeters;


            //Based on tile numbering type
            double tileOffsetXd = Math.Floor(offsetXd);
            double tileOffsetYd = Math.Floor(offsetYd);

            tileOffset = new Vector2((float)tileOffsetXd, (float)tileOffsetYd);
            // Fractional remainder is always in [0,1) - small magnitude, safe to store as float.
            layerTilesOffset = new Vector2((float)(offsetXd - tileOffsetXd), (float)(offsetYd - tileOffsetYd));
        }

        public void ScaleMapOverOrigin(Vector2 origin, Vector3 newScale)
        {
            var origin3 = (Vector3)origin;
            var currentPosition = transform.position;
            var newOrigin = currentPosition - origin3;
            var relativeScale = newScale.x / transform.scale.x;
            var finalPosition = origin3 + newOrigin * relativeScale;

            transform.scale = newScale;
            transform.position = finalPosition;
            
            currentCenterLocalPosition = CalculateCurrentCenterLocalPosition();
        }

        private Vector2 CalculateCurrentCenterLocalPosition()
        {
            var viewportSize = new Vector2(parent.resolvedStyle.width, parent.resolvedStyle.height);
            return (viewportSize * 0.5f - (Vector2)transform.position) / transform.scale.x;
        }
        
        private void RemoveOtherLayers()
        {
            var mapTileKeys = new List<int>(tileLayers.Keys);
            foreach (int layerKey in mapTileKeys)
            {
                if ((layerKey < layerIndex - 1 && layerKey != layerStartIndex) || layerKey > layerIndex)
                {
                    foreach (var tile in tileLayers[layerKey])
                    {
                        tile.Value.Dispose();
                    }

                    tileLayers.Remove(layerKey);
                }
            }
        }

        private void UpdateLayerTiles()
        {
            if (parent == null || !tileLayers.TryGetValue(layerIndex, out var tileList))
                return;

            var viewportSize = new Vector2(parent.resolvedStyle.width, parent.resolvedStyle.height);
            var localPosition = transform.position;
            var localScale = transform.scale;
            
            float tileStepX = tileSize * localScale.x;
            float tileStepY = tileSize * localScale.y;

            int startX = Mathf.Max(0, Mathf.FloorToInt(-localPosition.x / tileStepX));
            int startY = Mathf.Max(0, Mathf.FloorToInt(-localPosition.y / tileStepY));
            int endX = Mathf.CeilToInt((viewportSize.x - localPosition.x) / tileStepX);
            int endY = Mathf.CeilToInt((viewportSize.y - localPosition.y) / tileStepY);

            var tilesToRemove = new List<Vector2>(tileList.Keys);

            for (int x = startX; x <= endX; x++)
            {
                for (int y = startY; y <= endY; y++)
                {
                    int tileCol;
                    int tileRow;

                    switch (minimapConfig.TileMatrixSet.minimapOriginAlignment)
                    {
                        case TileMatrixSet.OriginAlignment.BottomLeft:
                            tileCol = x + (int)tileOffset.x;
                            tileRow = (int)(divide - 1) - (y + (int)tileOffset.y);
                            break;

                        case TileMatrixSet.OriginAlignment.TopLeft:
                        default:
                            tileCol = x + (int)tileOffset.x;
                            tileRow = y + (int)tileOffset.y;
                            break;
                    }

                    Vector2 tileKey = new Vector2(tileCol, tileRow);
                    
                    float tileXPosition = (x - layerTilesOffset.x) * tileSize;
                    float tileYPosition = (y - layerTilesOffset.y) * tileSize;

                    if (!tileList.TryGetValue(tileKey, out _))
                    {
                        var mapTile = new WMTSTile();
                        mapTile.Initialize(tileContainer, layerIndex, tileSize, tileXPosition, tileYPosition, tileKey, minimapConfig);
                        tileList.Add(tileKey, mapTile);
                    }

                    tilesToRemove.Remove(tileKey);
                }
            }

            foreach (var tileKey in tilesToRemove)
            {
                tileList[tileKey].Dispose();
                tileList.Remove(tileKey);
            }

            TilesChanged.Invoke();
        }
        
        private void MoveToPosition(Vector3 newWorldPosition)
        {
            if (!initialized) return;
            
            var rdCoordinate = new Coordinate(newWorldPosition).Convert(CoordinateSystem.RDNAP);
            Vector2 mapPosition = DeterminePositionOnMap(rdCoordinate);
            CenterOnLocalPosition(mapPosition);
        }

        private void CenterOnLocalPosition(Vector2 localPosition)
        {
            if (parent == null) return;
            
            currentCenterLocalPosition = localPosition;
            
            var viewportSize = new Vector2(parent.resolvedStyle.width, parent.resolvedStyle.height);
            transform.position = -(Vector3)(localPosition * transform.scale.x) + (Vector3)(viewportSize * 0.5f);

            Clamp();
            UpdateLayerTiles();
        }
    }
}