using UnityEngine;
using UnityEngine.UIElements;

namespace Netherlands3D.UI.Panels
{
    [CreateAssetMenu(fileName = "DomeButtonBehaviour", menuName = "ScriptableObjects/FloatingButtonBehaviours/DomeButtonBehaviour", order = 1)]
    public class DomeButtonBehaviour : FloatingButtonBehaviour
    {
        public override void Initialize(VisualElement parent)
        {
            base.Initialize(parent);
            floatingButton.RegisterCallback<PointerDownEvent>(evt =>
            {
                Debug.Log("dome down!");
            });
            floatingButton.RegisterCallback<PointerUpEvent>(evt =>
            {
                Debug.Log("dome up!");
            });
        }

        public override VisualElement SpawnFloatingButtonContent()
        {
            Button button = new Netherlands3D.UI.Components.Button();
            button.name = "DomeButton";
            return button;
        }
    }
}
