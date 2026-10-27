using GLTFast;
using Netherlands3D.Coordinates;
using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;
using KindMen.Uxios;
using Netherlands3D.Functionalities.Wms;
using GG.Extensions;
using Netherlands3D.Twin.FloatingOrigin;

namespace Netherlands3D.WMSTiles3D
{
    [System.Serializable]
    public class Content : MonoBehaviour, IDisposable
    {
        //public string uri = "";
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

            StartCoroutine("DownloadDataAndGenerateTexture");
        }

        private System.Collections.IEnumerator DownloadDataAndGenerateTexture()
        {
            
            string wmsUrl = parentTile.tileSet.wmsLayerProperties.getMapRequestUrl(parentTile.BottomLeft,parentTile.TopRight);
            //Debug.Log(wmsUrl);
            //on loading project form save file this can be empty 
            if (string.IsNullOrEmpty(wmsUrl)) yield break;

           

            

            var promise = Uxios.DefaultInstance.Get<Texture2D>(new Uri(wmsUrl));
            promise.Then(OnDownloadedTexture);
            promise.Catch(OnFailedToDownloadTexture);
            yield return Uxios.WaitForRequest(promise);

        }

        private void OnDownloadedTexture(IResponse response)
        {
           // var payload = response.Config.GetPayload<WMSTileDataLayerChangePayload>();
           // var tileKey = payload.TileKey;
            var tex = response.Data as Texture2D;

            if (!tex)
            {
                FinishedLoading(false);  
                //onLogMessage.Invoke(LogType.Warning, $"Texture could not load for tile '{tileKey.x},{tileKey.y}'");
                return;
            }
            GameObject projectorObject = Instantiate(parentTile.tileSet.projectorPrefab);
            WorldTransform worldtransform = projectorObject.AddComponent<WorldTransform>();
            worldtransform.MoveToCoordinate(new Coordinate(CoordinateSystem.RDNAP,parentTile.boundingVolume.values[0],parentTile.boundingVolume.values[1],parentTile.boundingVolume.values[2]));
            projectorObject.transform.parent = this.transform;
            TextureDecalProjector projector = projectorObject.GetComponent<TextureDecalProjector>();
            projector.Project(tex, (float)(parentTile.TopRight.easting-parentTile.BottomLeft.easting),100,-1,110,true);
                  FinishedLoading(true);  
            //tex.name = tile.tileKey.ToString();
            //projector.Project(tex, tileSize, ProjectorHeight, renderIndex, ProjectorMinDepth, isEnabled);
        }

        private void OnFailedToDownloadTexture(Exception exception)
        {
 
        }

        private void FinishedLoading(bool succes)
        {
            State = ContentLoadState.DOWNLOADED;
            onDoneDownloading.Invoke();
            onTileLoadCompleted.Invoke(this);
        }


        /// <summary>
        /// Clean up coroutines and content gameobjects
        /// </summary>
        public void Dispose()
        {
            
            Destroy(this.gameObject);            
        }

       
    }
}
