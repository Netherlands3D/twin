using Netherlands3D.LayerStyles;
using Netherlands3D.Twin.Layers;
using UnityEngine;
using UnityEngine.UIElements;

namespace Netherlands3D.Twin.Editor.Layers
{
    public static class LayerDataVisualElements
    {
        public static void LayerData(LayerData layerData, VisualElement root)
        {
            // Add a divider first
            root.Add(Divider(2, 8));
            root.Add(Heading("Layer Data"));
            root.Add(Divider());

            if (layerData == null)
            {
                root.Add(FieldContent("This layer doesn't have any data associated with it (yet)."));
                return; 
            }

            root.Add(FieldWithCaption("Name", layerData.Name));
            root.Add(FieldWithCaption("Color", ColorUtility.ToHtmlStringRGBA(layerData.Color)));

            root.Add(FieldCaption("Styles"));
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
            var group = GroupWithBorder();
            group.Add(FieldWithCaption("Name", layerStyle.Metadata.Name));
            group.Add(FieldCaption("Rules"));
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
            ruleFoldout.Add(FieldWithCaption("Selector", "If " + stylingRule.Selector));
            var styles = stylingRule.Symbolizer.ToString();
            ruleFoldout.Add(FieldWithCaption("Styles", string.IsNullOrEmpty(styles) ? "[None]" : styles ));

            return ruleFoldout;
        }

        private static VisualElement FieldWithCaption(string caption, string content)
        {
            var group = new VisualElement()
            {
                style =
                {
                    marginBottom = 4
                }
            };
            group.Add(FieldCaption(caption));
            group.Add(FieldContent(content));

            return group;
        }

        private static Label FieldCaption(string caption)
        {
            return new Label(caption) { style = { unityFontStyleAndWeight = FontStyle.Bold}};
        }

        private static Label FieldContent(string content)
        {
            if (string.IsNullOrEmpty(content))
            {
                content = "[None]";
            }
            return new Label(content);
        }

        private static VisualElement GroupWithBorder()
        {
            return new VisualElement
            {
                style =
                {
                    borderTopWidth = 1,
                    borderRightWidth = 1,
                    borderBottomWidth = 1,
                    borderLeftWidth = 1,
                    borderBottomColor = new Color(0.7f, 0.7f, 0.7f), // Light gray color
                    borderTopColor = new Color(0.7f, 0.7f, 0.7f), // Light gray color
                    borderLeftColor = new Color(0.7f, 0.7f, 0.7f), // Light gray color
                    borderRightColor = new Color(0.7f, 0.7f, 0.7f), // Light gray color
                    paddingLeft = 8,
                    paddingRight = 8,
                    paddingTop = 4,
                    paddingBottom = 4,
                    marginTop = 4,
                    marginBottom = 4
                }
            };
        }
        
        private static VisualElement Divider(int thickness = 1, int marginTop = 4, int marginBottom = 4)
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