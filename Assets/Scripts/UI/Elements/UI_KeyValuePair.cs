using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;

namespace Netherlands3D.Twin
{
    public class UI_KeyValuePair : MonoBehaviour
    {
        [SerializeField] private TMP_Text keyText;
        [SerializeField] private TMP_Text valueText;
        
        public void Set(string key, string value)
        {
            keyText.text = key;
            valueText.text = value;

            gameObject.name = "KeyValuePair: " + keyText.text;
        }
    }
}
