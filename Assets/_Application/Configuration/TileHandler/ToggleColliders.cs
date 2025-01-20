using System.Collections;
using Netherlands3D.CartesianTiles;
using UnityEngine;

namespace Netherlands3D.Twin.Configuration.TileHandler
{
    public class ToggleColliders : MonoBehaviour
    {
        [SerializeField] Tool toolThatEnablesColliders;

        private BinaryMeshLayer binaryMeshLayer;
        [SerializeField] private float waitForAnimationTime = 0.85f;
        private void Awake() {
            binaryMeshLayer = GetComponent<BinaryMeshLayer>();

            toolThatEnablesColliders.onOpen.AddListener(EnableColliders);
            toolThatEnablesColliders.onClose.AddListener(DisableColliders);    

            if(toolThatEnablesColliders.Open) {
                EnableColliders();
            }        
        }

        private void EnableColliders() {
            binaryMeshLayer.CreateMeshColliders = true;
            StartCoroutine(AddColliders());
        }

        private IEnumerator AddColliders() {
            yield return new WaitForSeconds(waitForAnimationTime);
            //Add a mesh colliders for all renderers that will spawn in the future
            var renderers = GetComponentsInChildren<Renderer>();
            foreach (var renderer in renderers) {
                if(!renderer) continue; //May be removed this frame

                if(renderer.gameObject.GetComponent<MeshCollider>() != null) continue;

                var meshCollider = renderer.gameObject.AddComponent<MeshCollider>();
                meshCollider.sharedMesh = renderer.GetComponent<MeshFilter>().sharedMesh;

                yield return null;
            }

            yield return null;
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
