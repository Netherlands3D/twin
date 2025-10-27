using Netherlands3D.UI.ExtensionMethods;
using UnityEngine.UIElements;

namespace Netherlands3D.UI.Components
{
    [UxmlElement]
    public partial class ListViewItem : VisualElement
    {
        public ListViewItem()
        {
            this.CloneComponentTree("Components");
            this.AddComponentStylesheet("Components");
        }

        public ListViewItem(VisualElement content) : this()
        {
            Add(content);
        }
    }
}