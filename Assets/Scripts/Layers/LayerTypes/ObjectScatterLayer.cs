using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Netherlands3D.Twin.Layers.LayerTypes;
using Netherlands3D.Twin.Layers.Properties;
using Netherlands3D.Twin.UI.LayerInspector;
using UnityEngine;
using UnityEngine.InputSystem;

namespace Netherlands3D.Twin.UI.LayerInspector
{
    public class ObjectScatterLayer : ReferencedLayer, ILayerWithProperties
    {
        private Mesh mesh;
        private Material material;
        private ScatterGenerationSettings settings;
        public ScatterGenerationSettings Settings => settings;
        private Matrix4x4[][] matrixBatches; //Graphics.DrawMeshInstanced can only draw 1023 instances at once
        private PolygonSelectionLayer polygonLayer => ReferencedProxy.ParentLayer as PolygonSelectionLayer;
        private List<IPropertySection> propertySections = new();

        public override bool IsActiveInScene
        {
            get => gameObject.activeSelf;
            set
            {
                gameObject.SetActive(value);
                ReferencedProxy.UI.MarkLayerUIAsDirty();
            }
        }

        public void Initialize(PolygonSelectionLayer polygon, Mesh mesh, Material material)
        {
            this.mesh = mesh;
            this.material = material;
            settings = ScriptableObject.CreateInstance<ScatterGenerationSettings>();
            settings.SettingsChanged.AddListener(RecalculateScatterMatrices);
            propertySections = new List<IPropertySection>() { settings };

            StartCoroutine(InitializeAfterReferencedProxy(polygon));
        }

        private IEnumerator InitializeAfterReferencedProxy(PolygonSelectionLayer polygon)
        {
            yield return null; //wait for ReferencedProxy layer to be initialized
            ReferencedProxy.SetParent(polygon);
            RecalculateScatterMatrices();
            polygon.polygonChanged.AddListener(RecalculateScatterMatrices);
        }

        protected override void OnDestroy()
        {
            settings.SettingsChanged.RemoveListener(RecalculateScatterMatrices);
            polygonLayer.polygonChanged.RemoveListener(RecalculateScatterMatrices);
        }

        private void RecalculateScatterMatrices()
        {
            print("Density: " + settings.Density);
            print("Scatter: " + settings.Scatter);
            print("Angle: " + settings.Angle);
            ScatterMap.Instance.GenerateScatterPoints(polygonLayer.Polygon, settings.Density / 10, settings.Scatter, settings.Angle, ProcessScatterPoints); //todo: when settings change but polygon doesn't don't re-render the scatter camera
        }

        private void ProcessScatterPoints(List<Vector3> scatterPoints)
        {
            // var scatterPoints = CompoundPolygon.GenerateScatterPoints(polygonLayer.Polygon, settings.Density, settings.Scatter, settings.Angle);
            var batchCount = (scatterPoints.Count / 1023) + 1; //x batches of 1023 + 1 for the remainder
            var remainder = scatterPoints.Count % 1023;

            print(scatterPoints.Count + " points in " + (batchCount - 1) + " batches of 1023 and a remainder of " + remainder);
            matrixBatches = new Matrix4x4[batchCount][];

            var meshOriginOffset = 0; //todo mesh.bounds.extents.y;
            print("offset: " + meshOriginOffset);
            var tempMatrix = new Matrix4x4();
            for (int i = 0; i < batchCount; i++)
            {
                var arraySize = i == batchCount - 1 ? remainder : 1023;
                matrixBatches[i] = new Matrix4x4[arraySize];
                for (int j = 0; j < arraySize; j++)
                {
                    // var pos = new Vector3(scatterPoints[1023 * i + j].x, 10, scatterPoints[1023 * i + j].y); //todo: use optical raycaster to determine y of entire polygon
                    // var scale = settings.GenerateRandomScale();
                    // var pos = scatterPoints[1023 * i + j];// + new Vector3(0, meshOriginOffset * scale.y, 0);
                    // var rot = Quaternion.identity;
                    tempMatrix.SetTRS(scatterPoints[1023 * i + j], Quaternion.identity, 10 * settings.GenerateRandomScale());
                    matrixBatches[i][j] = tempMatrix;
                }
            }
        }

        private void Update()
        {
            if (Keyboard.current.upArrowKey.wasPressedThisFrame)
            {
                settings.Scatter = Mathf.Clamp01(settings.Scatter + 0.1f);
            }
            else if (Keyboard.current.downArrowKey.wasPressedThisFrame)
            {
                settings.Scatter = Mathf.Clamp01(settings.Scatter - 0.1f);
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

        public List<IPropertySection> GetPropertySections()
        {
            return propertySections;
        }
    }
}