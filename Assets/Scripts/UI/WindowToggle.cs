using System;
using System.Collections;
using System.Collections.Generic;
using Netherlands3D.Twin.Features;
using UnityEngine;
using UnityEngine.Events;

namespace Netherlands3D.Twin.Interface
{
    public class WindowToggle : MonoBehaviour
    {
        [Tooltip("A scriptable object with an IWindow interface")]
        public ScriptableObject windowData;

        public UnityEvent OnWindowToggleOn;
        public UnityEvent InWindowToggleOff;
        
        public IWindow window;

        private void CheckData(){
            if(!windowData) return;

            if (windowData is IWindow windowInterface)
            {
                window = windowInterface;
                if (window.IsOpen)
                {
                    OnWindowOpen();
                }
                else
                {
                    OnWindowClose();
                }
            }
            else
            {
                windowData = null;
                Debug.LogError("windowData does not contain a window interface.");
            }
        }

        private void OnValidate() {
            CheckData();
        }

        private void OnEnable() {
            CheckData();

            if(window == null){
                Debug.LogError("No window set for WindowToggle");
            }

            window.OnOpen.AddListener(OnWindowOpen);
            window.OnClose.AddListener(OnWindowClose);
        }

        private void OnDisable() {
            window.OnOpen.RemoveListener(OnWindowOpen);
            window.OnClose.RemoveListener(OnWindowClose);
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
