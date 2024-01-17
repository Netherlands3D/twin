using System;
using System.Collections;
using System.Collections.Generic;
using Netherlands3D.Twin.UI.LayerInspector;
using UnityEngine;
using UnityEngine.UI;

namespace Netherlands3D.Twin
{
    [RequireComponent(typeof(Button))]
    public class StandardLayerButton : MonoBehaviour
    {
        private Button button;
        [SerializeField] private GameObject prefab;

        private void Awake()
        {
            button = GetComponent<Button>();
        }

        private void OnEnable()
        {
            button.onClick.AddListener(CreateObject);
        }

        private void OnDisable()
        {
            button.onClick.RemoveListener(CreateObject);
        }

        private void CreateObject()
        {
            var tileHandler = FindAnyObjectByType<CartesianTiles.TileHandler>();
            
            var newObject = Instantiate(prefab, Vector3.zero, Quaternion.identity, tileHandler.transform);
            newObject.name = prefab.name;
            tileHandler.AddLayer(newObject.GetComponent<CartesianTiles.Layer>());
            
            var layerComponent = newObject.GetComponent<Tile3DLayer>();
            if (!layerComponent)
                layerComponent = newObject.AddComponent<Tile3DLayer>();
            
            layerComponent.ReferencedProxy.UI.Select();
        }
    }
}
