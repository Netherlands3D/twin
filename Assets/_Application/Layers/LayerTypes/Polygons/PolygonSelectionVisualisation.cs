using System;
using System.Collections.Generic;
using System.Linq;
using Netherlands3D.SelectionTools;
using Netherlands3D.Twin.ExtensionMethods;
using Netherlands3D.Twin.Layers.Properties;
using Netherlands3D.Twin.Utility;
using UnityEngine;

namespace Netherlands3D.Twin.Layers.LayerTypes.Polygons
{
    public class PolygonSelectionVisualisation : LayerGameObject, ILayerWithPropertyPanels
    {
        public override bool IsMaskable => false;

        private BoundingBox polygonBounds;
        public override BoundingBox Bounds => polygonBounds;
        public PolygonVisualisation PolygonVisualisation { get; private set; }
        public Material PolygonMeshMaterial;
        [SerializeField] private Material polygonMaskMaterial;
        private bool isMask;

        /// <summary>
        /// Create or update PolygonVisualisation
        /// </summary>
        public void UpdateVisualisation(Vector2[] newPolygon, float extrusionHeight)
        {
            var polygon3D = newPolygon.ToVector3List();

            if (!PolygonVisualisation)
            {
                PolygonVisualisation = CreatePolygonMesh(polygon3D, extrusionHeight, PolygonMeshMaterial);
                PolygonVisualisation.transform.SetParent(transform);
                polygonBounds = new(PolygonVisualisation.GetComponent<Renderer>().bounds);
            }
            else
            {
                PolygonVisualisation.UpdateVisualisation(polygon3D);
                polygonBounds = new(PolygonVisualisation.GetComponent<Renderer>().bounds);
            }

            PolygonProjectionMask.ForceUpdateVectorsAtEndOfFrame();
        }

        private PolygonVisualisation CreatePolygonMesh(List<Vector3> polygon, float polygonExtrusionHeight, Material polygonMeshMaterial)
        {
            var contours = new List<List<Vector3>> { polygon };
            var polygonVisualisation = PolygonVisualisationUtility.CreateAndReturnPolygonObject(contours, polygonExtrusionHeight, false, false, true, polygonMeshMaterial);

            //Add the polygon shifter to the polygon visualisation, so it can move with our origin shifts
            polygonVisualisation.DrawLine = false; //lines will be drawn per layer, but a single mesh will receive clicks to select
            polygonVisualisation.gameObject.layer = LayerMask.NameToLayer("Projected");

            return polygonVisualisation;
        }

        public List<IPropertySectionInstantiator> GetPropertySections()
        {
            return GetComponents<IPropertySectionInstantiator>().ToList();
        }

        protected override void OnDestroy()
        {
            Destroy(PolygonVisualisation.gameObject);
        }

        public void SetMaterial(bool isMask, int bitIndex, bool invert)
        {
            if (!isMask)
            {
                Destroy(PolygonVisualisation.VisualisationMaterial); //clean up the mask material instance
                PolygonVisualisation.VisualisationMaterial = PolygonMeshMaterial;
                this.isMask = false;

                return;
            }

            // the max integer value we can represent in a float without rounding errors is 2^24-1, so we can support 23 masking bit channels
            if (bitIndex < 0 || bitIndex > 23)
                throw new IndexOutOfRangeException("bitIndex must be 23 or smaller to avoid floating point rounding errors since we must use a float formatted masking texture. BitIndex value: " + bitIndex);
            
            int maskValue = 1 << bitIndex;
            float floatMaskValue = (float)maskValue;
            var bitMask = new Vector4(floatMaskValue, 0, 0, 1); //regular masks use the red channel
            if (invert)
                bitMask = new Vector4(0, floatMaskValue, 0, 1); //invert masks use the green channel

            if (this.isMask != isMask)
            {
                var newMat = new Material(polygonMaskMaterial);
                PolygonVisualisation.VisualisationMaterial = newMat;
            }
            
            PolygonVisualisation.VisualisationMaterial.SetVector("_MaskBitMask", bitMask);
            
            this.isMask = true;
        }
    }
}