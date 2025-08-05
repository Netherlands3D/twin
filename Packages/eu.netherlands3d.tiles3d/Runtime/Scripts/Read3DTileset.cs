using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using SimpleJSON;
using UnityEngine.Networking;
using System;
using System.Linq;
using System.Text;
using System.Collections.Specialized;
using UnityEngine.Events;
using GLTFast;
using Netherlands3D.Coordinates;



#if UNITY_EDITOR
using UnityEditor;
#endif

namespace Netherlands3D.Tiles3D
{
    [RequireComponent(typeof(ReadSubtree))]
    public class Read3DTileset : MonoBehaviour
    {
        public string tilesetUrl = "https://storage.googleapis.com/ahp-research/maquette/kadaster/3dbasisvoorziening/test/landuse_1_1/tileset.json";
        public CoordinateSystem contentCoordinateSystem = CoordinateSystem.WGS84_ECEF;
       
        [Header("API Key (Optional)")]
        [Tooltip("Public API key for production use. This key will be used in production builds.")]
        public string publicKey;
        [Tooltip("Personal API key for testing purposes. This key will override the public key in Unity editor.")]
        public string personalKey;
        [Tooltip("The key name to use for the API key in the query string like 'key', or 'code' etc. Default is 'key' for Google Maps API.")]
        [SerializeField] private string queryKeyName = "key";
        public string QueryKeyName { get => queryKeyName; set => queryKeyName = value; }

        private string absolutePath = "";
        private string rootPath = "";
        private NameValueCollection queryParameters;

        [Header("Tileset")]
        public Tile root;
        public double[] transformValues;

        TilingMethod tilingMethod = TilingMethod.ExplicitTiling;

        public ImplicitTilingSettings implicitTilingSettings;

        public int tileCount;
        public int nestingDepth;

        public bool parseAssetMetadata = false;
#if SUBOBJECT
        public bool parseSubObjects = false;
#endif

        [Tooltip("Limits amount of detail higher resolution would cause to load.")]
        public int maxScreenHeightInPixels = 1080;
        public int maximumScreenSpaceError = 5;

        [SerializeField] private float sseComponent = -1;
        private List<Tile> visibleTiles = new List<Tile>();

        [SerializeField] private TilePrioritiser tilePrioritiser;
        private bool usingPrioritiser = true;

        private Camera currentCamera;
        private Vector3 lastCameraPosition;
        private Quaternion lastCameraRotation;
        private Vector3 currentCameraPosition;
        private Quaternion currentCameraRotation;
        private float lastCameraAngle = 60;

        internal string tilesetFilename = "tileset.json";

        private bool nestedTreeLoaded = false;

        [Header("Optional material override")] public Material materialOverride;

        [Header("Debugging")] public bool debugLog;
        
        public string[] usedExtensions { get; private set; }

        //Custom WebRequestHeader dictionary
        private Dictionary<string, string> customHeaders = new Dictionary<string, string>();
        public Dictionary<string, string> CustomHeaders { get => customHeaders; private set => customHeaders = value; }

        [Space(2)]
        public UnityEvent<string[]> unsupportedExtensionsParsed;

        [HideInInspector] public UnityEvent<UnityWebRequest> OnServerResponseReceived = new();
        [HideInInspector] public UnityEvent<UnityWebRequest.Result> OnServerRequestFailed = new();
        [HideInInspector] public UnityEvent<ContentMetadata> OnLoadAssetMetadata = new();
        [HideInInspector] public UnityEvent<Content> OnTileLoaded = new();
        
        public string CredentialQuery { get; private set; } = string.Empty;
        
        public void ClearKeyFromURL()
        {
            if (CredentialQuery != string.Empty)
            {
                tilesetUrl = tilesetUrl.Replace(CredentialQuery, string.Empty);
            }
        }
        
        public void ConstructURLWithKey()
        {
            ClearKeyFromURL(); //remove existing key if any is there
            UriBuilder uriBuilder = new UriBuilder(tilesetUrl);

            // Keep an existing query and ensure the leading `?` and, if so, a trailing `&` is stripped
            var queryString = uriBuilder.Query.TrimStart('?').TrimEnd('&') ?? string.Empty;
            if (!string.IsNullOrEmpty(queryString))
            {
                queryString += "&";
            }

            uriBuilder.Query = queryString;

            // Append the key query parameter
#if UNITY_EDITOR
            if (!string.IsNullOrEmpty(personalKey))
            {
                CredentialQuery = $"{QueryKeyName}={personalKey}";
                uriBuilder.Query += CredentialQuery;
            }
#else
            if (!string.IsNullOrEmpty(publicKey))
            {
                CredentialQuery = $"{QueryKeyName}={publicKey}";
                uriBuilder.Query += CredentialQuery;
            }
#endif
            tilesetUrl = uriBuilder.ToString();
        }

