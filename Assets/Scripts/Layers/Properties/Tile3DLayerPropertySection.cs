using System.Collections;
using System.Collections.Generic;
using Netherlands3D.Twin.Layers;
using Netherlands3D.Twin.Layers.Properties;
using Netherlands3D.Twin.UI.LayerInspector;
using TMPro;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

namespace Netherlands3D.Twin
{
    public class Tile3DLayerPropertySection : MonoBehaviour
    {
        [SerializeField] private TMP_InputField urlInputField;

        private Tile3DLayer2 layer;
        public Tile3DLayer2 Layer
        {
            get => layer;
            set
            {
                layer = value;
                urlInputField.text = layer.URL;
            }
        }

        private void OnEnable()
        {
            urlInputField.onValueChanged.AddListener(HandleURLChange);
        }
        
        private void OnDisable()
        {
            urlInputField.onValueChanged.RemoveListener(HandleURLChange);
        }

        private void HandleURLChange(string newValue)
        {
            layer.URL = newValue;
        }
    }
}
