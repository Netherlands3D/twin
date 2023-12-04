using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;

namespace Netherlands3D.Twin
{
    /// <summary>
    /// The default TMP_Dropdown puts the 'Blocker' canvas item on the 'Default' layer, instead of the same layer as the dropdown itself. 
    /// This script fixes that by setting the layer of the 'Blocking' canvas item to the same layer as the dropdown.
    /// This script can be removed after Unity has fixed the issue.
    /// </summary>
    public class SetBlockerLayer : MonoBehaviour
    {
        private Canvas canvas;

        private void Awake() {
            canvas = GetComponent<Canvas>();
        }

        private void OnTransformChildrenChanged() {
            //Force 'blocker' objects to UI layer
            var blocker = canvas.transform.Find("Blocker");
            if(blocker)
            {
                Debug.Log("Moved 'Blocker' to canvas layer: " + canvas.gameObject.layer, blocker);
                blocker.gameObject.layer = canvas.gameObject.layer;
            }
        }
    }
}