        void Start()
        {
            RefreshTiles();
        }

        private void OnEnable()
        {
            if (root !=null)
            {
                InvalidateBounds();
                StartCoroutine(LoadInView());
            }
            
        }
        public void RefreshTiles()
        {
            StopAllCoroutines();
            DisposeAllTilesRecursive(root);
            root = null;
            visibleTiles = new();

            InitializeURLAndLoadTileSet();
        }

        /// <summary>
        /// Add custom headers for all internal WebRequests
        /// </summary>
        public void AddCustomHeader(string key, string value, bool replace = true)
        {
            if(replace && customHeaders.ContainsKey(key))
                customHeaders[key] = value;
            else
                customHeaders.Add(key, value);
        }
        
        public void ClearCustomHeaders()
        {
            customHeaders.Clear();
        }

        private void DisposeAllTilesRecursive(Tile tile)
        {
            if (tile == null)
                return;

            foreach (var t in tile.children)
            {
                DisposeAllTilesRecursive(t);
            }

            tilePrioritiser.RequestDispose(tile, true);
        }

        void InitializeURLAndLoadTileSet()
        {
            ConstructURLWithKey();

            currentCamera = Camera.main;
            StartCoroutine(LoadInView());

            if (usingPrioritiser)
            {
                tilePrioritiser.SetCamera(currentCamera);
            }

            ExtractDatasetPaths();

            if(debugLog)
                print("loading tilset from : " + tilesetUrl);
            StartCoroutine(LoadTileset());
        }

        private void ExtractDatasetPaths()
        {
            Uri uri = new(tilesetUrl);
            absolutePath = tilesetUrl.Substring(0, tilesetUrl.LastIndexOf("/") + 1);
            if (tilesetUrl.StartsWith("file://"))
            {
                rootPath = absolutePath;
            }
            else
            {
                rootPath = uri.GetLeftPart(UriPartial.Authority);
            }

            queryParameters = ParseQueryString(uri.Query);
            
            if(debugLog)
                Debug.Log($"Query url {ToQueryString(queryParameters)}");
    
            foreach (string segment in uri.Segments)
            {
                if (segment.EndsWith(".json"))
                {
                    tilesetFilename = segment;
                    if (debugLog)
                    {
                        Debug.Log($"Dataset filename: {tilesetFilename}");
                        Debug.Log($"Absolute path: {absolutePath}");
                        Debug.Log($"Root path: {rootPath}");
                    }
                    break;
                }
            }
        }

        /// <summary>
        /// TODO: Use existing nl3d query parser / or move to Uri extention?
        /// </summary>
        /// <param name="queryString">?param=value&otherparam=othervalue</param>
        public NameValueCollection ParseQueryString(string queryString)
        {
            // Remove leading '?' if present
            if (queryString.StartsWith("?"))
                queryString = queryString.Substring(1);

            NameValueCollection queryParameters = new NameValueCollection();

            string[] querySegments = queryString.Split('&');
            for (int i = 0; i < querySegments.Length; i++)
            {
                string[] parts = querySegments[i].Split('=');
                if (parts.Length > 1)
                {
                    string key = UnityWebRequest.UnEscapeURL(parts[0]);
                    string value = UnityWebRequest.UnEscapeURL(parts[1]);
                    queryParameters.Add(key, value);
                }
            }

            return queryParameters;
        }

        /// <summary>
        /// Change camera used by tileset 'in view' calculations
        /// </summary>
        /// <param name="camera">Target camera</param>
        public void SetCamera(Camera camera)
        {
            currentCamera = camera;
        }

