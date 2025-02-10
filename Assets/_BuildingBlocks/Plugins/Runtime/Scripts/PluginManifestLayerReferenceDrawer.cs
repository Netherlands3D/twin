using System.Collections.Generic;
using UnityEditor;
using UnityEditor.UIElements;
using UnityEngine;
using UnityEngine.UIElements;

namespace Netherlands3D.Plugins
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
                if (target != null)
                {
                    if (fieldInfo.GetValue(target) is List<PluginManifestLayerReference> layerRefs)
                    {
                        foreach (var layerRef in layerRefs)
                        {
                            layerRef.UpdateFields();
                        }
                    }
                }
                property.serializedObject.ApplyModifiedProperties();
            });

            return container;
        }
    }
}