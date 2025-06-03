using System;
using System.Collections;
using System.Collections.Generic;
using KindMen.Uxios;
using Netherlands3D.CartesianTiles;
using Netherlands3D.Coordinates;
using Netherlands3D.Credentials.StoredAuthorization;
using Netherlands3D.Twin.Utility;
using UnityEngine;
using UnityEngine.Rendering.Universal;

namespace Netherlands3D.Functionalities.Wms
{
    public class WMSTileDataLayer : ImageProjectionLayer
    {
        private const string DefaultEpsgCoordinateSystem = "28992";

        private Config requestConfig { get; set; } = new Config()
        {
            TypeOfResponseType = ExpectedTypeOfResponse.Texture(true)
        };

        public int RenderIndex
        {
            get => renderIndex;
            set
            {
                int oldIndex = renderIndex;
                renderIndex = value;
                if (oldIndex != renderIndex)
                    UpdateDrawOrderForChildren();
            }
        }

        private int renderIndex = -1;

        private string wmsUrl = "";

        public string WmsUrl
        {
            get => wmsUrl;
            set
            {
                wmsUrl = value;
                if (!wmsUrl.Contains("{0}"))
                    Debug.LogError("WMS URL does not contain a '{0}' placeholder for the bounding box.", gameObject);
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
        }

        public void SetAuthorization(StoredAuthorization auth)
        {
            ClearConfig();
            requestConfig = auth.AddToConfig(requestConfig);
        }

        public void ClearConfig()
        {
            requestConfig = new Config()
            {
                TypeOfResponseType = ExpectedTypeOfResponse.Texture(true)
            };
        }
        
        protected override IEnumerator DownloadDataAndGenerateTexture(TileChange tileChange, Action<TileChange> callback = null)
        {
            var tileKey = new Vector2Int(tileChange.X, tileChange.Y);

            if (!tiles.ContainsKey(tileKey))
            {
                onLogMessage.Invoke(LogType.Warning, "Tile key does not exist");
                yield break;
            }

            //on loading project form save file this can be empty 
            if (string.IsNullOrEmpty(wmsUrl))
                yield break;

            var mapData = MapFilters.FromUrl(new Uri(wmsUrl));
            Tile tile = tiles[tileKey];

            var boundingBox = DetermineBoundingBox(tileChange, mapData);
            string url = wmsUrl.Replace("{0}", boundingBox.ToString());
            var promise = Uxios.DefaultInstance.Get<Texture2D>(new Uri(url), requestConfig);

            promise.Then(response =>
                {
                    ClearPreviousTexture(tile);
                    Texture2D tex = response.Data as Texture2D;
                    tex.name = tile.tileKey.ToString();
                    tex.Compress(true);
                    tex.filterMode = FilterMode.Bilinear;
                    tex.Apply(false, true);

                    if (!tile.gameObject.TryGetComponent<TextureProjectorBase>(out var projector))
                    {
                        Destroy(tex);
                        return;
                    }
                    //if (foundCRS == CoordinateSystem.CRS84)
                    //    projector.SetSize((float)widthMeters, (float)heightMeters, ProjectorMinDepth);
                    //else
                        projector.SetSize(tileSize, tileSize, tileSize);
                    projector.gameObject.SetActive(isEnabled);
                    projector.SetTexture(tex);

                    //force the depth to be at least larger than its height to prevent z-fighting
                    DecalProjector decalProjector = tile.gameObject.GetComponent<DecalProjector>();
                    TextureDecalProjector textureDecalProjector = tile.gameObject.GetComponent<TextureDecalProjector>();
                    if (ProjectorHeight >= decalProjector.size.z)
                        textureDecalProjector.SetSize(decalProjector.size.x, decalProjector.size.y, ProjectorMinDepth);

                    //set the render index, to make sure the render order is maintained
                    textureDecalProjector.SetPriority(renderIndex);
                }
            );

            promise.Catch(exception =>
            {
                Debug.LogError($"Could not download {url}: " + exception.Message);
                RemoveGameObjectFromTile(tileKey);
            });

            // Always issue the callback
            promise.Finally(() => { callback(tileChange); });

            yield return Uxios.WaitForRequest(promise);
        }

        private double widthMeters, heightMeters;
        private CoordinateSystem foundCRS = CoordinateSystem.Undefined;
        private BoundingBox DetermineBoundingBox(TileChange tileChange, MapFilters mapFilters)
        {
            var bottomLeft = new Coordinate(CoordinateSystem.RD, tileChange.X, tileChange.Y, 0);
            var topRight = new Coordinate(CoordinateSystem.RD, tileChange.X + tileSize, tileChange.Y + tileSize, 0);

            // Yes, there is a semicolon missing, this is on purpose because FindCoordinateSystem finds this and not 
            // with the semicolon
            string coordinateSystemAsString = "CRS84";
            if (mapFilters.spatialReference != "CRS:84")
            {
                var splitReferenceCode = mapFilters.spatialReference.Split(':');
                coordinateSystemAsString = splitReferenceCode[0].ToLower() == "epsg"
                    ? splitReferenceCode[^1]
                    : DefaultEpsgCoordinateSystem;
            }

            CoordinateSystem foundCoordinateSystem = CoordinateSystems.FindCoordinateSystem(coordinateSystemAsString);
            foundCRS = foundCoordinateSystem;
            if(foundCoordinateSystem == CoordinateSystem.CRS84)
            {
                Coordinate bl = bottomLeft.Convert(CoordinateSystem.CRS84);
                Coordinate tr = topRight.Convert(CoordinateSystem.CRS84);

                // double minLon = Math.Min(bl.value1, tr.value1);
                // double minLat = Math.Min(bl.value2, tr.value2);
                // double maxLon = Math.Max(bl.value1, tr.value1);
                // double maxLat = Math.Max(bl.value2, tr.value2);

                // bottomLeft = new Coordinate(CoordinateSystem.CRS84, minLon, minLat).Convert(CoordinateSystem.RD);
                // topRight = new Coordinate(CoordinateSystem.CRS84, maxLon, maxLat).Convert(CoordinateSystem.RD);

                // // Bereken breedte en hoogte in meters
                // widthMeters = Math.Abs(topRight.value1 - bottomLeft.value1);
                // heightMeters = Math.Abs(topRight.value2 - bottomLeft.value2);
                // Debug.Log("WIDTH:" + widthMeters + "HEIGHT:" + heightMeters);
                const double earthRadius = 6378137; // meters

                double meanLat = (bl.value2 + tr.value2) / 2.0;
                double latRad = Math.PI * meanLat / 180.0;

                double metersPerDegreeLon = Math.Cos(latRad) * (Math.PI * earthRadius / 180.0);
                double metersPerDegreeLat = Math.PI * earthRadius / 180.0;

                widthMeters = Math.Abs(tr.value1 - bl.value1) * metersPerDegreeLon;
                heightMeters = Math.Abs(tr.value2 - bl.value2) * metersPerDegreeLat;

                // Verhoudingsfactoren tov 1000 meter
                double scaleX = 1000.0 / widthMeters;
                double scaleY = 1000.0 / heightMeters;

                // Originele center in graden
                double centerLon = (bl.value1 + tr.value1) / 2.0;
                double centerLat = (bl.value2 + tr.value2) / 2.0;

                double halfWidthDeg = Math.Abs(tr.value1 - bl.value1) / 2.0 * scaleX;
                double halfHeightDeg = Math.Abs(tr.value2 - bl.value2) / 2.0 * scaleY;

                double minLon = centerLon - halfWidthDeg;
                double maxLon = centerLon + halfWidthDeg;
                double minLat = centerLat - halfHeightDeg;
                double maxLat = centerLat + halfHeightDeg;

                // Update bottomLeft en topRight met geschaalde coördinaten
                bottomLeft = new Coordinate(CoordinateSystem.CRS84, minLon, minLat);
                topRight = new Coordinate(CoordinateSystem.CRS84, maxLon, maxLat);
            }

            var boundingBox = new BoundingBox(bottomLeft, topRight);
            boundingBox.Convert(foundCoordinateSystem);

            return boundingBox;
        }


        private void UpdateDrawOrderForChildren()
        {
            foreach (KeyValuePair<Vector2Int, Tile> tile in tiles)
            {
                if (tile.Value == null || tile.Value.gameObject == null)
                    continue;

                TextureDecalProjector projector = tile.Value.gameObject.GetComponent<TextureDecalProjector>();
                projector.SetPriority(renderIndex);
            }
        }
    }
}