        /// <summary>
        /// Initialize tileset with these settings.
        /// This allows you initialize this component via code directly.
        /// </summary>
        /// <param name="tilesetUrl">The url pointing to tileset; https://.../tileset.json</param>
        /// <param name="maximumScreenSpaceError">The maximum screen space error for this tileset (default=5)</param>
        /// <param name="tilePrioritiser">Optional tile prioritisation system</param>
        public void Initialize(string tilesetUrl,CoordinateSystem contentCoordinateystem = CoordinateSystem.WGS84_ECEF, int maximumScreenSpaceError = 5, TilePrioritiser tilePrioritiser = null)
        {
            currentCamera = Camera.main;
            this.tilesetUrl = tilesetUrl;
            this.maximumScreenSpaceError = maximumScreenSpaceError;
            this.contentCoordinateSystem = contentCoordinateystem;
            SetTilePrioritiser(tilePrioritiser);
            RefreshTiles();
        }

        /// <summary>
        /// Optional injection of tile prioritiser system
        /// </summary>
        /// <param name="tilePrioritiser">Prioritising system with TilePrioritiser base class. Set to null to disable.</param>
        public void SetTilePrioritiser(TilePrioritiser tilePrioritiser)
        {
            this.tilePrioritiser = tilePrioritiser;
            usingPrioritiser = (tilePrioritiser);
        }

        public void RecalculateBounds()
        {
            if (root == null) return;

            
            RecalculateAllTileBounds(root);
        }

        public void InvalidateBounds()
        {
            if (root == null) return;

            //Flag all calculated bounds to be recalculated when tile bounds is requested
            InvalidateAllTileBounds(root);
        }

        /// <summary>
        /// Recursive recalculation of tile bounds
        /// </summary>
        /// <param name="tile">Starting tile</param>
        private void RecalculateAllTileBounds(Tile tile)
        {
            if (tile == null) return;

            tile.CalculateUnitBounds();

            foreach (var child in tile.children)
            {
                RecalculateAllTileBounds(child);
            }
        }

        /// <summary>
        /// Recursive invalidation of tile bounds
        /// tilebounds will be recaluclated when testing for isInView
        /// </summary>
        /// <param name="tile">Starting tile</param>
        private void InvalidateAllTileBounds(Tile tile)
        {
            if (tile == null) return;

            tile.boundsAvailable = false ;

            foreach (var child in tile.children)
            {
                InvalidateAllTileBounds(child);
            }
        }

        /// <summary>
        /// IEnumerator to load tileset.json from url
        /// </summary>
        IEnumerator LoadTileset()
        {
            UnityWebRequest www = UnityWebRequest.Get(tilesetUrl);
            foreach (var header in customHeaders)
                www.SetRequestHeader(header.Key, header.Value);

            yield return www.SendWebRequest();

            if (www.result != UnityWebRequest.Result.Success)
            {
                Debug.Log($"Could not load tileset from url:{tilesetUrl} Error:{www.error}");
                OnServerRequestFailed.Invoke(www.result);
            }
            else
            {
                string jsonstring = www.downloadHandler.text;
                ParseTileset.DebugLog = debugLog;
                ParseTileset.subtreeReader = GetComponent<ReadSubtree>();
                JSONNode rootnode = JSON.Parse(jsonstring)["root"];
                root = ParseTileset.ReadTileset(rootnode,this);
                
                var extensions = ParseTileset.GetUsedExtensions(rootnode);
                usedExtensions = extensions.Item1;
                unsupportedExtensionsParsed.Invoke(extensions.Item2);
            }

            OnServerResponseReceived.Invoke(www);
        }

        private void RequestContentUpdate(Tile tile)
        {
            if (tile.content!=null)
            {
                return;
            }
            if (!tile.content)
            {
                var newContentGameObject = new GameObject($"{tile.level},{tile.X},{tile.Y} content");
                newContentGameObject.transform.SetParent(transform, false);
                newContentGameObject.layer = gameObject.layer;
                tile.content = newContentGameObject.AddComponent<Content>();
                tile.content.tilesetReader = this;
                tile.content.State = Content.ContentLoadState.NOTLOADING;
                tile.content.ParentTile = tile;
                tile.content.uri = GetFullContentUri(tile);
                tile.content.parseAssetMetaData = parseAssetMetadata;
                tile.content.onTileLoadCompleted.AddListener(OnTileLoaded.Invoke);
#if SUBOBJECT
                tile.content.parseSubObjects = parseSubObjects;
                tile.content.contentcoordinateSystem = contentCoordinateSystem;
#endif

                //Request tile content update via optional prioritiser, or load directly
                if (usingPrioritiser)
                {
                    if (!tile.requestedUpdate)
                        tilePrioritiser.RequestUpdate(tile);
                }
                else
                {
                    tile.content.Load(materialOverride, verbose:debugLog);
                }
            }
        }

