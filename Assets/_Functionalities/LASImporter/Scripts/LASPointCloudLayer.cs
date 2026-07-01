using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Netherlands3D.Coordinates;
using Netherlands3D.Functionalities.LASImporter.Parsing;
using Netherlands3D.Services;
using Netherlands3D.Twin.FloatingOrigin;
using Netherlands3D.Twin.Layers;
using Netherlands3D.Twin.Layers.LayerTypes.HierarchicalObject.Properties;
using Netherlands3D.Twin.Layers.Properties;
using Netherlands3D.Twin.Projects;
using Netherlands3D.Twin.UI;
using Netherlands3D.Twin.Utility;
using UnityEngine;
using UnityEngine.Rendering;

namespace Netherlands3D.Functionalities.LASImporter
{
    [RequireComponent(typeof(WorldTransform))]
    public class LASPointCloudLayer : LayerGameObject, IVisualizationWithPropertyData
    {
        [SerializeField] private int maxLoadedPoints = 2000000;
        [SerializeField] private float chunkSizeMeters = 75f;
        [SerializeField] private int maxPointsPerChunkMesh = 45000;
        [SerializeField] private int pointsPerFrameWhileLoading = 25000;
        [SerializeField] private float lodDistanceMultiplier = 3f;
        [SerializeField] private bool centerWhenHeaderLoaded = true;
        [SerializeField] private float pointSizePixels = 4f;
        [SerializeField] private float pointSizeReferenceDistance = 200f;
        [SerializeField] private float minPointSizePixels = 1f;
        [SerializeField] private float maxPointSizePixels = 8f;
        [SerializeField] private Material materialTemplate;

        private readonly List<PointCloudChunk> chunks = new();
        private readonly Dictionary<Vector2Int, PointCloudChunk> chunkMap = new();
        private readonly Dictionary<byte, int> classificationCounts = new();
        private LASPointCloudPropertyData propertyData;
        private LASPointCloudRenderPropertyData renderPropertyData;
        private LASClassificationColorPropertyData classificationColorPropertyData;
        private TransformLayerPropertyData transformPropertyData;
        private LASHeader header;
        private Material pointMaterial;
        private BoundingBox loadedBounds;
        private bool loading;
        private bool centerWhenLoaded;
        private float nextLodUpdateTime;
        private Coroutine loadingCoroutine;
        private Vector3 previousPosition;
        private Quaternion previousRotation;
        private Vector3 previousScale;
        private WorldTransform worldTransform;
        private Coordinate? georeferencedAnchor;

        private static readonly int PointSizeShaderProperty = Shader.PropertyToID("_PointSize");
        private static readonly int PointSizeReferenceDistanceShaderProperty = Shader.PropertyToID("_PointSizeReferenceDistance");
        private static readonly int MinPointSizeShaderProperty = Shader.PropertyToID("_MinPointSize");
        private static readonly int MaxPointSizeShaderProperty = Shader.PropertyToID("_MaxPointSize");

        public override BoundingBox Bounds => loadedBounds;
        public IReadOnlyDictionary<byte, int> ClassificationCounts => classificationCounts;

        protected override void OnVisualizationInitialize()
        {
            if (!TryGetComponent(out worldTransform))
                worldTransform = gameObject.AddComponent<WorldTransform>();
        }

        protected override void OnVisualizationReady()
        {
            pointMaterial = CreatePointMaterial();
            ApplyRenderPropertySettings();
            ApplyTransformProperty();
            CacheCurrentTransform();
            loadingCoroutine = StartCoroutine(LoadPointCloudProgressively());
        }

        private void Update()
        {
            UpdateTransformPropertyFromCurrentTransform();

            if (chunks.Count == 0 || Time.time < nextLodUpdateTime)
                return;

            nextLodUpdateTime = Time.time + 0.15f;
            UpdateChunkVisibilityAndLod();
        }

