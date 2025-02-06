using Netherlands3D.Twin.Layers.Editor;
using UnityEditor;
using UnityEditor.UIElements;
using UnityEngine.UIElements;

namespace Netherlands3D.Functionalities.OgcWebServices.Wfs.Editor
{
    [CustomEditor(typeof(WFSGeoJsonLayerGameObject))]
    public class WfsGeoJsonLayerGameObjectEditor : UnityEditor.Editor
    {
        public override VisualElement CreateInspectorGUI()
        {
            var root = new VisualElement();

            InspectorElement.FillDefaultInspector(root, serializedObject, this);

            var layerGameObject = (WFSGeoJsonLayerGameObject)target;
            LayerDataVisualElements.LayerData(layerGameObject.LayerData, root);

            return root;
        }
    }
}