        private void RequestDispose(Tile tile)
        {
            if (!tile.content) return;

            if (usingPrioritiser && !tile.requestedDispose)
            {
                tilePrioritiser.RequestDispose(tile);
            }
            else
            {
                tile.content.Dispose();
                tile.content = null;                
            }
        }


        /// <summary>
        /// Check what tiles should be loaded/unloaded based on view recursively
        /// </summary>
        private IEnumerator LoadInView()
        {
            yield return new WaitUntil(() => root != null);
            while (true)
            {
                //If camera changed, recalculate what tiles are be in view
                currentCamera.transform.GetPositionAndRotation(out currentCameraPosition, out currentCameraRotation);
                lastCameraAngle = (currentCamera.orthographic ? currentCamera.orthographicSize : currentCamera.fieldOfView);
                currentCamera.transform.GetPositionAndRotation(out lastCameraPosition, out lastCameraRotation);

                SetSSEComponent(currentCamera);
                DisposeTilesOutsideView(currentCamera);
                foreach (var child in root.children)
                {
                    LoadInViewRecursively(child, currentCamera);
                }

                yield return null;
            }
        }

        /// <summary>
        /// Check for tiles in our visibile tiles list that moved out of the view / max distance.
        /// Request dispose for tiles that moved out of view
        /// </summary>
        /// <param name="currentCamera">Camera to use for visibility check</param>
        private void DisposeTilesOutsideView(Camera currentCamera)
        {
            for (int i = visibleTiles.Count - 1; i >= 0; i--)
            {
                var tile = visibleTiles[i];
                var closestPointOnBounds = tile.ContentBounds.ClosestPoint(currentCamera.transform.position); //Returns original point when inside the bounds
                CalculateTileScreenSpaceError(tile, currentCamera, closestPointOnBounds);
            }

            //Clean up list op previously loaded tiles outside of view
            for (int i = visibleTiles.Count - 1; i >= 0; i--)
            {
                var tile = visibleTiles[i];
                var tileIsInView = tile.IsInViewFrustrum(currentCamera);
                if (!tileIsInView)
                {
                    tilePrioritiser.RequestDispose(tile, true);
                    visibleTiles.RemoveAt(i);
                    continue;
                }


                var enoughDetail = tile.screenSpaceError < maximumScreenSpaceError;
                if (enoughDetail) // tile has (more then) enoug detail
                {
                    if (tile.parent.screenSpaceError<maximumScreenSpaceError) //parent tile also has enough detail
                    {
                        // can be removed if a parentTile is loaded
                        if (tile.parent.CountLoadedParents() > 0)
                        {
                            tilePrioritiser.RequestDispose(tile, true);
                            visibleTiles.RemoveAt(i);
                            continue;
                        }
                    }
                   

                }

                else //too little detail
                {
                    if (tile.refine=="ADD")
                    {
                        // tile should remain
                    }
                    else if (tile.CountLoadingChildren() == 0)
                    {
                        if (tile.CountLoadedChildren() > 0)
                        {
                            tilePrioritiser.RequestDispose(tile);

                            visibleTiles.RemoveAt(i);
                        }
                    }


                }
            }
        }

        private void CalculateTileScreenSpaceError(Tile child, Camera currentMainCamera, Vector3 closestPointOnBounds)
        {
            float sse;
            if (currentMainCamera.orthographic)
            {
                //geometric error has no influence anymore so lets calculate the sse camera values
                Bounds bounds = child.ContentBounds;
                Vector3 maxExtent = Vector3.one * 1000f;
                Vector3 clampedExtents = Vector3.Min(bounds.extents, maxExtent);
                float halfHeight = currentMainCamera.orthographicSize;
                float halfWidth = halfHeight * currentMainCamera.aspect;
                float frustumDiagonal = new Vector2(halfWidth * 2f, halfHeight * 2f).magnitude;
                float tileGroundSize = new Vector2(clampedExtents.x * 2f, clampedExtents.z * 2f).magnitude;
                float ratio = tileGroundSize / frustumDiagonal;
                float adjustedRatio = Mathf.Pow(ratio, 0.5f);
                float zoomFactor = Mathf.Clamp(halfHeight / 10f, 0.1f, 10f);
                float rawSSE = sseComponent * adjustedRatio * zoomFactor * 2f;
                sse = Mathf.Max(rawSSE, 0.5f);
            }
            else if (Vector3.Distance(currentMainCamera.transform.position, closestPointOnBounds) < 0.1)
            {
                sse = float.MaxValue;
            }
            else
            {
                sse = (sseComponent * (float)child.geometricError) / Vector3.Distance(currentMainCamera.transform.position, closestPointOnBounds);
            }
            child.screenSpaceError = sse;
        }