        public override void OnLayerActiveInHierarchyChanged(bool activeInHierarchy)
        {
            foreach (var chunk in chunks)
            {
                if (chunk.GameObject)
                    chunk.GameObject.SetActive(activeInHierarchy && chunk.IsVisible);
            }
        }

        protected override void OnDoubleClick(LayerData layer)
        {
            if (Bounds != null)
            {
                base.OnDoubleClick(layer);
                return;
            }

            if (loading)
            {
                centerWhenLoaded = true;
                Debug.Log("LAS point cloud is still loading. It will center when the point cloud bounds are available.", this);
                return;
            }

            if (TryRecalculateBoundsFromChunks())
            {
                base.OnDoubleClick(layer);
                return;
            }

            Debug.LogWarning("LAS point cloud has no bounds. The file may not have loaded any renderable points.", this);
        }

        public void LoadProperties(List<LayerPropertyData> properties)
        {
            InitProperty<LASPointCloudPropertyData>(properties);
            InitProperty<LASPointCloudRenderPropertyData>(properties, null,
                LASPointColorMode.FileColors,
                pointSizePixels,
                pointSizeReferenceDistance,
                minPointSizePixels,
                maxPointSizePixels,
                maxLoadedPoints,
                maxPointsPerChunkMesh,
                lodDistanceMultiplier
            );
            InitProperty<LASClassificationColorPropertyData>(properties);
            InitProperty<TransformLayerPropertyData>(properties, null, new Coordinate(transform.position),
                transform.eulerAngles, transform.localScale, "%");

            propertyData = properties.OfType<LASPointCloudPropertyData>().FirstOrDefault();
            renderPropertyData = properties.OfType<LASPointCloudRenderPropertyData>().FirstOrDefault();
            classificationColorPropertyData = properties.OfType<LASClassificationColorPropertyData>().FirstOrDefault();
            transformPropertyData = properties.OfType<TransformLayerPropertyData>().FirstOrDefault();
            if (transformPropertyData != null)
                transformPropertyData.IsEditable = false;
        }

        protected override void RegisterEventListeners()
        {
            base.RegisterEventListeners();

            transformPropertyData = LayerData.GetProperty<TransformLayerPropertyData>();
            if (transformPropertyData != null)
            {
                transformPropertyData.OnPositionChanged.AddListener(UpdatePosition);
                transformPropertyData.OnRotationChanged.AddListener(UpdateRotation);
                transformPropertyData.OnScaleChanged.AddListener(UpdateScale);
            }

            renderPropertyData = LayerData.GetProperty<LASPointCloudRenderPropertyData>();
            if (renderPropertyData != null)
                renderPropertyData.RenderSettingsChanged.AddListener(ApplyRenderSettingsChanged);

            classificationColorPropertyData = LayerData.GetProperty<LASClassificationColorPropertyData>();
            if (classificationColorPropertyData != null)
                classificationColorPropertyData.OnStylingChanged.AddListener(ApplyClassificationStylingChanged);
        }

        protected override void UnregisterEventListeners()
        {
            if (transformPropertyData != null)
            {
                transformPropertyData.OnPositionChanged.RemoveListener(UpdatePosition);
                transformPropertyData.OnRotationChanged.RemoveListener(UpdateRotation);
                transformPropertyData.OnScaleChanged.RemoveListener(UpdateScale);
            }

            if (renderPropertyData != null)
                renderPropertyData.RenderSettingsChanged.RemoveListener(ApplyRenderSettingsChanged);

            if (classificationColorPropertyData != null)
                classificationColorPropertyData.OnStylingChanged.RemoveListener(ApplyClassificationStylingChanged);

            base.UnregisterEventListeners();
        }

