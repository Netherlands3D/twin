using System.Collections.Generic;
using UnityEngine;
using System;
using Netherlands3D.Twin.Layers;
using Netherlands3D.Twin.Layers.LayerTypes;
using UnityEngine.Events;
using Netherlands3D.Twin.Services;

namespace Netherlands3D.Twin.Functionalities
{
    public class PrefabSpawner : MonoBehaviour
    {
        [Serializable]
        public class FunctionalityAndPrefab{
            [HideInInspector] public string name = "";
            public Functionality functionality;
            public GameObject spawnedObject;
        }

        [Header("Add/remove GameObjects based on functionality active state")]
        [SerializeField] private FunctionalityAndPrefab[] functionalityAndLayers;
        private Dictionary<FunctionalityAndPrefab, UnityAction> listenerMapping = new Dictionary<FunctionalityAndPrefab, UnityAction>();
        
        private void OnEnable() 
        {
            foreach (var functionalityAndlayer in functionalityAndLayers)
            {
                UnityAction action = () => Spawn(functionalityAndlayer.spawnedObject);
                listenerMapping[functionalityAndlayer] = action;
                functionalityAndlayer.functionality.OnEnable.AddListener(action);
            }
        }

        private void OnDisable()
        {
            foreach (var functionalityAndlayer in functionalityAndLayers)
            {
                if (listenerMapping.TryGetValue(functionalityAndlayer, out var action))
                {
                    functionalityAndlayer.functionality.OnEnable.RemoveListener(action);
                    listenerMapping.Remove(functionalityAndlayer);
                }
            }
            listenerMapping.Clear();
        }

        private void OnValidate() {
            foreach (var functionalityAndlayer in functionalityAndLayers)
            {
                //Set name if functionality and layer are set and name is not set
                if(functionalityAndlayer.functionality && functionalityAndlayer.spawnedObject){
                    functionalityAndlayer.name = functionalityAndlayer.functionality.name + " - " + functionalityAndlayer.spawnedObject.name;
                }
            }
        }

        public async void Spawn(GameObject prefab)
        {
            var layerGameObject = prefab.GetComponent<LayerGameObject>();
            if (layerGameObject)
            {
                var layerBuilder = LayerBuilder.Create()
                    .OfType(layerGameObject.PrefabIdentifier)
                    .NamedAs(prefab.name);
                    //.WhenBuilt(data => ((ReferencedLayerData)data).Reference.transform.parent = transform);
                Layer layer = await App.Layers.Add(layerBuilder);
                layer.LayerGameObject.transform.parent = transform;
                return;
            }

            var newLayer = Instantiate(prefab, transform);
            newLayer.name = prefab.name;
        }
    }
}
