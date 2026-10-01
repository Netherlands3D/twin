using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Netherlands3D.Coordinates;
using Netherlands3D.Functionalities.Wms;
using Netherlands3D.Twin.FloatingOrigin;
using Netherlands3D.Twin.Layers;
using Netherlands3D.Twin.Layers.Properties;
using Netherlands3D.Twin.Projects;
using Netherlands3D.Twin.Utility;
using UnityEngine;

namespace Netherlands3D.Functionalities.GeoTiff
{
    [RequireComponent(typeof(WorldTransform))]
    public class GeoTiffLayerGameObject : LayerGameObject, IVisualizationWithPropertyData
    {
        [SerializeField] private TextureProjectorBase projectorPrefab;
        [SerializeField] private int maxTextureDimension = 4096;
        [SerializeField] private float projectionHeight = 1000f;
        [SerializeField] private float projectionDepth = 1100f;
        [SerializeField] private int renderPriority = -1;

        private GeoTiffPropertyData propertyData;
        private WorldTransform worldTransform;
        private TextureProjectorBase projector;
        private BoundingBox bounds;
        private Coroutine loadCoroutine;

        public override BoundingBox Bounds => bounds;

        protected override void OnVisualizationInitialize()
        {
            worldTransform = GetComponent<WorldTransform>();
        }

        protected override void OnVisualizationReady()
        {
            ScheduleReload(propertyData?.GeoTiffFile);
        }

        public void LoadProperties(List<LayerPropertyData> properties)
        {
            InitProperty<GeoTiffPropertyData>(properties);
            propertyData = properties.OfType<GeoTiffPropertyData>().FirstOrDefault();
        }

        protected override void RegisterEventListeners()
        {
            base.RegisterEventListeners();
            propertyData = LayerData.GetProperty<GeoTiffPropertyData>();
            propertyData?.GeoTiffUriChanged.AddListener(ScheduleReload);
        }

        protected override void UnregisterEventListeners()
        {
            propertyData?.GeoTiffUriChanged.RemoveListener(ScheduleReload);
            base.UnregisterEventListeners();
        }

        public override void OnLayerActiveInHierarchyChanged(bool activeInHierarchy)
        {
            if (projector)
                projector.gameObject.SetActive(activeInHierarchy);
        }

        private void ScheduleReload(Uri ignored)
        {
            if (loadCoroutine != null)
                StopCoroutine(loadCoroutine);
            loadCoroutine = StartCoroutine(LoadGeoTiff());
        }

        private IEnumerator LoadGeoTiff()
        {
            ClearProjection();
            if (propertyData?.GeoTiffFile == null)
                yield break;

            var localPath = AssetUriFactory.GetLocalPath(propertyData.GeoTiffFile);
            if (string.IsNullOrEmpty(localPath))
                localPath = propertyData.GeoTiffFile.LocalPath;
            if (string.IsNullOrEmpty(localPath) || !File.Exists(localPath))
            {
                ReportError($"GeoTIFF file could not be found: {propertyData.GeoTiffFile}");
                yield break;
            }

            GeoTiffMetadata metadata;
            try
            {
                metadata = GeoTiffMetadataReader.Read(localPath);
                ApplyPlacement(metadata);
            }
            catch (Exception exception)
            {
                ReportError($"Could not place GeoTIFF '{Path.GetFileName(localPath)}': {exception.Message}", exception);
                yield break;
            }

            Texture2D decodedTexture = null;
            Exception decodeError = null;
            yield return GeoTiffTextureDecoder.Decode(localPath, maxTextureDimension,
                texture => decodedTexture = texture,
                exception => decodeError = exception);

            if (decodeError != null)
            {
                ReportError($"Could not decode GeoTIFF '{Path.GetFileName(localPath)}': {decodeError.Message}", decodeError);
                yield break;
            }

            if (!projectorPrefab)
            {
                if (decodedTexture)
                    Destroy(decodedTexture);
                ReportError("The GeoTIFF layer has no projector prefab configured.");
                yield break;
            }

            projector = Instantiate(projectorPrefab, transform);
            projector.transform.localPosition = Vector3.zero;
            projector.transform.localRotation = Quaternion.identity;
            projector.transform.localScale = Vector3.one;
            projector.SetTexture(decodedTexture);
            projector.SetSize((float)(bounds.TopRight.easting - bounds.BottomLeft.easting),
                (float)(bounds.TopRight.northing - bounds.BottomLeft.northing), projectionDepth);
            if (projector is TextureDecalProjector decalProjector)
                decalProjector.SetPriority(renderPriority);
            projector.gameObject.SetActive(LayerData.ActiveInHierarchy);
            loadCoroutine = null;
        }

        private void ApplyPlacement(GeoTiffMetadata metadata)
        {
            var isAmersfoortGeographic = metadata.EpsgCode == 4289;
            var sourceSystem = CoordinateSystem.Undefined;
            if (!isAmersfoortGeographic &&
                !CoordinateSystems.FindCoordinateSystem(metadata.EpsgCode.ToString(), out sourceSystem))
                throw new NotSupportedException($"EPSG:{metadata.EpsgCode} is not supported by this NL3D build.");

            var corners = new[]
            {
                ToRd(metadata.EpsgCode, sourceSystem, metadata.MinX, metadata.MinY),
                ToRd(metadata.EpsgCode, sourceSystem, metadata.MinX, metadata.MaxY),
                ToRd(metadata.EpsgCode, sourceSystem, metadata.MaxX, metadata.MinY),
                ToRd(metadata.EpsgCode, sourceSystem, metadata.MaxX, metadata.MaxY)
            };
            var minX = corners.Min(c => c.easting);
            var minY = corners.Min(c => c.northing);
            var maxX = corners.Max(c => c.easting);
            var maxY = corners.Max(c => c.northing);
            bounds = new BoundingBox(new Coordinate(CoordinateSystem.RDNAP, minX, minY, 0),
                new Coordinate(CoordinateSystem.RDNAP, maxX, maxY, projectionHeight));

            var center = new Coordinate(CoordinateSystem.RDNAP, (minX + maxX) * 0.5, (minY + maxY) * 0.5,
                projectionHeight);
            worldTransform.MoveToCoordinate(center);
            worldTransform.SetRotation(Quaternion.identity);
        }

        private static Coordinate ToRd(int epsgCode, CoordinateSystem sourceSystem, double x, double y)
        {
            if (epsgCode == 4289)
            {
                AmersfoortCoordinateConverter.ToRd(x, y, out var easting, out var northing);
                return new Coordinate(CoordinateSystem.RD, easting, northing);
            }

            Coordinate source;
            if (sourceSystem == CoordinateSystem.WGS84_LatLon || sourceSystem == CoordinateSystem.ETRS89_LatLon)
                source = new Coordinate(sourceSystem, y, x);
            else
                source = new Coordinate(sourceSystem, x, y);
            return source.Convert(CoordinateSystem.RD);
        }

        private void ClearProjection()
        {
            bounds = null;
            if (projector)
            {
                Destroy(projector.gameObject);
                projector = null;
            }
        }

        private void ReportError(string message, Exception exception = null)
        {
            SendErrorMessage(message);
            if (exception == null)
                Debug.LogError(message, this);
            else
                Debug.LogException(new InvalidOperationException(message, exception), this);
            loadCoroutine = null;
        }

        private void OnDestroy()
        {
            if (loadCoroutine != null)
                StopCoroutine(loadCoroutine);
            ClearProjection();
        }
    }
}