        private IEnumerator LoadPointCloudProgressively()
        {
            if (propertyData?.LasFile == null)
                yield break;

            var localPath = AssetUriFactory.GetLocalPath(propertyData.LasFile);
            if (string.IsNullOrEmpty(localPath))
                localPath = propertyData.LasFile.LocalPath;

            if (string.IsNullOrEmpty(localPath) || !File.Exists(localPath))
            {
                Debug.LogError($"LAS point cloud file could not be found: {propertyData.LasFile}", this);
                yield break;
            }

            loading = true;
            LASFileReader reader = null;

            try
            {
                reader = new LASFileReader(localPath);
                header = reader.Header;

                ClearChunks();
                classificationCounts.Clear();
                ApplyPlacementFromHeader(header);
                SetTransformEditingAvailability();
                CenterAfterHeaderLoaded();

                var pointCoordinateSystem = header.HasCoordinateSystem
                    ? CoordinateSystems.To3D(header.CoordinateSystem)
                    : CoordinateSystem.Undefined;

                var anchor = georeferencedAnchor;
                var centerX = (header.MinX + header.MaxX) * 0.5;
                var centerY = (header.MinY + header.MaxY) * 0.5;
                var centerZ = (header.MinZ + header.MaxZ) * 0.5;
                var maxPointsToLoad = Math.Max(1, GetMaxLoadedPoints());
                int stride = header.PointCount > (ulong)maxPointsToLoad
                    ? Mathf.CeilToInt(header.PointCount / (float)maxPointsToLoad)
                    : 1;

                int pointsThisFrame = 0;
                for (ulong pointIndex = 0; pointIndex < header.PointCount; pointIndex += (ulong)stride)
                {
                    if (!reader.TryReadPoint(pointIndex, out var point))
                        continue;

                    var fileColor = point.HasColor ? point.Color : LASClassificationColors.ForClassification(point.Classification);
                    var unityPosition = header.HasCoordinateSystem
                        ? CoordinateDeltaToLocalUnity(
                            new Coordinate(pointCoordinateSystem, point.X, point.Y, point.Z),
                            anchor.Value
                        )
                        : new Vector3((float)(point.X - centerX), (float)(point.Z - centerZ), (float)(point.Y - centerY));

                    AddPointToChunk(new RenderPoint(unityPosition, fileColor, point.HasColor, point.Classification));
                    AddClassification(point.Classification);

                    pointsThisFrame++;
                    if (pointsThisFrame >= pointsPerFrameWhileLoading)
                    {
                        pointsThisFrame = 0;
                        UpdateChunkVisibilityAndLod();
                        yield return null;
                    }
                }

                UpdateChunkVisibilityAndLod(force: true);
                SyncClassificationPropertyData();
            }
            finally
            {
                reader?.Dispose();
                loading = false;
                loadingCoroutine = null;
            }
        }

        private void ApplyPlacementFromHeader(LASHeader lasHeader)
        {
            if (lasHeader.HasCoordinateSystem)
            {
                var boundsCoordinateSystem = CoordinateSystems.To3D(lasHeader.CoordinateSystem);
                georeferencedAnchor = new Coordinate(
                    boundsCoordinateSystem,
                    (lasHeader.MinX + lasHeader.MaxX) * 0.5,
                    (lasHeader.MinY + lasHeader.MaxY) * 0.5,
                    (lasHeader.MinZ + lasHeader.MaxZ) * 0.5
                );

                worldTransform.MoveToCoordinate(georeferencedAnchor.Value);
                worldTransform.SetRotation(Quaternion.identity);
                transform.localScale = Vector3.one;
                CacheCurrentTransform();
                loadedBounds = new BoundingBox(
                    new Coordinate(boundsCoordinateSystem, lasHeader.MinX, lasHeader.MinY, lasHeader.MinZ),
                    new Coordinate(boundsCoordinateSystem, lasHeader.MaxX, lasHeader.MaxY, lasHeader.MaxZ)
                );
                return;
            }

            georeferencedAnchor = null;
            var centerX = (lasHeader.MinX + lasHeader.MaxX) * 0.5;
            var centerY = (lasHeader.MinY + lasHeader.MaxY) * 0.5;
            var centerZ = (lasHeader.MinZ + lasHeader.MaxZ) * 0.5;
            var localMin = new Vector3(
                (float)(lasHeader.MinX - centerX),
                (float)(lasHeader.MinZ - centerZ),
                (float)(lasHeader.MinY - centerY)
            );
            var localMax = new Vector3(
                (float)(lasHeader.MaxX - centerX),
                (float)(lasHeader.MaxZ - centerZ),
                (float)(lasHeader.MaxY - centerY)
            );

            var worldBounds = CreateWorldBoundsFromLocalExtents(localMin, localMax);
            loadedBounds = new BoundingBox(worldBounds);
        }

