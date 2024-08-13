using System.Collections;
using System.Collections.Generic;
using Netherlands3D.SelectionTools;
using Netherlands3D.Twin.Layers;
using Newtonsoft.Json;
using UnityEngine;

namespace Netherlands3D.Twin
{
    public class PolygonSelectionVisualisation : MonoBehaviour
    {
        public PolygonVisualisation PolygonVisualisation { get; private set; }
        public Material PolygonMeshMaterial { get; set; }

        /// <summary>
        /// Create or update PolygonVisualisation
        /// </summary>
        public void UpdateVisualisation(Vector2[] newPolygon, float extrusionHeight)
        {
            var polygon3D = newPolygon.ToVector3List();

            if (!PolygonVisualisation)
                PolygonVisualisation = CreatePolygonMesh(polygon3D, extrusionHeight, PolygonMeshMaterial);
            else
                PolygonVisualisation.UpdateVisualisation(polygon3D);
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

    }
}
