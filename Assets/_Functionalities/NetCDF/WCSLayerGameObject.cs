using System;
using Netherlands3D.Twin.Layers.Properties;
using System.Collections.Generic;
using Netherlands3D.Coordinates;
using Netherlands3D.OgcWebServices.Shared;
using Netherlands3D.Twin.Layers.LayerTypes.CartesianTiles;
using Netherlands3D.Twin.Utility;
using UnityEngine;
using Netherlands3D.Credentials;
using Netherlands3D.Credentials.StoredAuthorization;
using Netherlands3D.Functionalities.Wcs;
using Netherlands3D.Functionalities.Wms;
using Netherlands3D.Services;
using Netherlands3D.Twin.Layers;
using Netherlands3D.Twin.Layers.LayerTypes.Credentials.Properties;
using Netherlands3D.Legend;

namespace Netherlands3D.Functionalities.Wcs
{
    /// <summary>
    /// Extention of LayerGameObject that injects a 'streaming' dataprovider WMSTileDataLayer
    /// </summary>
    [RequireComponent(typeof(WCSTileDataLayer))]
    [RequireComponent(typeof(ICredentialHandler))]
    public class WCSLayerGameObject : CartesianTileLayerGameObject, IVisualizationWithPropertyData
    {
        private WCSTileDataLayer wcsLayer;
        private ICredentialHandler credentialHandler;
        
        [SerializeField] private GameObject volumePrefab;
        
        public Vector2Int Resolution = Vector2Int.one * 256;
   
        public override BoundingBox Bounds => wcsLayer?.BoundingBox;
        
        protected override void OnVisualizationInitialize()
        {
            base.OnVisualizationInitialize();
            wcsLayer = GetComponent<WCSTileDataLayer>();
            credentialHandler = GetComponent<ICredentialHandler>();
        }

        protected override void OnVisualizationReady()
        {
            base.OnVisualizationReady();
            var urlPropertyData = LayerData.GetProperty<LayerURLPropertyData>();
            UpdateURL(urlPropertyData.Url);
        }
        
         private void HandleCredentials(Uri uri, StoredAuthorization auth)
        {
            ClearCredentials();

            if (auth.GetType() != typeof(Public))//if it is public, we don't want the property panel to show up
            {
                InitProperty<CredentialsRequiredPropertyData>(LayerData.LayerProperties);
            }
            
            if (auth is FailedOrUnsupported)
            {
                LayerData.HasValidCredentials = false;
                wcsLayer.isEnabled = false;
                return;
            }
            
            var getCapabilitiesString = OgcWebServicesUtility.CreateGetCapabilitiesURL(wcsLayer.Url, ServiceType.Wcs);
            var getCapabilitiesUrl = new Uri(getCapabilitiesString);
            BoundingBoxCache.Instance.GetBoundingBoxContainer(
                getCapabilitiesUrl,
                auth,
                (responseText) => new WcsGetCapabilities(getCapabilitiesUrl, responseText),
                SetBoundingBox
            );

            wcsLayer.SetAuthorization(auth);
            LayerData.HasValidCredentials = true;           
            wcsLayer.isEnabled = LayerData.ActiveInHierarchy;
        }

        public void ClearCredentials()
        {
            wcsLayer.ClearConfig();
        }

        public virtual void LoadProperties(List<LayerPropertyData> properties)
        {
            var urlPropertyData = LayerData.GetProperty<LayerURLPropertyData>();
            if (urlPropertyData == null) return;
            
            UpdateURL(urlPropertyData.Url);
        }

        private void UpdateURL(Uri storedUri)
        {
            if (storedUri == credentialHandler.Uri && credentialHandler.Authorization != null)
            {
                HandleCredentials(storedUri, credentialHandler.Authorization);
                return;
            }
            
            credentialHandler.Uri = storedUri; //apply the URL from what is stored in the Project data
            wcsLayer.Url = storedUri.ToString();
            credentialHandler.ApplyCredentials();
        }

        protected override void RegisterEventListeners()
        {
            base.RegisterEventListeners();
            credentialHandler.OnAuthorizationHandled.AddListener(HandleCredentials);
            
            wcsLayer.OnDataLoaded.AddListener(UpdateVolume);
            wcsLayer.OnTileDestroyed.AddListener(DestroyCloudVolume);
        }

        protected override void UnregisterEventListeners()
        {
            base.UnregisterEventListeners();
            credentialHandler.OnAuthorizationHandled.RemoveListener(HandleCredentials);
            
            wcsLayer.OnDataLoaded.RemoveListener(UpdateVolume);
            wcsLayer.OnTileDestroyed.RemoveListener(DestroyCloudVolume);
        }

