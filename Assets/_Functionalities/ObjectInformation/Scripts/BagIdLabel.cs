using System;
using TMPro;
using UnityEngine;

namespace Netherlands3D
{
    public class BagIdLabel : MonoBehaviour
    {
        public string Text => bagIdText.text;
        
        [SerializeField] private TextMeshProUGUI bagIdText;
        
        public void SetText(string text)
        {
            bagIdText.text = text;
        }
    }
}