        private Bounds CreateWorldBoundsFromLocalExtents(Vector3 localMin, Vector3 localMax)
        {
            var worldBounds = new Bounds(transform.TransformPoint(localMin), Vector3.zero);
            worldBounds.Encapsulate(transform.TransformPoint(new Vector3(localMax.x, localMin.y, localMin.z)));
            worldBounds.Encapsulate(transform.TransformPoint(new Vector3(localMin.x, localMax.y, localMin.z)));
            worldBounds.Encapsulate(transform.TransformPoint(new Vector3(localMin.x, localMin.y, localMax.z)));
            worldBounds.Encapsulate(transform.TransformPoint(new Vector3(localMax.x, localMax.y, localMin.z)));
            worldBounds.Encapsulate(transform.TransformPoint(new Vector3(localMax.x, localMin.y, localMax.z)));
            worldBounds.Encapsulate(transform.TransformPoint(new Vector3(localMin.x, localMax.y, localMax.z)));
            worldBounds.Encapsulate(transform.TransformPoint(localMax));
            return worldBounds;
        }

        private static Vector3 CoordinateDeltaToLocalUnity(Coordinate coordinate, Coordinate anchor)
        {
            var connectedCoordinate = coordinate.Convert(CoordinateSystems.connectedCoordinateSystem);
            var connectedAnchor = anchor.Convert(CoordinateSystems.connectedCoordinateSystem);
            var difference = connectedCoordinate - connectedAnchor;
            var relativePosition = new Vector3(
                (float)difference.value1,
                (float)difference.value2,
                (float)difference.value3
            );

            if (CoordinateSystems.getCoordinateSystemType(CoordinateSystems.connectedCoordinateSystem) ==
                CoordinateSystemType.Geocentric)
            {
                return new Vector3(-relativePosition.x, relativePosition.z, -relativePosition.y);
            }

            return new Vector3(relativePosition.x, relativePosition.z, relativePosition.y);
        }

        private void SetTransformEditingAvailability()
        {
            if (transformPropertyData == null)
                return;

            transformPropertyData.IsEditable = !header.HasCoordinateSystem;
            if (!header.HasCoordinateSystem)
            {
                transformPropertyData.Position = new Coordinate(transform.position);
                transformPropertyData.EulerRotation = transform.eulerAngles;
                transformPropertyData.LocalScale = transform.localScale;
            }
        }

        private bool CanEditTransform()
        {
            return transformPropertyData != null && header != null && !header.HasCoordinateSystem;
        }

        private void ApplyTransformProperty()
        {
            if (transformPropertyData == null)
                return;

            transform.position = transformPropertyData.UnityPosition;
            transform.rotation = transformPropertyData.Rotation;
            transform.localScale = transformPropertyData.LocalScale;
        }

        private void UpdatePosition(Coordinate newPosition)
        {
            if (!CanEditTransform())
                return;

            transform.position = newPosition.ToUnity();
            CacheCurrentTransform();
            RecalculateLocalBoundsFromHeader();
        }

        private void UpdateRotation(Vector3 newAngles)
        {
            if (!CanEditTransform())
                return;

            transform.rotation = Quaternion.Euler(newAngles);
            CacheCurrentTransform();
            RecalculateLocalBoundsFromHeader();
        }

