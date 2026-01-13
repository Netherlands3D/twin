using System.Collections.Generic;
using System.Linq;
using Netherlands3D.CartesianTiles;
using Netherlands3D.Coordinates;
using Netherlands3D.LayerStyles;
using Netherlands3D.SelectionTools;
using Netherlands3D.Twin.ExtensionMethods;
using Netherlands3D.Twin.Layers.ExtensionMethods;
using Netherlands3D.Twin.Layers.LayerTypes.HierarchicalObject.Properties;
using Netherlands3D.Twin.Layers.LayerTypes.Polygons.Properties;
using Netherlands3D.Twin.Layers.Properties;
using Netherlands3D.Twin.Projects;
using Netherlands3D.Twin.Samplers;
using Netherlands3D.Twin.Utility;
using UnityEngine;

namespace Netherlands3D.Twin.Layers.LayerTypes.Polygons
{
    public enum FillType
    {
        Complete,
        Fill,
        Stroke,
    }

    public class ObjectScatterLayerGameObject : LayerGameObject, IVisualizationWithPropertyData
    {
        [SerializeField] private Material polygonMaterial;

        public override BoundingBox Bounds => new(polygonBounds);
        public const string ScatterBasePrefabID = "acb0d28ce2b674042ba63bf1d7789bfd"; //todo: not hardcode this
        private static readonly int baseColorID = Shader.PropertyToID("_BaseColor");

        private Mesh mesh;
        private Material material;

        private Matrix4x4[][] matrixBatches; //Graphics.DrawMeshInstanced can only draw 1023 instances at once, so we use a 2d array to batch the matrices

        public LayerData polygonLayer;
        private List<PolygonVisualisation> visualisations = new();

        private Bounds polygonBounds = new();
        private SampleTexture sampleTexture;

        private TileHandler tileHandler;
        private HashSet<BinaryMeshLayer> layersThatCauseUpdates = new();
        private bool samplingDirty = true;
        
        
        public void LoadProperties(List<LayerPropertyData> properties)
        {
            InitProperty<ToggleScatterPropertyData>(properties, InitializePropertyForBackwardsCompatibility);

            SetParentPolygonLayer(LayerData.ParentLayer);
            var scatterSettings = properties.Get<ScatterGenerationSettingsPropertyData>();
            InitializeScatterMesh(scatterSettings.OriginalPrefabId);
        }

        private void InitializePropertyForBackwardsCompatibility(ToggleScatterPropertyData data)
        {
            data.AllowScatter = true;
            data.IsScattered = true;
        }

        protected override void OnVisualizationInitialize()
        {
            base.OnVisualizationInitialize();
            tileHandler = FindAnyObjectByType<TileHandler>();
        }

        protected override void RegisterEventListeners()
        {
            base.RegisterEventListeners();
            AddReScatterListeners();
            tileHandler.layerAdded.AddListener(AddListenersToCartesianTerrainTiles);
            tileHandler.layerRemoved.AddListener(RemoveListenersFromCartesianTerrainTiles);

            foreach (var layer in tileHandler.layers)
            {
                AddListenersToCartesianTerrainTiles(layer);
            }
        }
        
        protected override void UnregisterEventListeners()
        {
            base.UnregisterEventListeners();
            RemoveReScatterListeners();
            tileHandler.layerAdded.RemoveListener(AddListenersToCartesianTerrainTiles);
            tileHandler.layerRemoved.RemoveListener(RemoveListenersFromCartesianTerrainTiles);

            foreach (var layer in layersThatCauseUpdates.ToArray())
            {
                RemoveListenersFromCartesianTerrainTiles(layer);
            }
        }
        
        protected override void OnVisualizationReady()
        {
            base.OnVisualizationReady();

            TransformLayerPropertyData transformProperty = LayerData.GetProperty<TransformLayerPropertyData>();
            transformProperty.IsEditable = false;
            RecalculatePolygonsAndSamplerTexture();
        }

        private void InitializeScatterMesh(string prefabId)
        {
            var scatterObjectPrefab = ProjectData.Current.PrefabLibrary.GetPrefabById(prefabId);
            this.mesh = CombineHierarchicalMeshes(scatterObjectPrefab.transform);
            var sharedMaterial = scatterObjectPrefab.GetComponentInChildren<MeshRenderer>().sharedMaterial; //todo: make this work with multiple materials for hierarchical meshes?
            this.material = new Material(sharedMaterial);
            this.material.enableInstancing = true;

            var feature = CreateFeature(material);
            LayerFeatures.Add(feature.Geometry, feature);
        }

