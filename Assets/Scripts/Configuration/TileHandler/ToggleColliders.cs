using System.Collections;
using System.Collections.Generic;
using netDxf.Tables;
using Netherlands3D.CartesianTiles;
using UnityEngine;

namespace Netherlands3D.Twin
{
    public class ToggleColliders : MonoBehaviour
    {
        [SerializeField] Tool toolThatEnablesColliders;

        private BinaryMeshLayer binaryMeshLayer;

        private void Awake() {
            binaryMeshLayer = GetComponent<BinaryMeshLayer>();

            toolThatEnablesColliders.onActivate.AddListener(EnableColliders);
            toolThatEnablesColliders.onDeactivate.AddListener(DisableColliders);    

            if(toolThatEnablesColliders.Open) {
                EnableColliders();
            }        
        }

        private void EnableColliders() {
            binaryMeshLayer.CreateMeshColliders = true;

            //Add a mesh colliders for all renderers that already have spawned
            var meshFilters = GetComponentsInChildren<MeshFilter>();
            foreach (var meshFilter in meshFilters) {
                if(meshFilter.gameObject.GetComponent<MeshCollider>() != null) continue;

                var meshCollider = meshFilter.gameObject.AddComponent<MeshCollider>();
                meshCollider.sharedMesh = meshFilter.sharedMesh;
            }
        }

        private void DisableColliders() {
            binaryMeshLayer.CreateMeshColliders = false;

            //Destroy all mesh colliders
            var meshColliders = GetComponentsInChildren<MeshCollider>();
            foreach (var meshCollider in meshColliders) {
                Destroy(meshCollider);
            }
        }
    }
}
