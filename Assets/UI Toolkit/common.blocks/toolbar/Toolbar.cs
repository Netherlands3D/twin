using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UIElements;

namespace Netherlands3D.Twin.UI
{
    public class Toolbar : MonoBehaviour
    {
        private const string TOOLBAR_BUTTON_SELECT_ID = "toolbar__button__select";
        private const string TOOLBAR_BUTTON_LAYERS_ID = "toolbar__button__layers";

        public UnityEvent onClickedSelectButton = new();
        public UnityEvent onClickedLayersButton = new();

        private void OnEnable()
        {
            var rootVisualElement = GetComponent<UIDocument>().rootVisualElement;
            rootVisualElement
                .Q<VisualElement>(TOOLBAR_BUTTON_SELECT_ID)
                .RegisterCallback<ClickEvent>(InvokeSelectButtonEvent);
            rootVisualElement
                .Q<VisualElement>(TOOLBAR_BUTTON_LAYERS_ID)
                .RegisterCallback<ClickEvent>(InvokeLayersButtonEvent);
        }

        private void InvokeSelectButtonEvent(ClickEvent evt)
        {
            onClickedSelectButton.Invoke();
        }

        private void InvokeLayersButtonEvent(ClickEvent evt)
        {
            onClickedLayersButton.Invoke();
        }
    }
}