        private void LoadInViewRecursively(Tile tile, Camera currentCamera)
        {
            var tileIsInView = tile.IsInViewFrustrum(currentCamera);
            if (!tileIsInView)
            {
                return;
            }

            if (tile.isLoading == false && tile.children.Count == 0 && tile.contentUri.Contains(".json"))
            {
                tile.isLoading = true;
                StartCoroutine(LoadNestedTileset(tile));
                return;
            }

            if (tile.isLoading == false && tile.children.Count == 0 && tile.contentUri.Contains(".subtree"))
            {
                UnityEngine.Debug.Log(tile.contentUri);
                ReadSubtree subtreeReader = GetComponent<ReadSubtree>();
                if (subtreeReader.isbusy)
                {
                    return;
                }

                subtreeReader.isbusy = true;
                tile.isLoading = true;

                Debug.Log("try to download a subtree");
                subtreeReader.DownloadSubtree("", implicitTilingSettings, tile, subtreeLoaded);
                return;
            }

            var closestPointOnBounds = tile.ContentBounds.ClosestPoint(currentCamera.transform.position); //Returns original point when inside the bounds
            CalculateTileScreenSpaceError(tile, currentCamera, closestPointOnBounds);
            var enoughDetail = tile.screenSpaceError < maximumScreenSpaceError;
            var Has3DContent = tile.contentUri.Length > 0 && !tile.contentUri.Contains(".json") && !tile.contentUri.Contains(".subtree");
            if (enoughDetail == false)  
            {
                if (tile.refine == "ADD" && Has3DContent)
                {
                    if (!visibleTiles.Contains(tile))
                    {
                        RequestContentUpdate(tile);
                        visibleTiles.Add(tile);
                    }
                }
                else if (tile.children.Count == 0 && Has3DContent) //show the geometry if more detailed geometry is not available
                {
                    if (!visibleTiles.Contains(tile))
                    {
                        RequestContentUpdate(tile);
                        visibleTiles.Add(tile);
                    }
                }
                foreach (var childTile in tile.children)
                {
                    LoadInViewRecursively(childTile, currentCamera);
                }
            }
            else
            {
                    if (Has3DContent)
                    {
                        int loadingParentsCount = tile.CountLoadingParents();
                        int loadedParentsCount = tile.CountLoadedParents();
                        //if (loadedParentsCount + loadingParentsCount < 2)
                        //{
                            if (!visibleTiles.Contains(tile))
                            {
                                RequestContentUpdate(tile);
                                visibleTiles.Add(tile);
                            }
                        //}
                    }
            }

           
        }

        public void subtreeLoaded(Tile tile)
        {
            tile.parent.isLoading = false;
        }

        private IEnumerator LoadNestedTileset(Tile tile)
        {
            if (tilingMethod == TilingMethod.ExplicitTiling)
            {
                if (tile.contentUri.Contains(".json") && !tile.nestedTilesLoaded)
                {
                    string nestedJsonPath = GetFullContentUri(tile);
                    UnityWebRequest www = UnityWebRequest.Get(nestedJsonPath);
                    
                    foreach (var header in customHeaders)
                        www.SetRequestHeader(header.Key, header.Value);
                        
                    yield return www.SendWebRequest();

                    if (www.result != UnityWebRequest.Result.Success)
                    {
                        Debug.Log(www.error + " at " + nestedJsonPath);
                        OnServerRequestFailed.Invoke(www.result);
                    }
                    else
                    {
                        string jsonstring = www.downloadHandler.text;
                        tile.nestedTilesLoaded = true;

                        JSONNode node = JSON.Parse(jsonstring)["root"];
                        ParseTileset.ReadExplicitNode(node, tile);
                        nestedTreeLoaded = true;
                    }
                    OnServerResponseReceived.Invoke(www);
                }

                tile.isLoading = false;
            }
            else if (tilingMethod == TilingMethod.ImplicitTiling)
            {
                //Possible future nested subtree support.
            }
        }