        public override void OnLayerActiveInHierarchyChanged(bool isActive)
        {
            var urlPropertyData = LayerData.GetProperty<LayerURLPropertyData>();
            if (isActive)
            {
                UpdateURL(urlPropertyData.Url);
            }
            
            if (wcsLayer.isEnabled == isActive) return;

            wcsLayer.isEnabled = isActive;
        }

        private void SetBoundingBox(BoundingBoxContainer boundingBoxContainer)
        {
            if (boundingBoxContainer == null) return;

            var urlPropertyData = LayerData.GetProperty<LayerURLPropertyData>();
            var wcsUrl = urlPropertyData.Url.ToString();

            var coverageName =
                OgcWebServicesUtility.GetParameterFromURL(wcsUrl, "coverageid")
                ?? OgcWebServicesUtility.GetParameterFromURL(wcsUrl, "coverage");

            if (boundingBoxContainer.LayerBoundingBoxes.ContainsKey(coverageName))
            {
                wcsLayer.BoundingBox = boundingBoxContainer.LayerBoundingBoxes[coverageName];
                return;
            }

            wcsLayer.BoundingBox = boundingBoxContainer.GlobalBoundingBox;
        }
       
        
        public class CloudTileData
        {
            public Texture2D cloudFraction;
            public Texture2D cloudHeight;
            public Texture3D volume;
            public float maxCloudHeight;
            public GameObject volumeObject;
            
        }

        public static Dictionary<Vector2Int, CloudTileData> CloudTiles = new();


        private void UpdateVolume(Texture2D texture, Vector2Int tileKey, Vector2 dataBounds)
        {
            var coverageName =
                OgcWebServicesUtility.GetParameterFromURL(
                    wcsLayer.Url,
                    "coverageid"
                )
                ??
                OgcWebServicesUtility.GetParameterFromURL(
                    wcsLayer.Url,
                    "coverage"
                );


            if (!CloudTiles.TryGetValue(tileKey, out var tile))
            {
                tile = new CloudTileData();
                CloudTiles.Add(tileKey, tile);
            }

            if (coverageName.Contains("cloud_area_fraction"))
            {
                tile.cloudFraction = texture;
            }
            else if (coverageName.Contains("height_at_cloud_top"))
            {
                tile.cloudHeight = texture;
            }
            else
            {
                return;
            }
            // Rebuild only this tile
            //tile.volume = CreateCloudVolume(tile.cloudFraction, tile.cloudHeight);
            tile.volume = CreatePyramidVolume(128, 128);
            if (tile.volume != null)
            {
                //float scale = 1000f / Mathf.Abs(StandardBoundingBoxes.Wgs84LatLon_NetherlandsBounds_Cropped.Size.ToUnity().x);
                if (tile.volumeObject == null)
                {
                    tile.maxCloudHeight = 12000; //this is arbitrary but loading the entire country and then min max all data gives 12000
                    tile.volumeObject = Instantiate(volumePrefab);
                    tile.volumeObject.name = $"CloudVolume_{tileKey.x}_{tileKey.y}";
                    tile.volumeObject.transform.localScale = new Vector3(
                        1000f,
                        tile.maxCloudHeight,
                        1000f
                    );

                    GameObject testCloud = GameObject.CreatePrimitive(PrimitiveType.Sphere);
                    float scale = dataBounds.y - dataBounds.x;
                    testCloud.transform.localScale = scale * Vector3.one;
                    testCloud.transform.position = new Coordinate(CoordinateSystem.RDNAP, tileKey.x + 0.5f * wcsLayer.tileSize, tileKey.y + 0.5f * wcsLayer.tileSize, (dataBounds.x + dataBounds.y) * 0.5f).ToUnity();

                }
                float halfHeight = tile.maxCloudHeight * 0.5f;
                tile.volumeObject.transform.position = new Coordinate(CoordinateSystem.RDNAP, tileKey.x + 0.5f * wcsLayer.tileSize, tileKey.y + 0.5f * wcsLayer.tileSize, halfHeight).ToUnity();
                MeshRenderer renderer = tile.volumeObject.GetComponent<MeshRenderer>();
                renderer.material.SetTexture("_CloudVolume", tile.volume);
                Debug.Log($"Created cloud volume x{tile.volume.width}y{tile.volume.height}z{tile.volume.depth}key x{tileKey.x}y{tileKey.y}");
            }
        }

