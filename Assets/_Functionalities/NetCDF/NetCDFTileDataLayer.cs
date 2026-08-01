using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using KindMen.Uxios;
using Netherlands3D.CartesianTiles;
using Netherlands3D.Coordinates;
using Netherlands3D.Credentials.StoredAuthorization;
using Netherlands3D.Twin.Utility;
using Newtonsoft.Json;
using UnityEngine;

namespace Netherlands3D.Functionalities.NetCDF
{
    public class NetCDFTileDataLayer : Layer
    {
        private const uint DefaultPageSize = 1000;
        
        private const CoordinateSystem DefaultEpsgCoordinateSystem = CoordinateSystem.RD;
        private Netherlands3D.CartesianTiles.TileHandler tileHandler;
        private Config requestConfig { get; set; } = Config.Default();

        private BoundingBox boundingBox;

        public BoundingBox BoundingBox
        {
            get => boundingBox;
            set
            {
                boundingBox = value;
                var crs2D = CoordinateSystems.To2D(value.CoordinateSystem);
                boundingBox.Convert(crs2D); //remove the height, since a GeoJSON is always 2D. This is needed to make the centering work correctly
            }
        }

        private string _url = "";

        public string Url
        {
            get => _url;
            set
            {
                _url = value;
                if (!_url.Contains("{0}"))
                    Debug.LogError("WFS URL does not contain a '{0}' placeholder for the bounding box.", gameObject);
            }
        }

        private void Awake()
        {
            //Make sure Datasets at least has one item
            if (Datasets.Count == 0)
            {
                var baseDataset = new DataSet()
                {
                    maximumDistance = 3000,
                    maximumDistanceSquared = 1000 * 1000
                };
                Datasets.Add(baseDataset);
            }

            StartCoroutine(FindTileHandler());
        }

        private IEnumerator FindTileHandler()
        {
            yield return null;

            //Find a required TileHandler in our parent, or else in the scene
            tileHandler = GetComponentInParent<Netherlands3D.CartesianTiles.TileHandler>();

            if (!tileHandler)
                tileHandler = FindAnyObjectByType<Netherlands3D.CartesianTiles.TileHandler>();

            if (tileHandler)
            {
                tileHandler.AddLayer(this);
                yield break;
            }

            Debug.LogError("No TileHandler found.", gameObject);
        }

        private bool IsInExtents(BoundingBox tileBox)
        {
            if (BoundingBox == null) //no bounds set, so we don't know the extents and always need to load the tile
                return true;

            return BoundingBox.Intersects(tileBox);
        }

        public override void HandleTile(TileChange tileChange, Action<TileChange> callback = null)
        {
            TileAction action = tileChange.action;
            var tileKey = new Vector2Int(tileChange.X, tileChange.Y);
            switch (action)
            {
                case TileAction.Create:
                    Tile newTile = CreateNewTile(tileKey);
                    tiles.Add(tileKey, newTile);
                    var tileBox = DetermineBoundingBox(tileChange, CoordinateSystem.RD);
                    if (IsInExtents(tileBox))
                    {
                        newTile.runningCoroutine = StartCoroutine(DownloadTile(tileChange, newTile, callback));
                    }
                    else
                    {
                        callback?.Invoke(tileChange); //nothing to download, call this to continue loading tiles
                    }

                    break;
                case TileAction.Upgrade:
                    tiles[tileKey].unityLOD++;
                    break;
                case TileAction.Downgrade:
                    tiles[tileKey].unityLOD--;
                    break;
                case TileAction.Remove:
                    InteruptRunningProcesses(tileKey);
                    tiles.Remove(tileKey);
                    callback?.Invoke(tileChange);
                    return;
                default:
                    break;
            }
        }

        private void OnGeoJSONLayerDestroyed()
        {
            Destroy(gameObject);
        }

        private void OnDestroy()
        {
            if (tileHandler)
                tileHandler.RemoveLayer(this);
        }

        private Tile CreateNewTile(Vector2Int tileKey)
        {
            Tile tile = new()
            {
                unityLOD = 0,
                tileKey = tileKey,
                layer = transform.gameObject.GetComponent<Layer>()
            };

            return tile;
        }