        private void UpdateScale(Vector3 newScale)
        {
            if (!CanEditTransform() || newScale == transform.localScale)
                return;

            transform.localScale = newScale;
            CacheCurrentTransform();
            RecalculateLocalBoundsFromHeader();
        }

        private void UpdateTransformPropertyFromCurrentTransform()
        {
            if (!CanEditTransform())
                return;

            if (transform.position != previousPosition)
            {
                transformPropertyData.Position = new Coordinate(transform.position);
                previousPosition = transform.position;
            }

            if (transform.rotation != previousRotation)
            {
                transformPropertyData.EulerRotation = transform.eulerAngles;
                previousRotation = transform.rotation;
            }

            if (transform.localScale != previousScale)
            {
                transformPropertyData.LocalScale = transform.localScale;
                previousScale = transform.localScale;
            }
        }

        private void CacheCurrentTransform()
        {
            previousPosition = transform.position;
            previousRotation = transform.rotation;
            previousScale = transform.localScale;
        }

        private void RecalculateLocalBoundsFromHeader()
        {
            if (header == null || header.HasCoordinateSystem)
                return;

            ApplyPlacementFromHeader(header);
        }

        private bool TryRecalculateBoundsFromChunks()
        {
            if (chunks.Count == 0)
                return false;

            Bounds bounds = chunks[0].WorldBounds;
            for (int i = 1; i < chunks.Count; i++)
            {
                bounds.Encapsulate(chunks[i].WorldBounds);
            }

            loadedBounds = new BoundingBox(bounds);
            return true;
        }

        private void CenterIfRequested()
        {
            if (!centerWhenLoaded)
                return;

            centerWhenLoaded = false;
            if (Bounds != null)
                CenterInView();
        }

        private void CenterAfterHeaderLoaded()
        {
            if (Bounds == null)
                return;

            var centered = false;
            if (centerWhenHeaderLoaded)
            {
                CenterInView();
                centered = true;
            }

            if (!centered)
                CenterIfRequested();
            else
                centerWhenLoaded = false;
        }

        private void AddPointToChunk(RenderPoint point)
        {
            var key = new Vector2Int(
                Mathf.FloorToInt(point.Position.x / chunkSizeMeters),
                Mathf.FloorToInt(point.Position.z / chunkSizeMeters)
            );

            if (!chunkMap.TryGetValue(key, out var chunk))
            {
                chunk = new PointCloudChunk(key);
                chunk.CreateGameObject(transform, pointMaterial);
                chunkMap.Add(key, chunk);
                chunks.Add(chunk);
            }

            chunk.AddPoint(point);
        }

        private void AddClassification(byte classification)
        {
            classificationCounts.TryGetValue(classification, out var count);
            classificationCounts[classification] = count + 1;
        }

        private void SyncClassificationPropertyData()
        {
            if (classificationColorPropertyData == null)
                return;

            foreach (var (classification, count) in classificationCounts)
            {
                classificationColorPropertyData.EnsureClassification(
                    classification,
                    count,
                    LASClassificationColors.ForClassification(classification)
                );
            }
        }

        private void UpdateChunkVisibilityAndLod(bool force = false)
        {
            var camera = Camera.main;
            if (!camera)
                return;

            var planes = GeometryUtility.CalculateFrustumPlanes(camera);
            foreach (var chunk in chunks)
            {
                var worldBounds = chunk.WorldBounds;
                var visible = LayerData.ActiveInHierarchy && GeometryUtility.TestPlanesAABB(planes, worldBounds);
                chunk.IsVisible = visible;
                chunk.GameObject.SetActive(visible);

                if (!visible)
                    continue;

                int stride = CalculateLodStride(camera, worldBounds, chunk);
                if (force || chunk.IsDirty || stride != chunk.CurrentStride)
                    chunk.RebuildMesh(stride, GetMaxPointsPerChunkMesh(), GetRenderColor);
            }
        }

