using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;
using UnityEngine.EventSystems;
namespace Netherlands3D.Twin
{
    public class Tabbing : MonoBehaviour
    {
        void Update()
        {
            if (Keyboard.current.tabKey.wasPressedThisFrame)
            {
                var currentEventSystem = EventSystem.current;
                if(currentEventSystem.currentSelectedGameObject == null) return;
                
                var currentSelected = currentEventSystem.currentSelectedGameObject;
                if(currentSelected.TryGetComponent<Selectable>(out var selectable))
                {
                    var next = selectable.navigation.selectOnRight;
                    if (next != null)
                    {
                        currentEventSystem.SetSelectedGameObject(next.gameObject, new BaseEventData(currentEventSystem));
                    }
                }
            }
        }
    }
}