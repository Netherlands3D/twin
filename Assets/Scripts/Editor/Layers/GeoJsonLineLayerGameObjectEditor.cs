using Netherlands3D.Twin.Layers;
using Netherlands3D.Twin.Layers.LayerTypes.GeoJsonLayers;
using UnityEditor;
using UnityEditor.UIElements;
using UnityEngine.UIElements;

namespace Netherlands3D.Twin.Editor.Layers
{
    [CustomEditor(typeof(GeoJSONLineLayer))]
    public class GeoJsonLineLayerGameObjectEditor : UnityEditor.Editor
    {
        public override VisualElement CreateInspectorGUI()
        {
            var root = new VisualElement();

            InspectorElement.FillDefaultInspector(root, serializedObject, this);

            var layerGameObject = (GeoJSONLineLayer)target;
            LayerDataVisualElements.LayerData(layerGameObject.LayerData, root);

            return root;
        }
    }
}