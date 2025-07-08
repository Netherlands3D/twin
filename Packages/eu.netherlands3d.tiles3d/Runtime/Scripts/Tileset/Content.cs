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

        private UnityEngine.Material overrideMaterial;

        private GltfImport gltf;
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
            //runningContentRequest = StartCoroutine(
            //    ImportB3DMGltf.ImportBinFromURL(
            //        uri, 
            //        GotGltfContent,
            //        false,
            //        headers
                //)
            //);
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

        }
//        private async void GotGltfContent(ParsedGltf parsedGltf)
//        {
//            if (State != ContentLoadState.DOWNLOADING)
//            {
//                State = ContentLoadState.DOWNLOADED;
//                return;
//            }
//            State = ContentLoadState.PARSING;
//            parentTile.isLoading = false;
//            if (parsedGltf == null)
//            {
//                Debug.Log("failed to parse: "+ uri +" , trying again");
//                State = ContentLoadState.DOWNLOADED;
//                return;
//            }

//            var gltf = parsedGltf.gltfImport;
//            var scene = transform;
//            if (gltf != null)
//            {
//                this.gltf = gltf;
//                var scenes = gltf.SceneCount;

//                for (int i = 0; i < scenes; i++)
//                {
//                    await gltf.InstantiateSceneAsync(transform, i);
//                    scene = transform.GetChild(i).transform;
//                    foreach (var child in scene.GetComponentsInChildren<Transform>(true)) //getting the Transform components ensures the layer of each recursive child is set 
//                    {
//                        child.gameObject.layer = gameObject.layer;
//                    }
                    
//                    if(scene == null) continue;

//                    if (parsedGltf.rtcCenter != null)
//                    {
                        
//                        //Debug.Log("Has RTC-Center "+ parsedGltf.rtcCenter[0] +";"+ parsedGltf.rtcCenter[1]+";"+ parsedGltf.rtcCenter[2]);
//                        scene.rotation = CoordinateConverter.ecefRotionToUp() * (scene.rotation);
//                        Coordinate sceneCoordinate = new Coordinate(CoordinateSystem.WGS84_ECEF_TELAAG, parsedGltf.rtcCenter[0], parsedGltf.rtcCenter[1], parsedGltf.rtcCenter[2]);
//                        Coordinate transformedCoordinate = parentTile.tileTransform.MultiplyPoint3x4(sceneCoordinate);
//                        Vector3 unityPosition = transformedCoordinate.ToUnity();

//                        //Vector3 unityPosition = CoordinateConverter.ECEFToUnity(new Vector3ECEF(parsedGltf.rtcCenter[0] + parentTile.transform[12], parsedGltf.rtcCenter[1] + parentTile.transform[13], parsedGltf.rtcCenter[2] + parentTile.transform[14]));
//                        scene.position = unityPosition;
//                    }
//                    else
//                    {
//                        Coordinate sceneCoordinate = new Coordinate(CoordinateSystem.WGS84_ECEF_TELAAG, -scene.localPosition.x, -scene.localPosition.z, scene.localPosition.y);
//                        Coordinate transformedCoordinate = parentTile.tileTransform.MultiplyPoint3x4(sceneCoordinate);
//                        Vector3 unityPosition = transformedCoordinate.ToUnity();
//                        scene.rotation = CoordinateConverter.ecefRotionToUp() * (scene.rotation);
//                        scene.position = unityPosition;
//                    }
//                }

//                this.gameObject.name = uri;
                
//                if(parseAssetMetaData)
//                {
//                    parsedGltf.ParseAssetMetaData(this);
//                }

//                //Check if mesh features addon is used to define subobjects
//#if SUBOBJECT
//                if(parseSubObjects)
//                {
//                    parsedGltf.ParseSubObjects(transform);
//                }
//#endif

//                if(overrideMaterial != null)
//                {
//                    OverrideAllMaterials(transform);
//                }
//            }

//            State = ContentLoadState.DOWNLOADED;
//            onDoneDownloading.Invoke();
//        }

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