        public override void ApplyStyling()
        {
            base.ApplyStyling();

            // Apply style to the features that was discovered
            foreach (var feature in LayerFeatures.Values)
            {
                Symbolizer styling = GetStyling(feature);
                var fillColor = styling.GetFillColor();

                // Keep the original material color if fill color is not set (null)
                if (!fillColor.HasValue) return;

                LayerData.Color = fillColor.Value;
                material.SetColor(baseColorID, fillColor.Value);
            }
        }

        public override void OnLayerActiveInHierarchyChanged(bool isActive)
        {
            gameObject.SetActive(isActive);
        }

        public void AddReScatterListeners()
        {
            var settings = LayerData.GetProperty<ScatterGenerationSettingsPropertyData>();
            settings.ScatterSettingsChanged.AddListener(ResampleTexture);
            settings.ScatterDistributionChanged.AddListener(RecalculatePolygonsAndSamplerTexture);
            settings.ScatterShapeChanged.AddListener(RecalculatePolygonsAndSamplerTexture);

            PolygonSelectionLayerPropertyData polygonProperties = polygonLayer.GetProperty<PolygonSelectionLayerPropertyData>();
            polygonProperties.polygonCoordinatesChanged.AddListener(RecalculatePolygonsAndSamplerTexture);

            var toggleScatterPropertyData = LayerData.GetProperty<ToggleScatterPropertyData>();
            toggleScatterPropertyData.IsScatteredChanged.AddListener(ConvertToHierarchicalLayerGameObject);
        }

        public void RemoveReScatterListeners()
        {
            var settings = LayerData.GetProperty<ScatterGenerationSettingsPropertyData>();
            settings.ScatterSettingsChanged.RemoveListener(ResampleTexture);
            settings.ScatterDistributionChanged.RemoveListener(RecalculatePolygonsAndSamplerTexture);
            settings.ScatterShapeChanged.RemoveListener(RecalculatePolygonsAndSamplerTexture);

            PolygonSelectionLayerPropertyData polygonProperties = polygonLayer.GetProperty<PolygonSelectionLayerPropertyData>();
            polygonProperties.polygonCoordinatesChanged.RemoveListener(RecalculatePolygonsAndSamplerTexture);

            var toggleScatterPropertyData = LayerData.GetProperty<ToggleScatterPropertyData>();
            toggleScatterPropertyData.IsScatteredChanged.RemoveListener(ConvertToHierarchicalLayerGameObject);
        }

        private List<CompoundPolygon> CalculateAndVisualisePolygons(CompoundPolygon basePolygon)
        {
            foreach (var visualisation in visualisations)
            {
                Destroy(visualisation.gameObject);
            }

            visualisations.Clear();

            var settings = LayerData.GetProperty<ScatterGenerationSettingsPropertyData>();
            var strokeWidth = -settings.StrokeWidth; //invert so the stroke is always inset
            if (settings.FillType == FillType.Complete)
                strokeWidth = 0;

            var polygons = PolygonUtility.CalculatePolygons(settings.FillType, basePolygon, strokeWidth);
            foreach (var polygon in polygons)
            {
                var visualisation = CreatePolygonMesh(polygon);
                visualisations.Add(visualisation);
                visualisation.gameObject.SetActive(false); //only enable when rendering
            }

            return polygons;
        }

        private PolygonVisualisation CreatePolygonMesh(CompoundPolygon polygon)
        {
            var contours = new List<List<Vector3>>(polygon.Paths.Count);
            for (int i = 0; i < polygon.Paths.Count; i++)
            {
                contours.Add(polygon.Paths[i].ToVector3List());
            }

            var polygonVisualisation = PolygonVisualisationUtility.CreateAndReturnPolygonObject(contours, 1f, false, false, false, polygonMaterial);
            polygonVisualisation.DrawLine = true;

            polygonVisualisation.gameObject.layer = LayerMask.NameToLayer("ScatterPolygons");
            return polygonVisualisation;
        }

        private void RecalculatePolygonsAndSamplerTexture()
        {
            samplingDirty = true;
        }

        private void CalculatePolygonsAndSamplerTexture()
        {
            RecalculatePolygonsAndGetBounds();
            if (polygonBounds.size.sqrMagnitude == 0)
                return; // the stroke/fill is clipped out because of the stroke width and no further processing is needed

            var settings = LayerData.GetProperty<ScatterGenerationSettingsPropertyData>();
            var densityPerSquareUnit = settings.Density / 10000f; //in de UI is het het bomen per hectare, in de functie is het punten per m2
            var normalizedScatter = settings.Scatter / 100f;
            ScatterMap.Instance.GenerateScatterPoints(visualisations, polygonBounds, densityPerSquareUnit, normalizedScatter, settings.Angle, UpdateSampleTexture);
        }

        private void UpdateSampleTexture(SampleTexture newTexture)
        {
            sampleTexture = newTexture;
            ResampleTexture();
        }

