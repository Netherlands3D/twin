using UnityEngine;
using UnityEngine.UIElements;

namespace Netherlands3D.UI.Panels
{   
    public abstract class FloatingButtonBehaviour : ScriptableObject
    {     
        protected VisualElement content;
        protected FloatingButton floatingButton;

        public virtual void Initialize(VisualElement parent)
        {
            this.content = parent;
            FloatingButton floatingButton = new FloatingButton();
            VisualElement element = SpawnFloatingButtonContent();
            floatingButton.Add(element);
            content.Add(floatingButton);
        }

        public virtual void Dispose()
        {
            content.Remove(floatingButton);
            content = null;
        }

        public abstract VisualElement SpawnFloatingButtonContent();
    }
}