        private int CalculateLodStride(Camera camera, Bounds worldBounds, PointCloudChunk chunk)
        {
            var maxRenderablePoints = chunk.GetMaxRenderablePointCount(GetMaxPointsPerChunkMesh());
            var baseStride = Mathf.Max(1, Mathf.CeilToInt(chunk.Points.Count / (float)Math.Max(1, maxRenderablePoints)));
            var radius = Mathf.Max(1f, worldBounds.extents.magnitude);
            var distance = camera.orthographic
                ? camera.orthographicSize
                : Vector3.Distance(camera.transform.position, worldBounds.ClosestPoint(camera.transform.position));

            var detail = Mathf.Max(1f, distance / (radius * Mathf.Max(0.01f, GetLodDistanceMultiplier())));
            return Mathf.Max(baseStride, Mathf.NextPowerOfTwo(Mathf.CeilToInt(detail)));
        }

        private Color32 GetRenderColor(RenderPoint point)
        {
            if (renderPropertyData == null)
                return point.FileColor;

            return renderPropertyData.ColorMode switch
            {
                LASPointColorMode.Classification => GetClassificationColor(point.Classification),
                LASPointColorMode.SingleColor => LayerData.Color,
                _ => point.HasFileColor ? point.FileColor : GetClassificationColor(point.Classification)
            };
        }

        private Color32 GetClassificationColor(byte classification)
        {
            var color = classificationColorPropertyData?.GetColorByClassification(classification)
                        ?? LASClassificationColors.ForClassification(classification);
            return color;
        }

        private int GetMaxLoadedPoints()
        {
            return renderPropertyData?.MaxLoadedPoints ?? maxLoadedPoints;
        }

        private int GetMaxPointsPerChunkMesh()
        {
            return renderPropertyData?.MaxPointsPerChunkMesh ?? maxPointsPerChunkMesh;
        }

        private float GetLodDistanceMultiplier()
        {
            return renderPropertyData?.LodDistanceMultiplier ?? lodDistanceMultiplier;
        }

        private void ApplyRenderPropertySettings()
        {
            if (renderPropertyData == null)
                return;

            pointSizePixels = renderPropertyData.PointSizePixels;
            pointSizeReferenceDistance = renderPropertyData.PointSizeReferenceDistance;
            minPointSizePixels = renderPropertyData.MinPointSizePixels;
            maxPointSizePixels = renderPropertyData.MaxPointSizePixels;
            maxLoadedPoints = renderPropertyData.MaxLoadedPoints;
            maxPointsPerChunkMesh = renderPropertyData.MaxPointsPerChunkMesh;
            lodDistanceMultiplier = renderPropertyData.LodDistanceMultiplier;
            if (pointMaterial)
                ApplyPointSizeSettings(pointMaterial);
        }

        private void ApplyRenderSettingsChanged()
        {
            ApplyRenderPropertySettings();
            MarkChunksDirty();
            UpdateChunkVisibilityAndLod(force: true);
        }

        private void ApplyClassificationStylingChanged()
        {
            MarkChunksDirty();
            UpdateChunkVisibilityAndLod(force: true);
        }

        private void MarkChunksDirty()
        {
            foreach (var chunk in chunks)
                chunk.MarkDirty();
        }

        private Material CreatePointMaterial()
        {
            if (materialTemplate)
            {
                var material = new Material(materialTemplate);
                ValidatePointCloudShader(material);
                ApplyPointSizeSettings(material);
                return material;
            }

            var shader = Shader.Find("Netherlands3D/PointCloudVertexColor");
            if (shader)
            {
                var material = new Material(shader);
                ValidatePointCloudShader(material);
                ApplyPointSizeSettings(material);
                return material;
            }

            Debug.LogError("LAS point cloud shader 'Netherlands3D/PointCloudVertexColor' could not be found. Falling back to URP Unlit; point colors and circular billboards may not render correctly.", this);
            return new Material(Shader.Find("Universal Render Pipeline/Unlit"));
        }

