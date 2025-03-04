using Netherlands3D.Twin.Layers.Editor;
using UnityEditor;
using UnityEditor.UIElements;
using UnityEngine;
using UnityEngine.UIElements;

namespace Netherlands3D.Twin.Layers.LayerTypes.HierarchicalObject.Editor
{
    [CustomEditor(typeof(HierarchicalObjectLayerGameObject))]
    public class HierarchicalObjectLayerGameObjectEditor : UnityEditor.Editor
    {
        public override VisualElement CreateInspectorGUI()
        {
            VisualElement root = new VisualElement();

            InspectorElement.FillDefaultInspector(root, serializedObject, this);

            if (Application.IsPlaying(this))
            {
                var layerGameObject = (HierarchicalObjectLayerGameObject)target;
                LayerDataVisualElements.LayerData(layerGameObject.LayerData, root);
            }

            return root;
        }
    }
}