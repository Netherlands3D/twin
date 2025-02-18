using System.Collections.Generic;
using Netherlands3D.Twin.Layers;
using UnityEditor;
using UnityEditor.UIElements;
using UnityEngine;
using UnityEngine.UIElements;

namespace Netherlands3D.Plugins.Editor
{
    [CustomPropertyDrawer(typeof(PluginManifestLayerReference))]
    public class PluginManifestLayerReferenceDrawer : PropertyDrawer
    {
        public override VisualElement CreatePropertyGUI(SerializedProperty property)
        {
            var container = new VisualElement();

            var assetProp = property.FindPropertyRelative("asset");
            var identifierProp = property.FindPropertyRelative("identifier");
            var layerNameProp = property.FindPropertyRelative("layerName");

            var assetField = new PropertyField(assetProp, "Asset Reference");
            var identifierField = new PropertyField(identifierProp, "Identifier");
            var layerNameField = new PropertyField(layerNameProp, "Layer Name");

            var helpText = new Label("Important: prefer dragging in the Asset Reference instead of using the dropdown. There is a bug in Unity when using the dropdown that you need to do it twice, and an error in your console appears");
            helpText.style.color = new StyleColor(new Color(0.8f, 0.8f, 0.8f));
            helpText.style.fontSize = 10;
            helpText.style.marginTop = 5;
            helpText.style.marginBottom = 5;
            helpText.style.paddingLeft = 3;
            helpText.style.whiteSpace = WhiteSpace.Normal;

            identifierField.SetEnabled(false);

            container.Add(assetField);
            container.Add(helpText);
            container.Add(identifierField);
            container.Add(layerNameField);

            assetField.RegisterValueChangeCallback(evt =>
            {
                var target = property.serializedObject.targetObject as PluginManifest;
                if (target != null && fieldInfo.GetValue(target) is List<PluginManifestLayerReference> layerRefs)
                {
                    foreach (var layerRef in layerRefs)
                    {
                        UpdateFields(layerRef);
                    }
                }
                property.serializedObject.ApplyModifiedProperties();
            });

            return container;
        }

        private void UpdateFields(PluginManifestLayerReference layerRef)
        {
            layerRef.identifier = "";
            if (layerRef.asset == null  || string.IsNullOrEmpty(layerRef.asset.AssetGUID)) return;
            
            string path = AssetDatabase.GUIDToAssetPath(layerRef.asset.AssetGUID);
            GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(path);

            if (prefab == null) return;
                
            LayerGameObject layerComponent = prefab.GetComponent<LayerGameObject>();
            if (layerComponent == null) return;
                
            layerRef.identifier = layerComponent.PrefabIdentifier;
            layerRef.layerName = layerComponent.name;
        }
    }
}