        private void ValidatePointCloudShader(Material material)
        {
            if (!material.shader || material.shader.isSupported)
                return;

            Debug.LogError($"LAS point cloud shader '{material.shader.name}' is not supported on this platform. The point cloud may not render.", this);
        }

        private void ApplyPointSizeSettings(Material material)
        {
            material.SetFloat(PointSizeShaderProperty, Mathf.Max(1f, pointSizePixels));
            material.SetFloat(PointSizeReferenceDistanceShaderProperty, Mathf.Max(1f, pointSizeReferenceDistance));
            material.SetFloat(MinPointSizeShaderProperty, Mathf.Max(0.1f, minPointSizePixels));
            material.SetFloat(MaxPointSizeShaderProperty, Mathf.Max(minPointSizePixels, maxPointSizePixels));
        }

        private void ClearChunks()
        {
            foreach (var chunk in chunks)
            {
                chunk.Destroy();
            }
            chunks.Clear();
            chunkMap.Clear();
        }

        public override void DestroyLayerGameObject()
        {
            if (loadingCoroutine != null)
            {
                StopCoroutine(loadingCoroutine);
                loadingCoroutine = null;
            }
            loading = false;

            ClearChunks();
            if (pointMaterial)
                Destroy(pointMaterial);
            base.DestroyLayerGameObject();
        }

        public override void OnSelect(LayerData layer)
        {
            if (!CanEditTransform())
                return;

            var transformInterfaceToggle = ServiceLocator.GetService<TransformHandleInterfaceToggle>();
            if (transformInterfaceToggle)
                transformInterfaceToggle.SetTransformTarget(gameObject);
        }

        public override void OnDeselect(LayerData layer)
        {
            var transformInterfaceToggle = ServiceLocator.GetService<TransformHandleInterfaceToggle>();
            if (transformInterfaceToggle)
                transformInterfaceToggle.ClearTransformTarget();
        }

        private readonly struct RenderPoint
        {
            public readonly Vector3 Position;
            public readonly Color32 FileColor;
            public readonly bool HasFileColor;
            public readonly byte Classification;

            public RenderPoint(Vector3 position, Color32 fileColor, bool hasFileColor, byte classification)
            {
                Position = position;
                FileColor = fileColor;
                HasFileColor = hasFileColor;
                Classification = classification;
            }
        }

        private sealed class PointCloudChunk
        {
            private const int MaxPointsFor16BitIndexBuffer = 16000;

            public Vector2Int Key { get; }
            public List<RenderPoint> Points { get; } = new();
            public GameObject GameObject { get; private set; }
            public Bounds LocalBounds { get; private set; }
            public Bounds WorldBounds => TransformBounds(GameObject.transform, LocalBounds);
            public int CurrentStride { get; private set; } = -1;
            public bool IsVisible { get; set; }
            public bool IsDirty { get; private set; } = true;

            private Mesh mesh;
            private readonly List<Vector3> vertices = new();
            private readonly List<Color32> colors = new();
            private readonly List<Vector2> corners = new();
            private readonly List<int> indices = new();

            public PointCloudChunk(Vector2Int key)
            {
                Key = key;
            }

            public void CreateGameObject(Transform parent, Material material)
            {
                GameObject = new GameObject($"LAS Chunk {Key.x},{Key.y}");
                GameObject.transform.SetParent(parent, false);

                mesh = new Mesh
                {
                    name = GameObject.name,
#if UNITY_WEBGL
                    indexFormat = IndexFormat.UInt16
#else
                    indexFormat = SystemInfo.supports32bitsIndexBuffer ? IndexFormat.UInt32 : IndexFormat.UInt16
#endif
                };
                mesh.MarkDynamic();

                var meshFilter = GameObject.AddComponent<MeshFilter>();
                meshFilter.sharedMesh = mesh;

                var meshRenderer = GameObject.AddComponent<MeshRenderer>();
                meshRenderer.sharedMaterial = material;
                meshRenderer.shadowCastingMode = ShadowCastingMode.Off;
                meshRenderer.receiveShadows = false;
            }

