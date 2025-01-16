using Netherlands3D.Twin.Layers.Editor;
using UnityEditor;
using UnityEditor.UIElements;
using UnityEngine.UIElements;

namespace Netherlands3D.Twin.Layers.LayerTypes.GeoJsonLayers.Editor
{
    [CustomEditor(typeof(GeoJsonLayerGameObject))]
    public class GeoJsonLayerGameObjectEditor : UnityEditor.Editor
    {
        public override VisualElement CreateInspectorGUI()
        {
            var root = new VisualElement();

            InspectorElement.FillDefaultInspector(root, serializedObject, this);

            var layerGameObject = (GeoJsonLayerGameObject)target;
            LayerDataVisualElements.LayerData(layerGameObject.LayerData, root);

            return root;
        }
    }
}