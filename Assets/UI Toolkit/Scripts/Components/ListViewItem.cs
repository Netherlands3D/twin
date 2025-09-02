using UnityEngine;
using UnityEngine.UIElements;

namespace Netherlands3D.UI.Components
{
    [UxmlElement]
    public partial class ListViewItem : VisualElement
    {
        public ListViewItem()
        {
            // Find and load UXML template for this component
            var asset = Resources.Load<VisualTreeAsset>("UI/" + nameof(ListViewItem));
            asset.CloneTree(this);

            // Find and load USS stylesheet specific for this component
            var styleSheet = Resources.Load<StyleSheet>("UI/" + nameof(ListViewItem) + "-style");
            styleSheets.Add(styleSheet);

            // Stable hooks for selectors
            AddToClassList("listview-item");
            if (string.IsNullOrEmpty(name)) name = nameof(ListViewItem);
        }
    }
}