            public void AddPoint(RenderPoint point)
            {
                Points.Add(point);
                if (Points.Count == 1)
                    LocalBounds = new Bounds(point.Position, Vector3.zero);
                else
                    LocalBounds.Encapsulate(point.Position);

                IsDirty = true;
            }

            public int GetMaxRenderablePointCount(int requestedMaxPoints)
            {
                return mesh != null && mesh.indexFormat == IndexFormat.UInt32
                    ? requestedMaxPoints
                    : Mathf.Min(requestedMaxPoints, MaxPointsFor16BitIndexBuffer);
            }

            public void RebuildMesh(int stride, int maxPoints, Func<RenderPoint, Color32> colorProvider)
            {
                CurrentStride = Mathf.Max(1, stride);
                var pointLimit = GetMaxRenderablePointCount(maxPoints);

                vertices.Clear();
                colors.Clear();
                corners.Clear();
                indices.Clear();

                for (int i = 0; i < Points.Count && vertices.Count / 4 < pointLimit; i += CurrentStride)
                {
                    var point = Points[i];
                    AddBillboardPoint(point, colorProvider(point));
                }

                mesh.Clear();
                mesh.SetVertices(vertices);
                mesh.SetColors(colors);
                mesh.SetUVs(0, corners);
                mesh.SetIndices(indices, MeshTopology.Triangles, 0);
                var meshBounds = LocalBounds;
                meshBounds.Expand(2f);
                mesh.bounds = meshBounds;
                IsDirty = false;
            }

            public void MarkDirty()
            {
                IsDirty = true;
            }

            private void AddBillboardPoint(RenderPoint point, Color32 color)
            {
                var startIndex = vertices.Count;
                AddBillboardVertex(point, color, -1f, -1f);
                AddBillboardVertex(point, color, -1f, 1f);
                AddBillboardVertex(point, color, 1f, 1f);
                AddBillboardVertex(point, color, 1f, -1f);

                indices.Add(startIndex);
                indices.Add(startIndex + 1);
                indices.Add(startIndex + 2);
                indices.Add(startIndex);
                indices.Add(startIndex + 2);
                indices.Add(startIndex + 3);
            }

            private void AddBillboardVertex(RenderPoint point, Color32 color, float cornerX, float cornerY)
            {
                vertices.Add(point.Position);
                colors.Add(color);
                corners.Add(new Vector2(cornerX, cornerY));
            }

            public void Destroy()
            {
                if (mesh)
                    UnityEngine.Object.Destroy(mesh);

                if (GameObject)
                    UnityEngine.Object.Destroy(GameObject);
            }

            private static Bounds TransformBounds(Transform transform, Bounds bounds)
            {
                var center = transform.TransformPoint(bounds.center);
                var extents = bounds.extents;

                var axisX = transform.TransformVector(extents.x, 0, 0);
                var axisY = transform.TransformVector(0, extents.y, 0);
                var axisZ = transform.TransformVector(0, 0, extents.z);

                extents.x = Mathf.Abs(axisX.x) + Mathf.Abs(axisY.x) + Mathf.Abs(axisZ.x);
                extents.y = Mathf.Abs(axisX.y) + Mathf.Abs(axisY.y) + Mathf.Abs(axisZ.y);
                extents.z = Mathf.Abs(axisX.z) + Mathf.Abs(axisY.z) + Mathf.Abs(axisZ.z);

                return new Bounds(center, extents * 2f);
            }
        }
    }
}
