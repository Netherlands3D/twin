using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;
using Netherlands3D.Twin.Functionalities;

namespace Netherlands3D.Twin
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

        private void Awake() {
            return;
            foreach (var functionalityAndlayer in functionalityAndLayers)
            {
                functionalityAndlayer.functionality.OnEnable.AddListener(() => Spawn(functionalityAndlayer.spawnedObject));

                //Start with default spawns
                if(functionalityAndlayer.functionality.IsEnabled){
                    Spawn(functionalityAndlayer.spawnedObject);
                }
            }
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

        public void Spawn(GameObject layer)
        {
            var newLayer = Instantiate(layer, transform);
            newLayer.name = layer.name;
        }
    }
}
