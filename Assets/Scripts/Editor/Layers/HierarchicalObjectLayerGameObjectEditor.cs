using Netherlands3D.Twin.Layers.LayerTypes;
using UnityEditor;
using UnityEditor.UIElements;
using UnityEngine.UIElements;

namespace Netherlands3D.Twin.Editor.Layers
{
    [CustomEditor(typeof(HierarchicalObjectLayerGameObject))]
    public class HierarchicalObjectLayerGameObjectEditor : UnityEditor.Editor
    {
        public override VisualElement CreateInspectorGUI()
        {
            VisualElement root = new VisualElement();

            InspectorElement.FillDefaultInspector(root, serializedObject, this);

            var layerGameObject = (HierarchicalObjectLayerGameObject)target;
            LayerDataVisualElements.LayerData(layerGameObject.LayerData, root);

            return root;
        }
    }
}