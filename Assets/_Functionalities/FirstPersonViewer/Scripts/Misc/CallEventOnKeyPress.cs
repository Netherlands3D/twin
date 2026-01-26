using Netherlands3D.SelectionTools;
using Netherlands3D.Services;
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
                //TODO: Switch this out for a inputfield checker instead of using the one from the FPV.
                if (ServiceLocator.GetService<FirstPersonViewer>().Input.IsInputfieldSelected()) return;

                OnKeyPressed.Invoke();
            }
        }
    }
}
