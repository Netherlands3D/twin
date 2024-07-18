using System.Collections.Generic;
using Netherlands3D.SelectionTools;
using Netherlands3D.Twin.FloatingOrigin;
using Netherlands3D.Twin.Layers.LayerTypes;
using Netherlands3D.Twin.Layers.Properties;
using UnityEngine;

namespace Netherlands3D.Twin.Layers
{
    public enum FillType
    {
        Complete,
        Fill,
        Stroke,
    }

    public class ObjectScatterLayerGameObject : LayerGameObject, ILayerWithProperties
    {
        private GameObject originalObject;
        private Mesh mesh;
        private Material material;
        private ScatterGenerationSettings settings;
        public ScatterGenerationSettings Settings => settings;
        private ToggleScatterPropertySectionInstantiator toggleScatterPropertySectionInstantiator;
        private Matrix4x4[][] matrixBatches; //Graphics.DrawMeshInstanced can only draw 1023 instances at once, so we use a 2d array to batch the matrices
        public PolygonSelectionLayer polygonLayer;
        private List<IPropertySectionInstantiator> propertySections = new();
        private List<PolygonVisualisation> visualisations = new();

        private Bounds polygonBounds = new();
        private SampleTexture sampleTexture;

        private WorldTransform worldTransform;

        private bool completedInitialization;

        public void Initialize(GameObject originalObject, PolygonSelectionLayer polygon, List<LayerData> children)
        {
            this.originalObject = originalObject;
            this.mesh = CombineHierarchicalMeshes(originalObject.transform);
            this.material = originalObject.GetComponentInChildren<MeshRenderer>().material; //todo: make this work with multiple materials for hierarchical meshes?
            this.material.enableInstancing = true;

            originalObject.SetActive(false); //todo: does this affect the WorldTransformShifter?
            polygonLayer = polygon;

            toggleScatterPropertySectionInstantiator = GetComponent<ToggleScatterPropertySectionInstantiator>();

            if (!toggleScatterPropertySectionInstantiator)
                toggleScatterPropertySectionInstantiator = gameObject.AddComponent<ToggleScatterPropertySectionInstantiator>();

            settings = ScriptableObject.CreateInstance<ScatterGenerationSettings>();
            settings.Density = 1000; // per ha for the UI
            if (polygon.ShapeType == ShapeType.Line)
            {
                settings.Angle = -1; //set angle to a value outside of the 0-180 range of Vector2.Angle in CalculateLineAngle(), because this will ensure the onchange event to be called when initializing the first time in SetAngleAndUpdateSampleTexture
                settings.AutoRotateToLine = true;
            }

            settings.MinScale = new Vector3(3, 3, 3);
            settings.MaxScale = new Vector3(6, 6, 6);

            propertySections = new List<IPropertySectionInstantiator>() { toggleScatterPropertySectionInstantiator, settings };

            gameObject.AddComponent<ScatterLayerShifter>();
            gameObject.AddComponent<WorldTransform>();

            LayerData.SetParent(polygon);
            foreach (var child in children)
            {
                child.SetParent(LayerData);
            }

            RecalculatePolygonsAndSamplerTexture();
            AddReScatterListeners();
            
// #if UNITY_EDITOR
//          gameObject.AddComponent<GridDebugger>();
// #endif
            completedInitialization = true;
        }

        protected override void OnLayerActiveInHierarchyChanged(bool isActive)
        {
            gameObject.SetActive(isActive);
        }

        public void AddReScatterListeners()
        {
            settings.ScatterSettingsChanged.AddListener(ResampleTexture);
            settings.ScatterDistributionChanged.AddListener(RecalculatePolygonsAndSamplerTexture);
            settings.ScatterShapeChanged.AddListener(RecalculatePolygonsAndSamplerTexture);
            polygonLayer.polygonChanged.AddListener(RecalculatePolygonsAndSamplerTexture);
            polygonLayer.polygonMoved.AddListener(RecalculatePolygonsAndSamplerTexture);
        }

        public void RemoveReScatterListeners()
        {
            settings.ScatterSettingsChanged.RemoveListener(ResampleTexture);
            settings.ScatterDistributionChanged.RemoveListener(RecalculatePolygonsAndSamplerTexture);
            settings.ScatterShapeChanged.RemoveListener(RecalculatePolygonsAndSamplerTexture);
            polygonLayer.polygonChanged.RemoveListener(RecalculatePolygonsAndSamplerTexture);
            polygonLayer.polygonMoved.RemoveListener(RecalculatePolygonsAndSamplerTexture);
        }

