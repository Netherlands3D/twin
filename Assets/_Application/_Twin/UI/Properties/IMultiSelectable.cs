using System.Collections.Generic;

namespace Netherlands3D.Twin.UI
{
    public interface IMultiSelectable
    {
        public PanelSelectionService MultiSelection { get; }
        public int SelectedButtonIndex { get; set; }
        public List<ISelectable> SelectedItems { get; }
        public List<ISelectable> Items { get; set; }
        public ISelectable FirstSelectedItem { get; set; }
    }
}
