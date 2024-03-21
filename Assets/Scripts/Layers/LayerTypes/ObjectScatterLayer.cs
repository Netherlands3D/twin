using System.Collections;
using System.Collections.Generic;
using Netherlands3D.SelectionTools;
using Netherlands3D.Twin.Layers.LayerTypes;
using Netherlands3D.Twin.Layers.Properties;
using Netherlands3D.Twin.UI.LayerInspector;
using UnityEngine;
using UnityEngine.InputSystem;

namespace Netherlands3D.Twin.Layers
{
    public enum FillType
    {
        Complete,
        Fill,
        Stroke,
    }

    public class ObjectScatterLayer : ReferencedLayer, ILayerWithProperties
    {
        private GameObject originalObject;
        private Mesh mesh;
        private Material material;
        private ScatterGenerationSettings settings;
        public ScatterGenerationSettings Settings => settings;
        private Matrix4x4[][] matrixBatches; //Graphics.DrawMeshInstanced can only draw 1023 instances at once, so we use a 2d array to batch the matrices
        private PolygonSelectionLayer polygonLayer; // => ReferencedProxy.ParentLayer as PolygonSelectionLayer;
        private List<IPropertySectionInstantiator> propertySections = new();
        private List<PolygonVisualisation> visualisations = new();

        private bool completedInitialization;

        public override bool IsActiveInScene
        {
            get => gameObject.activeSelf;
            set
            {
                gameObject.SetActive(value);
                ReferencedProxy.UI.MarkLayerUIAsDirty();
            }
        }

        public void Initialize(GameObject originalObject, PolygonSelectionLayer polygon, bool initialActiveState)
        {
            this.originalObject = originalObject;
            this.mesh = CombineHierarchicalMeshes(originalObject.transform);
            this.material = originalObject.GetComponentInChildren<MeshRenderer>().material; //todo: make this work with multiple materials for hierarchical meshes?
            polygonLayer = polygon;

            settings = ScriptableObject.CreateInstance<ScatterGenerationSettings>();
            settings.Density = 1000; // per ha for the UI
            settings.MinScale = new Vector3(3, 3, 3);
            settings.MaxScale = new Vector3(6, 6, 6);
            settings.SettingsChanged.AddListener(RecalculateScatterMatrices);
            propertySections = new List<IPropertySectionInstantiator>() { settings };

            StartCoroutine(InitializeAfterReferencedProxy(polygon, initialActiveState));
        }

        private IEnumerator InitializeAfterReferencedProxy(PolygonSelectionLayer polygon, bool initialActiveState)
        {
            yield return null; //wait for ReferencedProxy layer to be initialized
            ReferencedProxy.SetParent(polygon);
            RecalculateScatterMatrices();
            polygon.polygonChanged.AddListener(RecalculateScatterMatrices);
            ReferencedProxy.ActiveSelf = initialActiveState; //set to same state as current layer

            completedInitialization = true;
        }

        protected override void OnDestroy()
        {
            base.OnDestroy();
            settings.SettingsChanged.RemoveListener(RecalculateScatterMatrices);
            polygonLayer.polygonChanged.RemoveListener(RecalculateScatterMatrices);
        }

        private List<CompoundPolygon> CalculateAndVisualisePolygons(CompoundPolygon basePolygon)
        {
            foreach (var visualisation in visualisations)
            {
                Destroy(visualisation.gameObject);
            }

            var strokeWidth = -settings.StrokeWidth; //invert so the stroke is always inset
            if (settings.FillType == FillType.Complete)
                strokeWidth = 0;

            var polygons = PolygonUtility.CalculatePolygons(settings.FillType, basePolygon, strokeWidth);
            visualisations = new List<PolygonVisualisation>(polygons.Count);
            foreach (var polygon in polygons)
            {
                visualisations.Add(CreatePolygonMesh(polygon));
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

        private void RecalculateScatterMatrices()
        {
            var polygons = CalculateAndVisualisePolygons(polygonLayer.Polygon);
            if (polygons.Count == 0)
                return; // the stroke/fill is clipped out because of the stroke width and no further processing is needed

            var bounds = polygons[0].Bounds; //start with the bounds of the first polygon and add the others if needed
            for (var index = 1; index < polygons.Count; index++)
            {
                var polygon = polygons[index];
                bounds.Encapsulate(polygon.Bounds);
            }

            var densityPerSquareUnit = settings.Density / 10000; //in de UI is het het bomen per hectare, in de functie is het punten per m2
            ScatterMap.Instance.GenerateScatterPoints(bounds, densityPerSquareUnit, settings.Scatter, settings.Angle, ProcessScatterPoints); //todo: when settings change but polygon doesn't don't re-render the scatter camera
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

            var newPolygonParent = ReferencedProxy.ParentLayer as PolygonSelectionLayer;
            if (!newPolygonParent) //new parent is not a polygon, so the scatter layer should revert to its original object
            {
                var initialActiveState = IsActiveInScene;
                gameObject.SetActive(true); //need to activate the GameObject to start the coroutine
                StartCoroutine(ConvertToHierarchicalObjectAtEndOfFrame(initialActiveState));
                return;
            }

            if (newPolygonParent && newPolygonParent != polygonLayer) //the new parent is a polygon, but not the same as the one currently registered, so a reinitialization is required.
            {
                polygonLayer.polygonChanged.RemoveListener(RecalculateScatterMatrices);
                polygonLayer = newPolygonParent;
                RecalculateScatterMatrices();
                polygonLayer.polygonChanged.AddListener(RecalculateScatterMatrices);
            }
        }

        private IEnumerator ConvertToHierarchicalObjectAtEndOfFrame(bool initialActiveState)
        {
            originalObject.gameObject.SetActive(true); //activate to initialize the added component.
            var layer = originalObject.AddComponent<HierarchicalObjectLayer>();
            yield return new WaitForEndOfFrame(); //wait for layer component to initialize
            layer.ReferencedProxy.SetParent(ReferencedProxy.ParentLayer, ReferencedProxy.transform.GetSiblingIndex());
            layer.ReferencedProxy.ActiveSelf = initialActiveState; //set to same state as current layer
            DestroyLayer();
        }

        public List<IPropertySectionInstantiator> GetPropertySections()
        {
            return propertySections;
        }
    }
}