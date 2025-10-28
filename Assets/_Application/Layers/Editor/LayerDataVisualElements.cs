using Netherlands3D.LayerStyles;
using Newtonsoft.Json;
using UnityEditor.UIElements;
using UnityEngine;
using UnityEngine.UIElements;

namespace Netherlands3D.Twin.Layers.Editor
{
    public static class LayerDataVisualElements
    {
        private const float LabelWidth = 150f; // match Unity’s inspector feel

        public static void LayerData(LayerData layerData, VisualElement root)
        {
            // Add a divider first
            root.Add(Heading("Layer Data"));
            root.Add(Divider());

            if (layerData == null)
            {
                root.Add(FieldContent("This layer doesn't have any data associated with it (yet)."));
                return; 
            }

            root.Add(FieldWithCaption("Id", layerData.Id.ToString()));
            root.Add(FieldWithCaption("Name", layerData.Name));
            root.Add(FieldWithColor("Color", layerData.Color));

            root.Add(Subheading("Styles"));
            if (layerData.Styles.Count == 0)
            {
                root.Add(FieldContent("This layer doesn't have any styles associated with it (yet)."));
            }

            bool first = true;
            foreach (var (_, style) in layerData.Styles)
            {
                var foldout = StyleFoldout(style);
                // first should be folded out, rest collapsed by default
                foldout.value = first;
                if (first) first = false;

                root.Add(foldout);
            }
        }

        private static Foldout StyleFoldout(LayerStyle layerStyle)
        {
            var foldout = new Foldout { text = layerStyle.Metadata.Name };
            var group = new VisualElement();
            group.Add(FieldWithCaption("Name", layerStyle.Metadata.Name));
            group.Add(Subheading("Rules"));
            if (layerStyle.StylingRules.Count == 0)
            {
                group.Add(FieldContent("This style doesn't have any styling rules associated with it (yet)."));
            }
            foreach (var (_, stylingRule) in layerStyle.StylingRules)
            {
                group.Add(StyleRuleFoldout(stylingRule));
            }
            foldout.Add(group);

            return foldout;
        }

        private static Foldout StyleRuleFoldout(StylingRule stylingRule)
        {
            var ruleFoldout = new Foldout { text = stylingRule.Name };
            ruleFoldout.Add(FieldWithCaption("Name", stylingRule.Name));
            ruleFoldout.Add(FieldWithCaption("Selector", "If " + JsonConvert.SerializeObject(stylingRule.Selector)));
            var styles = stylingRule.Symbolizer.ToString();
            ruleFoldout.Add(FieldWithCaption("Styles", string.IsNullOrEmpty(styles) ? "[None]" : styles ));

            return ruleFoldout;
        }

        private static VisualElement FieldWithCaption(string caption, string content)
        {
            var tf = new TextField(caption)
            {
                value = string.IsNullOrEmpty(content) ? "[None]" : content,
                isReadOnly = true,
                enabledSelf = false
            };
            tf.style.flexGrow = 1;
            tf.labelElement.style.minWidth = LabelWidth;
            tf.labelElement.style.maxWidth = LabelWidth;   // keep columns aligned
            tf.labelElement.style.unityTextAlign = TextAnchor.MiddleLeft;
            return tf;
        }
        
        private static VisualElement FieldWithColor(string caption, Color color)
        {
            var row = new VisualElement
            {
                style = { flexDirection = FlexDirection.Row, alignItems = Align.Center, marginBottom = 4 }
            };

            var colorField = new ColorField(caption) { value = color, showAlpha = true };
            colorField.SetEnabled(false); // read-only, but keeps native look
            colorField.style.flexGrow = 1;
            colorField.labelElement.style.minWidth = LabelWidth;
            colorField.labelElement.style.maxWidth = LabelWidth;
            colorField.labelElement.style.unityTextAlign = TextAnchor.MiddleLeft;

            var hex = new Label("#" + ColorUtility.ToHtmlStringRGBA(color))
            {
                style = { marginLeft = 6 }
            };

            row.Add(colorField);
            row.Add(hex); // optional extra readout
            return row;
        }

        private static Label Subheading(string caption)
        {
            return new Label(caption) { style =
            {
                unityFontStyleAndWeight = FontStyle.Bold,
                marginTop = 6,
                marginLeft = 3
            }};
        }

        private static Label FieldContent(string content)
        {
            if (string.IsNullOrEmpty(content))
            {
                content = "[None]";
            }
            return new Label(content);
        }

        public static VisualElement Divider(int thickness = 1, int marginTop = 4, int marginBottom = 4)
        {
            return new VisualElement
            {
                style =
                {
                    height = thickness,
                    marginTop = marginTop,
                    marginBottom = marginBottom,
                    backgroundColor = new Color(0.7f, 0.7f, 0.7f)
                }
            };
        }

        private static VisualElement Heading(string caption)
        {
            return new Label(caption)
            {
                style =
                {
                    unityFontStyleAndWeight = FontStyle.Bold,
                    fontSize = 14,
                    marginBottom = 4
                }
            };
        }
    }
}