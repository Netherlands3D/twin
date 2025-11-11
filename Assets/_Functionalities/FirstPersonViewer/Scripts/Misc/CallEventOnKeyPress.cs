using UnityEngine;
using UnityEngine.Events;
using UnityEngine.InputSystem;

namespace Netherlands3D.FirstPersonViewer.Miscellaneous
{
    public class CallEventOnKeyPress : MonoBehaviour
    {
        [SerializeField] private InputActionReference keyEvent;

        [SerializeField] private UnityEvent OnKeyPressed;

        private void Update()
        {
            if (keyEvent.action.triggered) OnKeyPressed?.Invoke();
        }
    }
}