        private void DestroyCloudVolume(Vector2Int tileKey)
        {
            if (!CloudTiles.TryGetValue(tileKey, out var tile))
                return;
           
            if (tile.volumeObject != null)
            {
                Destroy(tile.volumeObject);
                tile.volumeObject = null;
            }
            if (tile.volume != null)
            {
                Destroy(tile.volume);
                tile.volume = null;
            }
            tile.cloudFraction = null;
            tile.cloudHeight = null;
            CloudTiles.Remove(tileKey);
        }

        private Texture3D CreateCloudVolume(Texture2D cloudFraction, Texture2D cloudHeight)
        {
            int width;
            int height;

            if (cloudFraction != null)
            {
                width = cloudFraction.width;
                height = cloudFraction.height;
            }
            else if (cloudHeight != null)
            {
                width = cloudHeight.width;
                height = cloudHeight.height;
            }
            else
            {
                Debug.LogError("No cloud data available");
                return null;
            }


            int depth = 64; // altitude layers

            // X = map width
            // Y = altitude
            // Z = map height
            Texture3D cloudVolume = new Texture3D(
                width,
                depth,
                height,
                TextureFormat.RFloat,
                false
            );

            Color[] voxels = new Color[width * depth * height];
            Color[] fractionPixels = null;
            Color[] heightPixels = null;
            if (cloudFraction != null)
                fractionPixels = cloudFraction.GetPixels();

            if (cloudHeight != null)
                heightPixels = cloudHeight.GetPixels();

            float defaultCloudHeight = 4000f / 12000f;

            // Y is now altitude
            for (int y = 0; y < depth; y++)
            {
                float altitude01 = y / (float)(depth - 1);

                for (int z = 0; z < height; z++)
                {
                    for (int x = 0; x < width; x++)
                    {
                        int index2D = z * width + x;
                        float coverage = 1f;
                        if (fractionPixels != null)
                        {
                            coverage = fractionPixels[index2D].r;
                        }
                        float cloudTop = defaultCloudHeight;
                        if (heightPixels != null)
                        {
                            cloudTop = heightPixels[index2D].r;
                        }

                        float density = 0f;
                        float cloudBase = 0.15f;

                        if (altitude01 > cloudBase && altitude01 < cloudTop)
                        {
                            float height01 = (altitude01 - cloudBase) / (cloudTop - cloudBase);
                            // smooth cloud vertical profile
                            float profile = Mathf.Sin(height01 * Mathf.PI);
                            density = coverage * profile;
                        }
                        // Texture3D indexing:
                        // X + Y*width + Z*width*depth
                        int index3D = x +
                                      y * width +
                                      z * width * depth;
                        voxels[index3D] = new Color(density, 0f, 0f, 1f);
                    }
                }
            }

            cloudVolume.SetPixels(voxels);
            cloudVolume.Apply();
            cloudVolume.wrapMode = TextureWrapMode.Clamp;
            cloudVolume.filterMode = FilterMode.Bilinear;
            return cloudVolume;
        }
       
        public Texture3D CreatePyramidVolume(int size, int depth)
        {
            // X = width
            // Y = height (up)
            // Z = depth
            Texture3D volume = new Texture3D(
                size,
                depth,
                size,
                TextureFormat.RFloat,
                false
            );


            Color[] voxels =
                new Color[size * depth * size];


            for (int y = 0; y < depth; y++)
            {
                // altitude 0 -> 1
                float y01 =
                    y / (float)(depth - 1);


                // pyramid gets narrower towards the top
                float pyramidWidth =
                    Mathf.Lerp(1.0f, 0.0f, y01);


                for (int z = 0; z < size; z++)
                {
                    for (int x = 0; x < size; x++)
                    {

                        float x01 =
                            x / (float)(size - 1);

                        float z01 =
                            z / (float)(size - 1);


                        // center coordinates -1 to 1
                        float px =
                            x01 * 2 - 1;

                        float pz =
                            z01 * 2 - 1;


                        float distance =
                            Mathf.Max(
                                Mathf.Abs(px),
                                Mathf.Abs(pz)
                            );


                        float density = 0f;


                        if (distance < pyramidWidth)
                        {
                            float edge =
                                1 -
                                distance / pyramidWidth;


                            density =
                                Mathf.SmoothStep(
                                    0,
                                    1,
                                    edge
                                );
                        }


                        // X + Y*width + Z*width*height
                        int index =
                            x +
                            y * size +
                            z * size * depth;


                        voxels[index] =
                            new Color(
                                density,
                                0,
                                0,
                                1
                            );
                    }
                }
            }


            volume.SetPixels(voxels);
            volume.Apply();


            volume.wrapMode =
                TextureWrapMode.Clamp;

            volume.filterMode =
                FilterMode.Bilinear;


            return volume;
        }
    }
}