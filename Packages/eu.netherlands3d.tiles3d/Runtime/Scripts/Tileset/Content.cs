using GLTFast;
using Netherlands3D.Coordinates;
using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;


namespace Netherlands3D.Tiles3D
{
    [System.Serializable]
    public class Content : MonoBehaviour, IDisposable
    {
        public string uri = "";
        public Coordinate contentCoordinate;
        public CoordinateSystem contentcoordinateSystem;

#if SUBOBJECT
        public bool parseSubObjects = true;
#endif

        public bool parseAssetMetaData = false;

        private Coroutine runningContentRequest;
        public Read3DTileset tilesetReader;
        [SerializeField] private Tile parentTile;
        public Tile ParentTile { get => parentTile; set => parentTile = value; }

        public UnityEvent onDoneDownloading = new();
        public UnityEvent<Content> onTileLoadCompleted = new();

        private UnityEngine.Material overrideMaterial;

        public GltfImport gltf;
        Dictionary<string, string> headers = null;
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
        public void Load(UnityEngine.Material overrideMaterial = null, Dictionary<string, string> headers = null, bool verbose = false)
        {
            this.headers = headers;
            if (overrideMaterial != null)
            {
                this.overrideMaterial = overrideMaterial;
            }

            if (State == ContentLoadState.DOWNLOADING || State == ContentLoadState.DOWNLOADED)
                return;

            State = ContentLoadState.DOWNLOADING;
            parentTile.isLoading = true;
            TIleContentLoader.debugLog = verbose;
            runningContentRequest = StartCoroutine(
           TIleContentLoader.DownloadContent(
               uri,
               transform,
               ParentTile,
               DownloadedData,
               parseAssetMetaData,
               parseSubObjects,
               overrideMaterial,
               false,
               headers

               )
           );
            return;
           
        }

        private void DownloadedData(byte[] data,string uri)
        {
            if (data == null)
            {
                FinishedLoading(false);
                return;
            }
            TIleContentLoader.LoadContent(
                data,
                uri,
                transform,
                ParentTile,
                FinishedLoading,
                parseAssetMetaData,
                parseSubObjects,
                overrideMaterial, 
                false,
                headers
                );
        }

        /// <summary>
        /// After parsing gltf content spawn gltf scenes
        /// </summary>
        /// 
        private void FinishedLoading(bool succes)
        {
            State = ContentLoadState.DOWNLOADED;
            onDoneDownloading.Invoke();
            onTileLoadCompleted.Invoke(this);
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

            if (overrideMaterial == null)
            {
                Renderer[] meshrenderers = this.gameObject.GetComponentsInChildren<Renderer>();
                ClearRenderers(meshrenderers);
            }
            MeshFilter[] meshFilters = this.gameObject.GetComponentsInChildren<MeshFilter>();
            foreach (var meshFilter in meshFilters)
            {
                //the order of destroying sharedmesh before mesh matters for cleaning up native shells
                if (meshFilter.sharedMesh != null)
                {
                    UnityEngine.Mesh mesh = meshFilter.sharedMesh;
                    meshFilter.sharedMesh.Clear();
                    Destroy(mesh);
                    meshFilter.sharedMesh = null;
                }
                if (meshFilter.mesh != null)
                {
                    UnityEngine.Mesh mesh = meshFilter.mesh;
                    meshFilter.mesh.Clear();
                    Destroy(mesh);
                    meshFilter.mesh = null;
                }
            }

            Destroy(this.gameObject);            
        }

        //todo we need to come up with a way to get all used texture slot property names from the gltf package
        private void ClearRenderers(Renderer[] renderers)
        {
            foreach (Renderer r in renderers)
            {
                Material mat = r.sharedMaterial;
                if (mat == null) continue;

                int mainTexNameID = NL3DShaders.MainTextureShaderProperty;

                if (mat.HasProperty(mainTexNameID))
                {
                    Texture tex = mat.GetTexture(mainTexNameID);

                    if (tex != null)
                    {
                        mat.SetTexture(mainTexNameID, null);
                        UnityEngine.Object.Destroy(tex);
                        tex = null;
                    }
                }
                UnityEngine.Object.Destroy(mat);
                r.sharedMaterial = null;
            }
        }
    }
}
