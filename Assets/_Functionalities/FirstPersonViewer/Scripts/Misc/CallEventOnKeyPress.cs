using Netherlands3D.SelectionTools;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.InputSystem;

namespace Netherlands3D.FirstPersonViewer.Miscellaneous
{
    public class CallEventOnKeyPress : MonoBehaviour
    {
        [SerializeField] private InputActionReference keyEvent;

        [SerializeField] private UnityEvent OnKeyPressed = new();

        private void Update()
        {
            if (keyEvent.action.triggered)
            {
                if (Interface.CurrentInputModuleIsInputSystemUIInputModule()) return;
                OnKeyPressed.Invoke();
            }
        }
    }
}
