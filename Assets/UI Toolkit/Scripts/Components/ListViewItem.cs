using Netherlands3D.UI.ExtensionMethods;
using UnityEngine.UIElements;

namespace Netherlands3D.UI.Components
{
    [UxmlElement]
    public partial class ListViewItem : VisualElement
    {
        private VisualElement contentContainer;

        public ListViewItem()
        {
            this.CloneComponentTree("Components");
            this.AddComponentStylesheet("Components");

            contentContainer = this.Q<VisualElement>("ContentContainer");
        }

        public ListViewItem(VisualElement content) : this()
        {
            contentContainer.Add(content);
        }
    }
}