        private Bounds RecalculatePolygonsAndGetBounds()
        {
            PolygonSelectionLayerPropertyData polygonProperties = polygonLayer.GetProperty<PolygonSelectionLayerPropertyData>();
            if (polygonProperties.ShapeType == ShapeType.Line)
            {
                var settings = LayerData.GetProperty<ScatterGenerationSettingsPropertyData>();
                settings.Angle = CalculateLineAngle(polygonProperties);
            }

            var boundingBox = polygonProperties.PolygonBoundingBox;
            if (boundingBox == null)
                return new Bounds();
            
            polygonBounds = boundingBox.ToUnityBounds();

            var vertices = PolygonUtility.CoordinatesToVertices(polygonProperties.OriginalPolygon, polygonProperties.LineWidth);
            var polygons = CalculateAndVisualisePolygons(new CompoundPolygon(vertices));
            if (polygons.Count == 0)
                return new Bounds(); // the stroke/fill is clipped out because of the stroke width and no further processing is needed

            var bounds = polygons[0].Bounds; //start with the bounds of the first polygon and add the others if needed
            for (var index = 1; index < polygons.Count; index++)
            {
                var polygon = polygons[index];
                bounds.Encapsulate(polygon.Bounds);
            }

            return polygonBounds;
        }

        public void ReGeneratePointsWithoutResampling()
        {
            RecalculatePolygonsAndGetBounds();
            ResampleTexture();
        }

        private void ResampleTexture()
        {
            var settings = LayerData.GetProperty<ScatterGenerationSettingsPropertyData>();
            var densityPerSquareUnit = settings.Density / 10000; //in de UI is het het bomen per hectare, in de functie is het punten per m2
            float cellSize = 1f / Mathf.Sqrt(densityPerSquareUnit);
            var gridPoints = CompoundPolygon.GenerateGridPoints(polygonBounds, cellSize, 0, out var gridBounds); //dont rotate the grid here, we will rotate the results after sampling to avoid issues with anti-aliassing
            var normalizedScatter = settings.Scatter / 100f;
            ScatterMap.Instance.SampleTexture(sampleTexture, gridPoints, gridBounds, normalizedScatter, cellSize, settings.Angle, ProcessScatterPoints);
        }

        private void ProcessScatterPoints(List<Vector3> scatterPoints, List<Vector2> sampledScales)
        {
            var batchCount = (scatterPoints.Count / 1023) + 1; //x batches of 1023 + 1 for the remainder
            var remainder = scatterPoints.Count % 1023;

            matrixBatches = new Matrix4x4[batchCount][];

            // var meshOriginOffset = mesh.bounds.extents.y;
            var tempMatrix = new Matrix4x4();
            var scale = new Vector3();
            for (int i = 0; i < batchCount; i++)
            {
                var arraySize = i == batchCount - 1 ? remainder : 1023;
                matrixBatches[i] = new Matrix4x4[arraySize];
                for (int j = 0; j < arraySize; j++)
                {
                    var index = 1023 * i + j;
                    var settings = LayerData.GetProperty<ScatterGenerationSettingsPropertyData>();
                    scale.x = Mathf.Lerp(settings.MinScale.x, settings.MaxScale.x, sampledScales[index].x);
                    scale.y = Mathf.Lerp(settings.MinScale.y, settings.MaxScale.y, sampledScales[index].y);
                    scale.z = scale.x; // also x since we are taking a diameter
                    tempMatrix.SetTRS(scatterPoints[index], Quaternion.identity, scale);
                    matrixBatches[i][j] = tempMatrix;
                }
            }
        }

        private void Update()
        {
            if (samplingDirty)
            {
                samplingDirty = false;
                CalculatePolygonsAndSamplerTexture();
            }
            RenderBatches();
        }

        private void RenderBatches()
        {
            if (matrixBatches == null) //this happens in the first frame because the polygon is not yet initialized
                return;

            foreach (var matrixBatch in matrixBatches)
            {
                Graphics.DrawMeshInstanced(mesh, 0, material, matrixBatch);
            }
        }

        public override void OnSelect(LayerData layer)
        {
            base.OnSelect(layer);
            foreach (var visualisation in visualisations)
            {
                visualisation.DrawLine = true;
            }
        }

        public override void OnDeselect(LayerData layer)
        {
            base.OnDeselect(layer);
            foreach (var visualisation in visualisations)
            {
                visualisation.DrawLine = false;
            }
        }

        public override void OnSiblingIndexOrParentChanged(int newSiblingIndex)
        {
            base.OnSiblingIndexOrParentChanged(newSiblingIndex);

            var newPolygonParent = LayerData.ParentLayer;
            if (newPolygonParent != polygonLayer) //the parent changed
            {
                SetParentPolygonLayer(newPolygonParent);
            }
        }

