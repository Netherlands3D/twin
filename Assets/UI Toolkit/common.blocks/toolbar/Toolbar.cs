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

        private void Start()
        {
            var rootVisualElement = GetComponent<UIDocument>().rootVisualElement;
            rootVisualElement
                .Q<VisualElement>(TOOLBAR_BUTTON_SELECT_ID)
                .RegisterCallback<ClickEvent>(evt => onClickedSelectButton.Invoke());
            rootVisualElement
                .Q<VisualElement>(TOOLBAR_BUTTON_LAYERS_ID)
                .RegisterCallback<ClickEvent>(evt => onClickedLayersButton.Invoke());
        }
    }
}
