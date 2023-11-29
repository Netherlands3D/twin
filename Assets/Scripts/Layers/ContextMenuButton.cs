using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace Netherlands3D.Twin
{
    public abstract class ContextMenuButton : MonoBehaviour
    {
        private Button button;

        protected virtual void Awake()
        {
            button = GetComponent<Button>();
        }

        private void OnEnable()
        {
            button.onClick.AddListener(OnButtonClick);
        }
        
        private void OnDisable()
        {
            button.onClick.RemoveListener(OnButtonClick);
        }

        public abstract void OnButtonClick();
    }
}
