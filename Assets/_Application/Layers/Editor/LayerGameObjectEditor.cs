using UnityEditor;
using UnityEditor.UIElements;
using UnityEngine.UIElements;

namespace Netherlands3D.Twin.Layers.Editor
{
    [CustomEditor(typeof(LayerGameObject),  true)]
    public class LayerGameObjectEditor : UnityEditor.Editor
    {
        public override VisualElement CreateInspectorGUI()
        {
            var root = new VisualElement();

            var layerGameObject = target as LayerGameObject;
            if (layerGameObject) LayerDataVisualElements.LayerData(layerGameObject.LayerData, root);

            root.Add(LayerDataVisualElements.Divider(2, 8));
            
            InspectorElement.FillDefaultInspector(root, serializedObject, this);

            return root;
        }
    }
}