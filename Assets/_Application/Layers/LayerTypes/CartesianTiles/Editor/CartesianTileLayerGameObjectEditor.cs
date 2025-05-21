using Netherlands3D.Twin.Layers.Editor;
using UnityEditor;
using UnityEditor.UIElements;
using UnityEngine.UIElements;

namespace Netherlands3D.Twin.Layers.LayerTypes.CartesianTiles.Editor
{
    [CustomEditor(typeof(CartesianTileLayerGameObject))]
    public class CartesianTileLayerGameObjectEditor : UnityEditor.Editor
    {
        public override VisualElement CreateInspectorGUI()
        {
            var root = new VisualElement();

            InspectorElement.FillDefaultInspector(root, serializedObject, this);

            var layerGameObject = (CartesianTileLayerGameObject)target;
            LayerDataVisualElements.LayerData(layerGameObject.LayerData, root);

            return root;
        }
    }
}