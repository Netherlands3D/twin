using UnityEngine;
using UnityEngine.EventSystems;

namespace Netherlands3D.Twin.Layers.UI.AddLayer
{
    public class LayerToolBarToggleAddLayer : LayerToolBarToggleBase
    {
        public override void ToggleAction(bool isOn)
        {
            if (!isOn)
                EventSystem.current.SetSelectedGameObject(null);
        }

        public override void OnDrop(PointerEventData eventData)
        {
            Debug.LogError("duplicate layer is currently not implemented");
            throw new System.NotImplementedException();
        }
    }
}
