using GLTFast;
using Netherlands3D.Coordinates;

//using Netherlands3D.Core;

using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.Events;


namespace Netherlands3D.Tiles3D
{
    [System.Serializable]
    public class Content : MonoBehaviour, IDisposable
    {
        public string uri = "";

#if SUBOBJECT
        public bool parseSubObjects = true;
#endif

        private Coroutine runningContentRequest;

        [SerializeField] private Tile parentTile;
        public Tile ParentTile { get => parentTile; set => parentTile = value; }

        public UnityEvent onDoneDownloading = new();

        private Material overrideMaterial;

        private GltfImport gltf;

        public enum ContentLoadState
        {
            NOTLOADING,
            DOWNLOADING,
            DOWNLOADED,
            PARSING,
        }
        private ContentLoadState state = ContentLoadState.NOTLOADING;
        public ContentLoadState State
        {
            get => state;
            set
            {
                state = value;
            }
        }
#if UNITY_EDITOR
        /// <summary>
        /// Draw wire cube in editor with bounds and color coded state
        /// </summary>
        private void OnDrawGizmosSelected()
        {
            if (ParentTile == null) return;

            Color color = Color.white;
            switch (State)
            {
                case ContentLoadState.NOTLOADING:
                    color = Color.red;
                    break;
                case ContentLoadState.DOWNLOADING:
                    color = Color.yellow;
                    break;
                case ContentLoadState.DOWNLOADED:
                    color = Color.green;
                    break;
                default:
                    break;
            }

            Gizmos.color = color;
            var parentTileBounds = ParentTile.ContentBounds;
            Gizmos.DrawWireCube(parentTileBounds.center, parentTileBounds.size);

            Gizmos.color = Color.blue;
            Gizmos.DrawLine(parentTileBounds.center, parentTileBounds.center + (ParentTile.priority * Vector3.up));
        }
#endif

        /// <summary>
        /// Load the content from an url
        /// </summary>
        public void Load(Material overrideMaterial = null, Dictionary<string, string> headers = null)
        {
            if(overrideMaterial != null)
            {
                this.overrideMaterial = overrideMaterial;
            }

            if (State == ContentLoadState.DOWNLOADING || State == ContentLoadState.DOWNLOADED)
                return;

            State = ContentLoadState.DOWNLOADING;
            parentTile.isLoading = true;

            runningContentRequest = StartCoroutine(
                ImportB3DMGltf.ImportBinFromURL(
                    uri, 
                    GotGltfContent,
                    false,
                    headers
                )
            );
        }

        /// <summary>
        /// After parsing gltf content spawn gltf scenes
        /// </summary>
        private async void GotGltfContent(ParsedGltf parsedGltf)
        {
            if (State != ContentLoadState.DOWNLOADING)
            {
                State = ContentLoadState.DOWNLOADED;
                return;
            }
            State = ContentLoadState.PARSING;
            parentTile.isLoading = false;
            if (parsedGltf == null)
            {
                Debug.Log("failed to parse: "+ uri +" , trying again");
                State = ContentLoadState.DOWNLOADED;
                return;
            }

            var gltf = parsedGltf.gltfImport;
            var scene = transform;
            if (gltf != null)
            {
                this.gltf = gltf;
                var scenes = gltf.SceneCount;

                for (int i = 0; i < scenes; i++)
                {
                    await gltf.InstantiateSceneAsync(transform, i);
                    scene = transform.GetChild(i).transform;
                    foreach (var child in scene.GetComponentsInChildren<Transform>(true)) //getting the Transform components ensures the layer of each recursive child is set 
                    {
                        child.gameObject.layer = gameObject.layer;
                    }
                    
                    if(scene == null) continue;

                   // MovingOriginFollower sceneOriginFollower = scene.gameObject.AddComponent<MovingOriginFollower>();
                    if (parsedGltf.rtcCenter != null)
                    {
                        scene.rotation = CoordinateConverter.ecefRotionToUp() * (scene.rotation);
                        Vector3 unityPosition = CoordinateConverter.ECEFToUnity(new Vector3ECEF(parsedGltf.rtcCenter[0] + parentTile.transform[12], parsedGltf.rtcCenter[1] + parentTile.transform[13], parsedGltf.rtcCenter[2] + parentTile.transform[14]));
                        scene.position = unityPosition;
                    }
                    else
                    {
                        Vector3 unityPosition = CoordinateConverter.ECEFToUnity(new Vector3ECEF(-scene.localPosition.x + parentTile.transform[12], -scene.localPosition.z + parentTile.transform[13], scene.localPosition.y + parentTile.transform[14]));
                        scene.rotation = CoordinateConverter.ecefRotionToUp() * (scene.rotation);
                        scene.position = unityPosition;
                    }
                }

                this.gameObject.name = uri;
                
                //Check if mesh features addon is used to define subobjects

#if SUBOBJECT
                if(parseSubObjects){
                    parsedGltf.ParseSubObjects(transform);
                }
#endif

                if(overrideMaterial != null)
                {
                    OverrideAllMaterials(transform);
                }
            }

            State = ContentLoadState.DOWNLOADED;
            onDoneDownloading.Invoke();
        }

        private void OverrideAllMaterials(Transform parent)
        {
            foreach (var renderer in parent.GetComponentsInChildren<Renderer>())
            {
                renderer.material = overrideMaterial;
            }
        }

        /// <summary>
        /// Clean up coroutines and content gameobjects
        /// </summary>
        public void Dispose()
        {
            onDoneDownloading.RemoveAllListeners();

            if (State == ContentLoadState.PARSING)
            {
                onDoneDownloading.AddListener(Dispose);
                return;
            }

            //Direct abort of downloads
            if (State == ContentLoadState.DOWNLOADING && runningContentRequest != null)
            {
                StopCoroutine(runningContentRequest);       
            }
           
            State = ContentLoadState.DOWNLOADED;

            if (gltf != null)
            {
                gltf.Dispose();     
            }
            Destroy(this.gameObject);
        }
    }
}
