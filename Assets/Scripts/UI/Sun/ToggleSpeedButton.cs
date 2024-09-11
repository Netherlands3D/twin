using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace Netherlands3D.Twin
{
    public class ToggleSpeedButton : MonoBehaviour
    {
        [SerializeField] int activeAtValue;
        private Button button;

        private void Awake()
        {
            button = GetComponent<Button>();
        }

        public void SetInteractable(int dropdownValue)
        {
            button.interactable = dropdownValue == activeAtValue;
        }
    }
}