        private void SetParentPolygonLayer(LayerData newPolygonParent)
        {
            PolygonSelectionLayerPropertyData polygonProperties = newPolygonParent.GetProperty<PolygonSelectionLayerPropertyData>();
            if (polygonProperties != null) //the new parent is also a polygon layer
            {
                var autoRotate = polygonProperties.ShapeType == ShapeType.Line;
                LayerData.GetProperty<ScatterGenerationSettingsPropertyData>().AutoRotateToLine = autoRotate;

                polygonProperties.polygonCoordinatesChanged.RemoveListener(RecalculatePolygonsAndSamplerTexture);

                polygonLayer = newPolygonParent;
                RecalculatePolygonsAndSamplerTexture();
                polygonProperties.polygonCoordinatesChanged.AddListener(RecalculatePolygonsAndSamplerTexture);
            }
            else //the layer is no longer parented to a polygon layer
            {
                LayerData.GetProperty<ToggleScatterPropertyData>().IsScattered = false; //revert to Object Visualization
            }
        }

        private void AddListenersToCartesianTerrainTiles(Netherlands3D.CartesianTiles.Layer layer)
        {
            //only listen to the terrain layer
            if (layer == null || layer.gameObject.layer != LayerMask.NameToLayer("Terrain"))
                return;
            
            BinaryMeshLayer bml = layer as BinaryMeshLayer;
            if (!layersThatCauseUpdates.Add(bml))
                return;
            
            bml.OnTileObjectCreated.AddListener(OnTerrainTileCreated);
            RecalculatePolygonsAndSamplerTexture();
        }
        
        private void RemoveListenersFromCartesianTerrainTiles(Netherlands3D.CartesianTiles.Layer layer)
        {
            //only listen to the terrain layer
            if (layer == null || layer.gameObject.layer != LayerMask.NameToLayer("Terrain"))
                return;

            BinaryMeshLayer bml = layer as BinaryMeshLayer;
            if (!layersThatCauseUpdates.Remove(bml))
                return;
            
            bml.OnTileObjectCreated.RemoveListener(OnTerrainTileCreated);
            RecalculatePolygonsAndSamplerTexture();
        }

        private void OnTerrainTileCreated(Tile tile)
        {
            var polygonBoundingBox = polygonLayer.GetProperty<PolygonSelectionLayerPropertyData>().PolygonBoundingBox;
            polygonBoundingBox.Convert(CoordinateSystem.RD);
            int size = tile.layer.tileSize;
            BoundingBox tileBox = new BoundingBox(
                new Coordinate(CoordinateSystem.RD, tile.tileKey.x, tile.tileKey.y), 
                new Coordinate(CoordinateSystem.RD, tile.tileKey.x + size, tile.tileKey.y + size));
            //is a tile being loaded intersecting with the polygon then regenerate the sampler texture
            if (tileBox.Intersects(polygonBoundingBox))
            {
                RecalculatePolygonsAndSamplerTexture();
            }
        }

        private static Mesh CombineHierarchicalMeshes(Transform transform)
        {
            var originalPosition = transform.position;
            var originalRotation = transform.rotation;
            var originalScale = transform.localScale;

            transform.position = Vector3.zero; //set position to 0 to get the correct worldToLocalMatrix
            transform.rotation = Quaternion.identity;
            transform.localScale = Vector3.one;

            var meshFilters = transform.GetComponentsInChildren<MeshFilter>();
            CombineInstance[] combine = new CombineInstance[meshFilters.Length];
            for (int i = 0; i < meshFilters.Length; i++)
            {
                combine[i].mesh = meshFilters[i].sharedMesh;
                combine[i].transform = meshFilters[i].transform.localToWorldMatrix;
            }

            Mesh mesh = new Mesh();
            mesh.CombineMeshes(combine);
            mesh.RecalculateBounds();

            transform.position = originalPosition; //reset position
            transform.rotation = originalRotation; //reset rotation
            transform.localScale = originalScale; //reset scale

            if (mesh.vertices.Length == 0)
                Debug.LogError("Combined mesh has no vertices, is read/write of the source meshes enabled?");

            return mesh;
        }

        private static float CalculateLineAngle(PolygonSelectionLayerPropertyData polygon)
        {
            var linePoints = polygon.OriginalPolygon.ToUnityPositions().ToList();
            var start = new Vector2(linePoints[0].x, linePoints[0].z);
            var end = new Vector2(linePoints[1].x, linePoints[1].z);
            var dir = end - start;
            return Vector2.Angle(Vector2.up, dir);
        }

        private void ConvertToHierarchicalLayerGameObject(bool isScattered)
        {
            if (isScattered)
                return;

            App.Layers.VisualizeAs(LayerData, LayerData.GetProperty<ScatterGenerationSettingsPropertyData>().OriginalPrefabId);
        }
    }
}