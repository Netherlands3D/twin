using Netherlands3D.UI.ExtensionMethods;
using UnityEngine.UIElements;

namespace Netherlands3D.UI.Components
{
    [UxmlElement]
    public partial class ListViewItem : VisualElement
    {
        private readonly VisualElement _contentContainer;

        public override VisualElement contentContainer => _contentContainer;

        public ListViewItem()
        {
            this.CloneComponentTree("Components");
            this.AddComponentStylesheet("Components");

            _contentContainer = this.Q<VisualElement>("ContentContainer");
        }

        public ListViewItem(VisualElement content) : this()
        {
            _contentContainer.Add(content);
        }
    }
}