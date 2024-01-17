using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Netherlands3D.CartesianTiles;
using Netherlands3D.Twin.UI.LayerInspector;
using UnityEngine;
using UnityEngine.UI;

namespace Netherlands3D.Twin
{
    [RequireComponent(typeof(Toggle))]
    public class StandardLayerToggle : MonoBehaviour
    {
        private CartesianTiles.TileHandler tileHandler;
        private Toggle toggle;
        [SerializeField] private Tile3DLayer layer;
        [SerializeField] private GameObject prefab;

        private void Awake()
        {
            toggle = GetComponent<Toggle>();
        }

        private void OnEnable()
        {
            tileHandler = FindAnyObjectByType<CartesianTiles.TileHandler>(FindObjectsInactive.Include);

            layer = tileHandler.layers.FirstOrDefault(l => l.name == prefab.name)?.GetComponent<Tile3DLayer>();
            toggle.isOn = layer != null;

            toggle.onValueChanged.AddListener(CreateOrDestroyObject);
        }

        private void OnDisable()
        {
            toggle.onValueChanged.RemoveListener(CreateOrDestroyObject);
        }

        private void CreateOrDestroyObject(bool isOn)
        {
            if (isOn)
                layer = CreateObject();
            else
                layer.DestroyLayer();
        }

        private Tile3DLayer CreateObject()
        {
            var newObject = Instantiate(prefab, Vector3.zero, Quaternion.identity, tileHandler.transform);
            newObject.name = prefab.name;
            tileHandler.AddLayer(newObject.GetComponent<CartesianTiles.Layer>());

            var layerComponent = newObject.GetComponent<Tile3DLayer>();
            if (!layerComponent)
                layerComponent = newObject.AddComponent<Tile3DLayer>();

            print(layerComponent);
            print(layerComponent?.ReferencedProxy);
            print(layerComponent?.ReferencedProxy?.UI);

            // layerComponent.ReferencedProxy.UI.Select();
            return layerComponent;
        }
    }
}