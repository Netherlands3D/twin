using System;
using System.Collections;
using System.Collections.Generic;
using Netherlands3D.Twin.Features;
using UnityEngine;
using UnityEngine.Events;

namespace Netherlands3D.Twin
{
    public class WindowToggle : MonoBehaviour
    {
        [SerializeField] bool isOn = false;

        public UnityEvent OnWindowToggleOn;
        public UnityEvent InWindowToggleOff;
        
        public IWindow window;

        private void OnEnable() {
            window.OnOpen.AddListener(OnWindowClose);
            window.OnClose.AddListener(OnWindowOpen);
        }

        private void OnDisable() {
            window.OnOpen.RemoveListener(OnWindowClose);
            window.OnClose.RemoveListener(OnWindowOpen);
        }

        private void OnWindowOpen()
        {
            OnWindowToggleOn.Invoke();
        }

        private void OnWindowClose()
        {
            InWindowToggleOff.Invoke();
        }

        public void Toggle()
        {
           window.IsOpen = !window.IsOpen;
        }
    }
}
