using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Stopwatch = System.Diagnostics.Stopwatch;
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
        private const int MaxPointLoadFrameMilliseconds = 12;
        private const int ClassificationSyncInterval = 50000;
        private const int ProfilingLogPointInterval = 250000;
        private const int EstimatedRenderPointBytes = 20;
        private const int MeshVertexBytes = 24;
        private const int BrowserSourceChunkBytes = 4 * 1024 * 1024;

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

        private const int CloseChunkPointBudgetMultiplier = 4;
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
        private Coroutine reloadCoroutine;
        private Vector3 previousPosition;
        private Quaternion previousRotation;
        private Vector3 previousScale;
        private WorldTransform worldTransform;
        private Coordinate? georeferencedAnchor;
        private bool syncingClassificationCounts;
        private int meshRebuildCount;

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
            {
                renderPropertyData.RenderSettingsChanged.AddListener(ApplyRenderSettingsChanged);
                renderPropertyData.PointBudgetChanged.AddListener(SchedulePointCloudReload);
            }

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
            {
                renderPropertyData.RenderSettingsChanged.RemoveListener(ApplyRenderSettingsChanged);
                renderPropertyData.PointBudgetChanged.RemoveListener(SchedulePointCloudReload);
            }

            if (classificationColorPropertyData != null)
                classificationColorPropertyData.OnStylingChanged.RemoveListener(ApplyClassificationStylingChanged);

            base.UnregisterEventListeners();
        }

        private IEnumerator LoadPointCloudProgressively(bool centerAfterHeaderLoaded = true)
        {
            if (propertyData?.LasFile == null)
                yield break;

            if (propertyData.LasFile.Scheme.Equals("browser-file", StringComparison.OrdinalIgnoreCase))
            {
                yield return LoadBrowserPointCloudProgressively(centerAfterHeaderLoaded);
                yield break;
            }

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
                SyncRenderPropertyData(header);
                SetTransformEditingAvailability();
                if (centerAfterHeaderLoaded)
                    CenterAfterHeaderLoaded();
                yield return null;

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
                var totalLoadTimer = Stopwatch.StartNew();
                var loadedPointCount = 0;
                var nextProfilingLogPoint = ProfilingLogPointInterval;
                meshRebuildCount = 0;
                LogLoadStarted(localPath, reader.FileLength, header, stride, maxPointsToLoad, "FileStream seek", 0);

                int pointsThisFrame = 0;
                int pointsSinceClassificationSync = 0;
                var loadFrameTimer = Stopwatch.StartNew();
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
                    loadedPointCount++;
                    pointsSinceClassificationSync++;

                    if (loadedPointCount >= nextProfilingLogPoint)
                    {
                        LogLoadProgress(loadedPointCount, reader.FileLength, totalLoadTimer.ElapsedMilliseconds);
                        nextProfilingLogPoint += ProfilingLogPointInterval;
                    }

                    pointsThisFrame++;
                    if (pointsThisFrame >= pointsPerFrameWhileLoading ||
                        loadFrameTimer.ElapsedMilliseconds >= MaxPointLoadFrameMilliseconds)
                    {
                        if (pointsSinceClassificationSync >= ClassificationSyncInterval)
                        {
                            SyncClassificationPropertyData();
                            pointsSinceClassificationSync = 0;
                        }
                        pointsThisFrame = 0;
                        UpdateChunkVisibilityAndLod();
                        loadFrameTimer.Restart();
                        yield return null;
                    }
                }

                UpdateChunkVisibilityAndLod(force: true);
                SyncClassificationPropertyData();
                LogLoadCompleted(loadedPointCount, reader.FileLength, totalLoadTimer.ElapsedMilliseconds);
            }
            finally
            {
                SyncClassificationPropertyData();
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
            var key = GetChunkKey(point.Position, chunkSizeMeters);

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

            syncingClassificationCounts = true;
            try
            {
                classificationColorPropertyData.SetClassifications(classificationCounts);
            }
            finally
            {
                syncingClassificationCounts = false;
            }
        }

        private IEnumerator LoadBrowserPointCloudProgressively(bool centerAfterHeaderLoaded)
        {
            loading = true;
            LASStreamingReader reader = null;
            var loadedPointCount = 0;
            var totalLoadTimer = Stopwatch.StartNew();

            try
            {
                ILASByteSource byteSource;
                try
                {
                    byteSource = new WebGLBrowserFileLASByteSource(propertyData.LasFile);
                    reader = new LASStreamingReader(byteSource);
                }
                catch (Exception exception)
                {
                    Debug.LogError($"Could not open streamed LAS source: {exception.Message}", this);
                    yield break;
                }

                yield return reader.Initialize();
                if (reader.Error != null)
                {
                    Debug.LogError($"Could not read streamed LAS header: {reader.Error.Message}", this);
                    yield break;
                }

                header = reader.Header;
                ClearChunks();
                classificationCounts.Clear();
                ApplyPlacementFromHeader(header);
                SyncRenderPropertyData(header);
                SetTransformEditingAvailability();
                if (centerAfterHeaderLoaded)
                    CenterAfterHeaderLoaded();
                yield return null;

                var pointCoordinateSystem = header.HasCoordinateSystem
                    ? CoordinateSystems.To3D(header.CoordinateSystem)
                    : CoordinateSystem.Undefined;
                var anchor = georeferencedAnchor;
                var centerX = (header.MinX + header.MaxX) * 0.5;
                var centerY = (header.MinY + header.MaxY) * 0.5;
                var centerZ = (header.MinZ + header.MaxZ) * 0.5;
                var maxPointsToLoad = Math.Max(1, GetMaxLoadedPoints());
                var pointBudget = (ulong)maxPointsToLoad;
                var strideValue = header.PointCount > pointBudget
                    ? header.PointCount / pointBudget + (header.PointCount % pointBudget == 0 ? 0UL : 1UL)
                    : 1UL;
                var stride = strideValue > int.MaxValue ? int.MaxValue : (int)strideValue;
                var recordsPerChunk = Math.Max(1, BrowserSourceChunkBytes / header.PointDataRecordLength);
                var pointBuffer = new byte[recordsPerChunk * header.PointDataRecordLength];
                var nextProfilingLogPoint = ProfilingLogPointInterval;
                meshRebuildCount = 0;
                LogLoadStarted(
                    Uri.UnescapeDataString(Path.GetFileName(propertyData.LasFile.AbsolutePath)),
                    reader.FileLength,
                    header,
                    stride,
                    maxPointsToLoad,
                    "browser File.slice",
                    pointBuffer.Length
                );

                var pointsThisFrame = 0;
                var pointsSinceClassificationSync = 0;
                var loadFrameTimer = Stopwatch.StartNew();
                ulong pointIndex = 0;
                long sourceOffset = header.OffsetToPointData;

                while (pointIndex < header.PointCount)
                {
                    var remainingRecords = header.PointCount - pointIndex;
                    var recordsToRead = (int)Math.Min((ulong)recordsPerChunk, remainingRecords);
                    var bytesToRead = recordsToRead * header.PointDataRecordLength;
                    var request = reader.ReadPointBytes(sourceOffset, pointBuffer, bytesToRead);
                    while (!request.IsDone)
                        yield return null;

                    if (request.Error != null)
                    {
                        Debug.LogError($"Could not read streamed LAS point data: {request.Error.Message}", this);
                        yield break;
                    }

                    var completeRecordsRead = request.BytesRead / header.PointDataRecordLength;
                    if (completeRecordsRead <= 0)
                    {
                        Debug.LogWarning(
                            $"Streamed LAS ended before point {pointIndex:N0} of {header.PointCount:N0}.",
                            this
                        );
                        break;
                    }

                    for (var recordIndex = 0; recordIndex < completeRecordsRead; recordIndex++)
                    {
                        var currentPointIndex = pointIndex + (ulong)recordIndex;
                        if (currentPointIndex % (ulong)stride != 0)
                            continue;

                        var point = LASStreamingReader.ParsePoint(
                            pointBuffer,
                            recordIndex * header.PointDataRecordLength,
                            header
                        );
                        var fileColor = point.HasColor
                            ? point.Color
                            : LASClassificationColors.ForClassification(point.Classification);
                        var unityPosition = header.HasCoordinateSystem
                            ? CoordinateDeltaToLocalUnity(
                                new Coordinate(pointCoordinateSystem, point.X, point.Y, point.Z),
                                anchor.Value
                            )
                            : new Vector3(
                                (float)(point.X - centerX),
                                (float)(point.Z - centerZ),
                                (float)(point.Y - centerY)
                            );

                        AddPointToChunk(new RenderPoint(
                            unityPosition,
                            fileColor,
                            point.HasColor,
                            point.Classification
                        ));
                        AddClassification(point.Classification);
                        loadedPointCount++;
                        pointsSinceClassificationSync++;

                        if (loadedPointCount >= nextProfilingLogPoint)
                        {
                            LogLoadProgress(loadedPointCount, reader.FileLength, totalLoadTimer.ElapsedMilliseconds);
                            nextProfilingLogPoint += ProfilingLogPointInterval;
                        }

                        pointsThisFrame++;
                        if (pointsThisFrame >= pointsPerFrameWhileLoading
                            || loadFrameTimer.ElapsedMilliseconds >= MaxPointLoadFrameMilliseconds)
                        {
                            if (pointsSinceClassificationSync >= ClassificationSyncInterval)
                            {
                                SyncClassificationPropertyData();
                                pointsSinceClassificationSync = 0;
                            }

                            pointsThisFrame = 0;
                            UpdateChunkVisibilityAndLod();
                            loadFrameTimer.Restart();
                            yield return null;
                        }
                    }

                    pointIndex += (ulong)completeRecordsRead;
                    sourceOffset += (long)completeRecordsRead * header.PointDataRecordLength;
                    if (completeRecordsRead < recordsToRead)
                    {
                        Debug.LogWarning(
                            $"Streamed LAS returned a partial final block at point {pointIndex:N0}.",
                            this
                        );
                        break;
                    }
                }

                UpdateChunkVisibilityAndLod(force: true);
                SyncClassificationPropertyData();
                LogLoadCompleted(loadedPointCount, reader.FileLength, totalLoadTimer.ElapsedMilliseconds);
            }
            finally
            {
                SyncClassificationPropertyData();
                reader?.Dispose();
                loading = false;
                loadingCoroutine = null;
            }
        }

        private void SyncRenderPropertyData(LASHeader lasHeader)
        {
            if (renderPropertyData == null)
                return;

            renderPropertyData.SetPointBudgetLimit(lasHeader.PointCount);
        }

        private void UpdateChunkVisibilityAndLod(bool force = false)
        {
            var camera = Camera.main;
            if (!camera)
                return;

            var planes = GeometryUtility.CalculateFrustumPlanes(camera);
            var cameraLocalPosition = transform.InverseTransformPoint(camera.transform.position);
            var cameraChunkKey = GetChunkKey(cameraLocalPosition, chunkSizeMeters);
            foreach (var chunk in chunks)
            {
                var worldBounds = chunk.WorldBounds;
                var isCameraChunk = chunk.Key == cameraChunkKey;
                var visible = LayerData.ActiveInHierarchy
                              && (isCameraChunk || GeometryUtility.TestPlanesAABB(planes, worldBounds));
                chunk.IsVisible = visible;
                chunk.GameObject.SetActive(visible);

                if (!visible)
                {
                    chunk.SetCameraPriorityBounds(false, camera);
                    continue;
                }

                int maxPointsForChunk = CalculateMaxPointsForChunk(camera, worldBounds, chunk, isCameraChunk);
                int stride = CalculateLodStride(camera, worldBounds, chunk, maxPointsForChunk, isCameraChunk);
                if (force || chunk.IsDirty || stride != chunk.CurrentStride || maxPointsForChunk != chunk.CurrentMaxPoints)
                {
                    chunk.RebuildMesh(stride, maxPointsForChunk, GetRenderColor);
                    meshRebuildCount++;
                }

                // SetActive controls our own coarse culling, but Unity performs another frustum
                // test against Mesh.bounds. Include the camera and a point beyond its near plane
                // for the current cell so that its mesh is not clipped by that second test.
                chunk.SetCameraPriorityBounds(isCameraChunk, camera);
            }
        }

        private int CalculateMaxPointsForChunk(
            Camera camera,
            Bounds worldBounds,
            PointCloudChunk chunk,
            bool isCameraChunk
        )
        {
            var maxPoints = GetMaxPointsPerChunkMesh();
            var distanceFactor = CalculateDistanceFactor(camera, worldBounds);
            if (isCameraChunk || distanceFactor <= 1f)
                maxPoints = maxPoints > int.MaxValue / CloseChunkPointBudgetMultiplier
                    ? int.MaxValue
                    : maxPoints * CloseChunkPointBudgetMultiplier;

            return chunk.GetMaxRenderablePointCount(maxPoints);
        }

        private int CalculateLodStride(
            Camera camera,
            Bounds worldBounds,
            PointCloudChunk chunk,
            int maxRenderablePoints,
            bool isCameraChunk
        )
        {
            var baseStride = Mathf.Max(1, Mathf.CeilToInt(chunk.Points.Count / (float)Math.Max(1, maxRenderablePoints)));
            var detail = isCameraChunk ? 1f : Mathf.Max(1f, CalculateDistanceFactor(camera, worldBounds));
            return Mathf.Max(baseStride, Mathf.NextPowerOfTwo(Mathf.CeilToInt(detail)));
        }

        internal static Vector2Int GetChunkKey(Vector3 localPosition, float chunkSize)
        {
            var safeChunkSize = Mathf.Max(0.01f, chunkSize);
            return new Vector2Int(
                Mathf.FloorToInt(localPosition.x / safeChunkSize),
                Mathf.FloorToInt(localPosition.z / safeChunkSize)
            );
        }

        private float CalculateDistanceFactor(Camera camera, Bounds worldBounds)
        {
            var radius = Mathf.Max(1f, worldBounds.extents.magnitude);
            var distance = camera.orthographic
                ? camera.orthographicSize
                : Vector3.Distance(camera.transform.position, worldBounds.ClosestPoint(camera.transform.position));

            return distance / (radius * Mathf.Max(0.01f, GetLodDistanceMultiplier()));
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
            // SetClassifications also raises OnStylingChanged when only the displayed counts
            // changed. During loading that must refresh the panel, but it must not rebuild all
            // visible meshes every 50,000 decoded points.
            if (syncingClassificationCounts)
                return;

            MarkChunksDirty();
            UpdateChunkVisibilityAndLod(force: true);
        }

        private void SchedulePointCloudReload()
        {
            if (!isActiveAndEnabled || propertyData?.LasFile == null)
                return;

            if (reloadCoroutine != null)
                StopCoroutine(reloadCoroutine);

            reloadCoroutine = StartCoroutine(ReloadPointCloudAfterDelay());
        }

        private IEnumerator ReloadPointCloudAfterDelay()
        {
            yield return new WaitForSeconds(0.25f);
            reloadCoroutine = null;
            RestartPointCloudLoading();
        }

        private void RestartPointCloudLoading()
        {
            if (loadingCoroutine != null)
            {
                StopCoroutine(loadingCoroutine);
                loadingCoroutine = null;
            }

            loading = false;
            ClearChunks();
            classificationCounts.Clear();
            loadingCoroutine = StartCoroutine(LoadPointCloudProgressively(centerAfterHeaderLoaded: false));
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

        [System.Diagnostics.Conditional("UNITY_EDITOR")]
        [System.Diagnostics.Conditional("DEVELOPMENT_BUILD")]
        private void LogLoadStarted(
            string path,
            long fileBytes,
            LASHeader lasHeader,
            int stride,
            int pointBudget,
            string sourceMode,
            int sourceChunkBytes
        )
        {
            var sourceChunkDescription = sourceChunkBytes > 0
                ? $", sourceChunk={FormatBytes(sourceChunkBytes)}"
                : string.Empty;
            Debug.Log(
                $"[LAS memory] Start '{Path.GetFileName(path)}': file={FormatBytes(fileBytes)}, " +
                $"points={lasHeader.PointCount:N0}, record={lasHeader.PointDataRecordLength} B, " +
                $"format={lasHeader.PointDataFormat}, sampleStride={stride}, pointBudget={pointBudget:N0}, " +
                $"sourceMode={sourceMode}{sourceChunkDescription}, managedSourceBytesRetained=0 B, " +
                $"spatialCell={chunkSizeMeters:0.##} m.",
                this
            );
        }

        [System.Diagnostics.Conditional("UNITY_EDITOR")]
        [System.Diagnostics.Conditional("DEVELOPMENT_BUILD")]
        private void LogLoadProgress(int decodedPoints, long fileBytes, long elapsedMilliseconds)
        {
            GetEstimatedMemoryUsage(
                out var pointStorageBytes,
                out var stagingBytes,
                out var meshCpuBytes,
                out var gpuBytes,
                out var renderedPoints
            );

            Debug.Log(
                $"[LAS memory] Progress: decoded={decodedPoints:N0}, spatialChunks={chunks.Count:N0}, " +
                $"renderedMeshPoints={renderedPoints:N0}, pointStorageCapacity={FormatBytes(pointStorageBytes)}, " +
                $"meshStagingCapacity={FormatBytes(stagingBytes)}, meshCpuEstimate={FormatBytes(meshCpuBytes)}, " +
                $"gpuEstimate={FormatBytes(gpuBytes)}, sourceFile={FormatBytes(fileBytes)}, " +
                $"meshRebuilds={meshRebuildCount:N0}, elapsed={elapsedMilliseconds / 1000f:0.00}s.",
                this
            );
        }

        [System.Diagnostics.Conditional("UNITY_EDITOR")]
        [System.Diagnostics.Conditional("DEVELOPMENT_BUILD")]
        private void LogLoadCompleted(int decodedPoints, long fileBytes, long elapsedMilliseconds)
        {
            GetEstimatedMemoryUsage(
                out var pointStorageBytes,
                out var stagingBytes,
                out var meshCpuBytes,
                out var gpuBytes,
                out var renderedPoints
            );

            Debug.Log(
                $"[LAS memory] Complete: file={FormatBytes(fileBytes)}, decoded={decodedPoints:N0}, " +
                $"spatialChunks={chunks.Count:N0}, renderedMeshPoints={renderedPoints:N0}, " +
                $"pointStorageCapacity={FormatBytes(pointStorageBytes)}, meshStagingCapacity={FormatBytes(stagingBytes)}, " +
                $"meshCpuEstimate={FormatBytes(meshCpuBytes)}, gpuEstimate={FormatBytes(gpuBytes)}, " +
                $"meshRebuilds={meshRebuildCount:N0}, elapsed={elapsedMilliseconds / 1000f:0.00}s. " +
                "Browser FileReader/IndexedDB/MEMFS copies are outside these Unity-side estimates.",
                this
            );
        }

        private void GetEstimatedMemoryUsage(
            out long pointStorageBytes,
            out long stagingBytes,
            out long meshCpuBytes,
            out long gpuBytes,
            out long renderedPoints
        )
        {
            pointStorageBytes = 0;
            stagingBytes = 0;
            meshCpuBytes = 0;
            gpuBytes = 0;
            renderedPoints = 0;

            foreach (var chunk in chunks)
            {
                pointStorageBytes += chunk.EstimatedPointStorageCapacityBytes;
                stagingBytes += chunk.EstimatedStagingCapacityBytes;
                meshCpuBytes += chunk.EstimatedMeshBytes;
                gpuBytes += chunk.EstimatedMeshBytes;
                renderedPoints += chunk.RenderedPointCount;
            }
        }

        private static string FormatBytes(long bytes)
        {
            const double mebibyte = 1024d * 1024d;
            return bytes >= mebibyte
                ? $"{bytes / mebibyte:0.0} MiB"
                : $"{bytes / 1024d:0.0} KiB";
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
            if (reloadCoroutine != null)
            {
                StopCoroutine(reloadCoroutine);
                reloadCoroutine = null;
            }

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
            public int CurrentMaxPoints { get; private set; } = -1;
            public bool IsVisible { get; set; }
            public bool IsDirty { get; private set; } = true;
            public int RenderedPointCount => vertices.Count / 4;
            public long EstimatedPointStorageCapacityBytes => (long)Points.Capacity * EstimatedRenderPointBytes;
            public long EstimatedStagingCapacityBytes =>
                (long)vertices.Capacity * 12 +
                (long)colors.Capacity * 4 +
                (long)corners.Capacity * 8 +
                (long)indices.Capacity * 4;
            public long EstimatedMeshBytes => mesh == null || mesh.subMeshCount == 0
                ? 0
                : (long)mesh.vertexCount * MeshVertexBytes +
                  (long)mesh.GetIndexCount(0) * (mesh.indexFormat == IndexFormat.UInt16 ? 2 : 4);

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
                CurrentMaxPoints = maxPoints;
                var pointLimit = GetMaxRenderablePointCount(maxPoints);
                var targetPointCount = Mathf.Min(pointLimit, Mathf.CeilToInt(Points.Count / (float)CurrentStride));

                vertices.Clear();
                colors.Clear();
                corners.Clear();
                indices.Clear();

                for (int i = 0; i < Points.Count && vertices.Count / 4 < targetPointCount; i++)
                {
                    if (!ShouldRenderPoint(i, targetPointCount))
                        continue;

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

            public void SetCameraPriorityBounds(bool prioritized, Camera camera)
            {
                if (!mesh || !GameObject)
                    return;

                var renderBounds = LocalBounds;
                renderBounds.Expand(2f);
                if (prioritized && camera)
                {
                    var nearPlaneMargin = Mathf.Max(0.1f, camera.nearClipPlane + 0.1f);
                    renderBounds.Encapsulate(GameObject.transform.InverseTransformPoint(camera.transform.position));
                    renderBounds.Encapsulate(GameObject.transform.InverseTransformPoint(
                        camera.transform.position + camera.transform.forward * nearPlaneMargin
                    ));
                }

                mesh.bounds = renderBounds;
            }

            private bool ShouldRenderPoint(int pointIndex, int targetPointCount)
            {
                if (targetPointCount >= Points.Count)
                    return true;

                return StableHash01(pointIndex) <= targetPointCount / (float)Points.Count;
            }

            private static float StableHash01(int value)
            {
                unchecked
                {
                    uint hash = (uint)value;
                    hash ^= hash >> 16;
                    hash *= 0x7feb352d;
                    hash ^= hash >> 15;
                    hash *= 0x846ca68b;
                    hash ^= hash >> 16;
                    return hash / (float)uint.MaxValue;
                }
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