        private string GetFullContentUri(Tile tile)
        {
            var relativeContentUrl = tile.contentUri;

            //RD amsterdam specific temp fix.
            relativeContentUrl = relativeContentUrl.Replace("../", "");

            var fullPath = (tile.contentUri.StartsWith("/")) ? rootPath + relativeContentUrl : absolutePath + relativeContentUrl;

            //Combine query to pass on session id and API key (Google Maps 3DTiles API style)
            UriBuilder uriBuilder = new(fullPath);
            NameValueCollection contentQueryParameters = ParseQueryString(uriBuilder.Query);
            foreach (string key in queryParameters.Keys)
            {
                if (!contentQueryParameters.AllKeys.Contains(key))
                {
                    contentQueryParameters.Add(key, queryParameters[key]);
                }
            }
            foreach (string key in contentQueryParameters.Keys)
            {
                if (!queryParameters.AllKeys.Contains(key))
                {
                    queryParameters.Add(key, contentQueryParameters[key]);
                }
            }

            uriBuilder.Query = ToQueryString(contentQueryParameters);
            var url = uriBuilder.ToString();
            return url;
        }

        private string ToQueryString(NameValueCollection queryParameters)
        {
            if (queryParameters.Count == 0) return "";

            StringBuilder queryString = new StringBuilder();
            for (int i = 0; i < queryParameters.Count; i++)
            {
                string key = queryParameters.GetKey(i);
                string[] values = queryParameters.GetValues(i);

                if (!string.IsNullOrEmpty(key) && values != null)
                {
                    for (int j = 0; j < values.Length; j++)
                    {
                        string value = values[j];

                        if (queryString.Length > 0)
                            queryString.Append("&");

                        queryString.AppendFormat("{0}={1}", Uri.EscapeDataString(key), Uri.EscapeDataString(value));
                    }
                }
            }

            return "?" + queryString.ToString();
        }


        /// <summary>
        /// Screen-space error component calculation.
        /// Screen height is clamped to limit the amount of geometry that
        /// would be loaded on very high resolution displays.
        /// </summary>
        public void SetSSEComponent(Camera currentCamera)
        {
            if (usingPrioritiser) maxScreenHeightInPixels = tilePrioritiser.MaxScreenHeightInPixels;

            var screenHeight = (maxScreenHeightInPixels > 0) ? Mathf.Min(maxScreenHeightInPixels, Screen.height) : Screen.height;

            if (currentCamera.orthographic)
            {
                sseComponent = screenHeight / currentCamera.orthographicSize;
            }
            else
            {
                var coverage = 2 * Mathf.Tan((Mathf.Deg2Rad * currentCamera.fieldOfView) / 2);
                sseComponent = screenHeight / coverage;
            }
        }

        public void SetCoordinateSystem(CoordinateSystem newContentCoordinateSystem)
        {
            contentCoordinateSystem = newContentCoordinateSystem;
            ScenePosition[] scenepositions = GetComponentsInChildren<ScenePosition>();
            foreach (ScenePosition scenepos in scenepositions)
            {
                if ((int)newContentCoordinateSystem == scenepos.contentposition.CoordinateSystem)
                {
                    continue;
                }
                Coordinate newCoord = new Coordinate(newContentCoordinateSystem);
                newCoord.easting = scenepos.contentposition.easting;
                newCoord.northing = scenepos.contentposition.northing;
                newCoord.height = scenepos.contentposition.height;
                scenepos.contentposition = newCoord;
                scenepos.gameObject.transform.position = newCoord.ToUnity();
            }
            InvalidateBounds();
        }
    }

    public enum TilingMethod
    {
        Unknown,
        ExplicitTiling,
        ImplicitTiling
    }

    public enum RefinementType
    {
        Replace,
        Add
    }

    public enum SubdivisionScheme
    {
        Quadtree,
        Octree
    }

    [System.Serializable]
    public class ImplicitTilingSettings
    {
        public RefinementType refinementType;
        public SubdivisionScheme subdivisionScheme;
        public int availableLevels;
        public int subtreeLevels;
        public string subtreeUri;
        public string contentUri;
        public float geometricError;
        public BoundingVolume boundingVolume;
        public double[] boundingRegion;
    }
}