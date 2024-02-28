using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace Netherlands3D.Twin
{
    public class SetToggleInitialStateToPolygonInputActive : MonoBehaviour
    {
        private Toggle toggle;
        
        private void Awake()
        {
            toggle = GetComponent<Toggle>();
        }

        private void OnEnable()
        {
            toggle.isOn = PolygonInputToLayer.Instance.PolygonInput.gameObject.activeInHierarchy;
        }
    }
}
