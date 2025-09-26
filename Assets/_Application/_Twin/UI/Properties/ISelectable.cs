using UnityEngine;

namespace Netherlands3D
{
    public interface ISelectable 
    {
        public bool IsSelected { get; }
        void SetSelected(bool isSelected);
    }
}
