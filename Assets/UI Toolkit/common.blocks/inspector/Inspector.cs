using UnityEngine;
using UnityEngine.UIElements;

namespace Netherlands3D.Twin.UI
{
    [RequireComponent(typeof(UIDocument))]
    public class Inspector : MonoBehaviour
    {
        private const string INSPECTOR_ID = "inspector";
        private const string TOOLBAR_ID = "toolbar";

        private bool opening = false;
        private bool closing = false;
        
        private VisualElement rootVisualElement;
        private VisualElement inspector;
        private VisualElement toolbar;

        private void OnEnable()
        {
            rootVisualElement = GetComponent<UIDocument>().rootVisualElement;
            inspector = rootVisualElement.Q<VisualElement>(INSPECTOR_ID);
            toolbar = rootVisualElement.Q<VisualElement>(TOOLBAR_ID);
            
            // Our animation is a two-step process, thus:

            // When opening, the toolbar needs to slide in and after that the sidebar .. 
            toolbar.RegisterCallback<TransitionEndEvent>(BeginOpeningOfInspector);

            // .. but when closing the sidebar needs to slide in first and after that the toolbar 
            inspector.RegisterCallback<TransitionEndEvent>(BeginClosingOfToolbar);
        }

        private void BeginOpeningOfInspector(TransitionEndEvent evt)
        {
            if (!opening) return;

            inspector.AddToClassList("inspector--open");
            opening = false;
        }

        private void BeginClosingOfToolbar(TransitionEndEvent evt)
        {
            if (!closing) return;

            toolbar.RemoveFromClassList("toolbar--open");
            closing = false;
        }

        public void Open()
        {
            // Open toolbar first
            opening = true;
            toolbar.AddToClassList("toolbar--open");
        }

        public void Close()
        {
            // Close sidebar as the first element
            closing = true;
            inspector.RemoveFromClassList("inspector--open");
        }
    }
}