        private BoundingBox DetermineBoundingBox(TileChange tileChange, CoordinateSystem system)
        {
            var bottomLeft = new Coordinate(CoordinateSystem.RD, tileChange.X, tileChange.Y, 0);
            var topRight = new Coordinate(CoordinateSystem.RD, tileChange.X + tileSize, tileChange.Y + tileSize, 0);

            var boundingBox = new BoundingBox(bottomLeft, topRight);
            boundingBox.Convert(system);

            return boundingBox;
        }

       private IEnumerator DownloadTile(
    TileChange tileChange,
    Tile tile,
    Action<TileChange> callback = null)
{
    var boundingBox = DetermineBoundingBox(
        tileChange,
        CoordinateSystem.RD
    );

    Debug.Log(
        $"Request weather tile bbox: {boundingBox}"
    );


    //
    // 1. Vraag beschikbare KNMI bestanden op
    //

    string filesUrl =
        "https://api.dataplatform.knmi.nl/open-data/v1/datasets/uwcw-ha-det-nl-s1/versions/1.0/files";


    var configWithPayload =
        Config.BasedOn(requestConfig);


    configWithPayload.AddHeader(
        "Authorization",
        GetAPIKey()
    );


    configWithPayload =
        configWithPayload.WithPayload(
            new NetCDFTileDataLayerChangePayload(
                tileChange,
                filesUrl
            )
        );


    string filesJson = null;


    var filesRequest =
        Uxios.DefaultInstance.Get<string>(
            new Uri(filesUrl),
            configWithPayload
        );


    filesRequest.Then(response =>
    {
        filesJson = response.Data as string;
    });


    filesRequest.Catch(OnFailedToDownload);


    yield return Uxios.WaitForRequest(filesRequest);



    if (string.IsNullOrEmpty(filesJson))
    {
        Debug.LogError(
            "Geen bestandenlijst ontvangen"
        );

        callback?.Invoke(tileChange);
        yield break;
    }



    var fileResult =
        JsonConvert.DeserializeObject<KnmiFileResponse>(
            filesJson
        );



    //
    // 2. Zoek cloud fraction bestand
    //

    var cloudFile =
        fileResult.files.FirstOrDefault(file =>
            file.filename.Contains(
                "effective-type-cloud-area-fraction"
            )
        );



    if (cloudFile == null)
    {
        Debug.LogError(
            "Geen cloud fraction NetCDF gevonden"
        );

        callback?.Invoke(tileChange);
        yield break;
    }



    Debug.Log(
        $"Cloud file gevonden: {cloudFile.filename}"
    );



    //
    // 3. Vraag tijdelijke download URL op
    //

    string downloadUrlEndpoint =
        "https://api.dataplatform.knmi.nl/open-data/v1/datasets/uwcw-ha-det-nl-s1/versions/1.0/files/"
        + cloudFile.filename
        + "/url";



    string downloadJson = null;


    var downloadConfig =
        Config.BasedOn(requestConfig);


    downloadConfig.AddHeader(
        "Authorization",
        GetAPIKey()
    );


    downloadConfig =
        downloadConfig.WithPayload(
            new NetCDFTileDataLayerChangePayload(
                tileChange,
                downloadUrlEndpoint
            )
        );



    var downloadRequest =
        Uxios.DefaultInstance.Get<string>(
            new Uri(downloadUrlEndpoint),
            downloadConfig
        );



    downloadRequest.Then(response =>
    {
        downloadJson =
            response.Data as string;
    });


    downloadRequest.Catch(OnFailedToDownload);



    yield return Uxios.WaitForRequest(downloadRequest);



    if (string.IsNullOrEmpty(downloadJson))
    {
        Debug.LogError(
            "Geen download URL ontvangen"
        );

        callback?.Invoke(tileChange);
        yield break;
    }



    var download =
        JsonConvert.DeserializeObject<KnmiDownloadResponse>(
            downloadJson
        );



    Debug.Log(
        $"NetCDF URL ontvangen"
    );



    //
    // 4. Download echte NetCDF file
    //

    byte[] ncBytes = null;


    var ncRequest =
        Uxios.DefaultInstance.Get<byte[]>(
            new Uri(
                download.temporaryDownloadUrl
            ),
            Config.Default()
        );


    ncRequest.Then(response =>
    {
        ncBytes =
            response.Data as byte[];
    });


    ncRequest.Catch(exception =>
    {
        Debug.LogError(
            $"NetCDF download failed: {exception.Message}"
        );
    });



    yield return Uxios.WaitForRequest(ncRequest);



    if (ncBytes == null || ncBytes.Length == 0)
    {
        Debug.LogError(
            "Geen NetCDF data ontvangen"
        );

        callback?.Invoke(tileChange);
        yield break;
    }



    Debug.Log(
        $"NetCDF ontvangen: {ncBytes.Length / 1024f / 1024f:F2} MB"
    );



    //
    // 5. Bewaar bestand voor inspectie
    //

    string savePath =
        Path.Combine(
            Application.persistentDataPath,
            cloudFile.filename
        );


    File.WriteAllBytes(
        savePath,
        ncBytes
    );


    Debug.Log(
        $"NetCDF opgeslagen:\n{savePath}"
    );



    //
// 6. Read cloud fraction data
//

   
    // Download the .nupkg directly from https://www.nuget.org/packages/PureHDF (there's a "Download package" link on the page)
    // Rename it to .zip, extract it
    // Pull the PureHDF.dll out of the lib/netstandard2.0/ (or similar) folder inside
    // Drop that single .dll into Assets/Plugins/ in your Unity project





    var cloudReader = new CloudFractionReader(ncBytes);
    float[,] cloudData = cloudReader.GetCloudLayer(0);


    Debug.Log(
        $"Cloud data loaded: {cloudData.GetLength(0)} x {cloudData.GetLength(1)}"
    );



//
// Example value check
//

    Debug.Log(
        $"Cloud value [0,0]: {cloudData[0,0]}"
    );



    callback?.Invoke(tileChange);
}
        
