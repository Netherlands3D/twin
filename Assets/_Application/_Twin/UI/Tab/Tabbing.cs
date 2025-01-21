using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;
using UnityEngine.EventSystems;

namespace Netherlands3D.Twin.UI.Tabbing
{
    public class Tabbing : MonoBehaviour
    {
        void Update()
        {
            if (!Keyboard.current.tabKey.wasPressedThisFrame)
                return;

            var currentEventSystem = EventSystem.current;
            if (currentEventSystem.currentSelectedGameObject == null)
                return;

            var currentSelected = currentEventSystem.currentSelectedGameObject;
            if (!currentSelected.TryGetComponent<Selectable>(out var selectable))
                return;

            var shiftPressed = Keyboard.current.shiftKey.isPressed;
            var next = shiftPressed ? selectable.navigation.selectOnLeft : selectable.navigation.selectOnRight;
            if (next != null)
                currentEventSystem.SetSelectedGameObject(next.gameObject, new BaseEventData(currentEventSystem));
        }
    }
}