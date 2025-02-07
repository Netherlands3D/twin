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
        public override BoundingBox Bounds => throw new NotImplementedException(); //todo
        public PolygonVisualisation PolygonVisualisation { get; private set; }
        public Material PolygonMeshMaterial;

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
            }
            else
            {
                PolygonVisualisation.UpdateVisualisation(polygon3D);
            }
        }

        private PolygonVisualisation CreatePolygonMesh(List<Vector3> polygon, float polygonExtrusionHeight, Material polygonMeshMaterial)
        {
            var contours = new List<List<Vector3>> { polygon };
            var polygonVisualisation = PolygonVisualisationUtility.CreateAndReturnPolygonObject(contours, polygonExtrusionHeight, false, false, false, polygonMeshMaterial);

            //Add the polygon shifter to the polygon visualisation, so it can move with our origin shifts
            polygonVisualisation.DrawLine = false; //lines will be drawn per layer, but a single mesh will receive clicks to select
            polygonVisualisation.gameObject.layer = LayerMask.NameToLayer("Projected");

            return polygonVisualisation;
        }

        public List<IPropertySectionInstantiator> GetPropertySections()
        {
            var polygon = LayerData as PolygonSelectionLayer;
            if (polygon.ShapeType == ShapeType.Line)
                return GetComponents<IPropertySectionInstantiator>().ToList(); //LineWidth

            return new List<IPropertySectionInstantiator>(); //no properties for a polygon
        }

        protected virtual void OnDestroy()
        {
            Destroy(PolygonVisualisation.gameObject);
        }
    }
}