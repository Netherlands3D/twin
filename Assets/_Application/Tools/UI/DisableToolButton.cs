using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace Netherlands3D.Twin.Tools
{
    public class DisableToolButton : MonoBehaviour
    {
        [SerializeField] private Tool tool;
        private Button button;

        private void Awake()
        {
            button = GetComponent<Button>();
        }

        private void OnEnable()
        {
            tool.onOpen.AddListener(DisableButton);
            tool.onClose.AddListener(EnableButton);
        }
        
        private void OnDisable()
        {
            tool.onOpen.RemoveListener(DisableButton);
            tool.onClose.RemoveListener(EnableButton);
        }

        private void EnableButton()
        {
            button.interactable = true;
        }

        private void DisableButton()
        {
            button.interactable = false;
        }
    }
}