        protected override void OnDestroy()
        {
            base.OnDestroy();
            RemoveReScatterListeners();
        }

        private List<CompoundPolygon> CalculateAndVisualisePolygons(CompoundPolygon basePolygon)
        {
            foreach (var visualisation in visualisations)
            {
                Destroy(visualisation.gameObject);
            }

            visualisations.Clear();

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

            var polygonVisualisation = PolygonVisualisationUtility.CreateAndReturnPolygonObject(contours, 1f, false, false, false, polygonLayer.PolygonMeshMaterial);
            polygonVisualisation.DrawLine = true;

            polygonVisualisation.gameObject.layer = LayerMask.NameToLayer("ScatterPolygons");
            return polygonVisualisation;
        }

        private void RecalculatePolygonsAndSamplerTexture()
        {
            RecalculatePolygonsAndGetBounds();
            if (polygonBounds.size.sqrMagnitude == 0)
                return; // the stroke/fill is clipped out because of the stroke width and no further processing is needed

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
            if (polygonLayer.ShapeType == ShapeType.Line)
                settings.Angle = CalculateLineAngle(polygonLayer);

            var polygons = CalculateAndVisualisePolygons(polygonLayer.Polygon);
            if (polygons.Count == 0)
                return new Bounds(); // the stroke/fill is clipped out because of the stroke width and no further processing is needed

            var bounds = polygons[0].Bounds; //start with the bounds of the first polygon and add the others if needed
            for (var index = 1; index < polygons.Count; index++)
            {
                var polygon = polygons[index];
                bounds.Encapsulate(polygon.Bounds);
            }

            polygonBounds = bounds;
            return bounds;
        }

        public void ReGeneratePointsWithoutResampling()
        {
            RecalculatePolygonsAndGetBounds();
            ResampleTexture();
        }

        private void ResampleTexture()
        {
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

        public override void OnSelect()
        {
            base.OnSelect();
            foreach (var visualisation in visualisations)
            {
                visualisation.DrawLine = true;
            }
        }

        public override void OnDeselect()
        {
            base.OnDeselect();
            foreach (var visualisation in visualisations)
            {
                visualisation.DrawLine = false;
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
                combine[i].mesh = meshFilters[i].mesh;
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

        public override void OnSiblingIndexOrParentChanged(int newSiblingIndex)
        {
            base.OnSiblingIndexOrParentChanged(newSiblingIndex);

            if (!completedInitialization) //this is needed because the initial instantiation will also set the parent, and this should not do any of the logic below before this layer is properly initialized.
                return;

            var newPolygonParent = LayerData.ParentLayer as PolygonSelectionLayer;
            if (newPolygonParent == null) //new parent is not a polygon, so the scatter layer should revert to its original object
            {
                RevertToHierarchicalObjectLayer();
                return;
            }

            if (newPolygonParent != polygonLayer) //the new parent is a polygon, but not the same as the one currently registered, so a reinitialization is required.
            {
                polygonLayer.polygonChanged.RemoveListener(RecalculatePolygonsAndSamplerTexture);
                polygonLayer.polygonMoved.RemoveListener(RecalculatePolygonsAndSamplerTexture);

                polygonLayer = newPolygonParent;
                RecalculatePolygonsAndSamplerTexture();
                polygonLayer.polygonMoved.AddListener(RecalculatePolygonsAndSamplerTexture);
                polygonLayer.polygonChanged.AddListener(RecalculatePolygonsAndSamplerTexture);
            }
        }

        public void RevertToHierarchicalObjectLayer()
        {
            gameObject.SetActive(true); //need to activate the GameObject to start the coroutine
            originalObject.SetActive(true);
            var layer = originalObject.AddComponent<HierarchicalObjectLayerGameObject>();
            layer.LayerData.ActiveSelf = true;

            foreach (var child in LayerData.ChildrenLayers)
            {
                child.SetParent(layer.LayerData);
            }

            layer.LayerData.SetParent(LayerData.ParentLayer, LayerData.SiblingIndex);
            DestroyLayer();
        }

        public List<IPropertySectionInstantiator> GetPropertySections()
        {
            return propertySections;
        }

        private static float CalculateLineAngle(PolygonSelectionLayer polygon)
        {
            var start = new Vector2(polygon.OriginalPolygon[0].x, polygon.OriginalPolygon[0].z);
            var end = new Vector2(polygon.OriginalPolygon[1].x, polygon.OriginalPolygon[1].z);
            var dir = end - start;
            return Vector2.Angle(Vector2.up, dir);
        }
    }
}