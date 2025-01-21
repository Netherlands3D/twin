using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace Netherlands3D.Twin.Layers.UI.AddLayer
{
    public class DisableToolsWhenDisabled : MonoBehaviour
    {
        [SerializeField] private Toggle[] toggles;

        private void OnDisable()
        {
            foreach (var toggle in toggles)
            {
                toggle.isOn = false;
            }
        }
    }
}