        private void OnDownloadedNetCDFMetadata(IResponse response)
        {
            string json = response.Data as string;

            Debug.Log(json);
        }
        
        private void OnFailedToDownload(Exception exception)
        {
            if (exception is not Error uxiosError)
            {
                Debug.LogException(exception);
                return;
            }

            var payload = uxiosError.Config.GetPayload<NetCDFTileDataLayerChangePayload>();

            Debug.LogError(
                $"Could not download {payload.Url}: {exception.Message}"
            );

            RemoveGameObjectFromTile(payload.TileKey);
        }
        
        protected void RemoveGameObjectFromTile(Vector2Int tileKey)
        {
            if (tiles.ContainsKey(tileKey))
            {
                Tile tile = tiles[tileKey];
                if (tile == null)
                {
                    return;
                }

                if (tile.gameObject == null)
                {
                    return;
                }

                Destroy(tile.gameObject);
            }
        }

        private string GetAPIKey()
        {
            return
                "eyJvcmciOiI1ZTU1NGUxOTI3NGE5NjAwMDEyYTNlYjEiLCJpZCI6IjRjZGY2M2FjYTk1MjRiMzU4N2UwZTNjMjM5NWZlMTlmIiwiaCI6Im11cm11cjEyOCJ9";
        }
        
        [Serializable]
        public class KnmiFileResponse
        {
            public bool isTruncated;
            public int resultCount;
            public KnmiFile[] files;
        }



        [Serializable]
        public class KnmiFile
        {
            public string filename;
            public long size;
            public string created;
            public string lastModified;
        }



        [Serializable]
        public class KnmiDownloadResponse
        {
            public string temporaryDownloadUrl;
        }
        
        public record NetCDFTileDataLayerChangePayload(TileChange TileChange, string Url)
        {
            public TileChange TileChange { get; } = TileChange;
            public string Url  { get; } = Url;
            public Vector2Int TileKey => new(TileChange.X, TileChange.Y);
        }
    }
}