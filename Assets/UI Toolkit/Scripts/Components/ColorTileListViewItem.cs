using Netherlands3D.UI.ExtensionMethods;
using UnityEngine.UIElements;

namespace Netherlands3D.UI.Components
{
    [UxmlElement]
    public partial class ColorTileListViewItem : VisualElement
    {
        private ColorTile tile;
        public ColorTile Tile => tile ??= this.Q<ColorTile>();
        public bool VisibilityState { get; set; } = true;
        
        public ColorTileListViewItem()
        {
            this.CloneComponentTree("Components");
            this.AddComponentStylesheet("Components");
        }
    }
}