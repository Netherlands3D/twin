using KindMen.Uxios;
using netDxf.Entities;
using Netherlands3D.CartesianTiles;
using Netherlands3D.Coordinates;
using Netherlands3D.Credentials.StoredAuthorization;
using Netherlands3D.Functionalities.Sun;
using Netherlands3D.Twin.FloatingOrigin;
using Netherlands3D.Twin.Utility;
using System;
using System.Collections;
using System.Collections.Generic;
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
                tex.wrapMode = TextureWrapMode.Clamp; //this is important we dont want artefacts at the edges of projections

                if (!tile.gameObject.TryGetComponent<TextureProjectorBase>(out var projector))
                {
                    Destroy(tex);
                    return;
                }

                projector.SetSize(tileSize, tileSize, tileSize);
                projector.gameObject.SetActive(isEnabled);
                projector.SetTexture(tex);

                if (foundCRS == CoordinateSystem.CRS84)
                {
                    //this works for crs84
                    projector.Material.SetVector("_UV00", projectorUVCorners[0]); // BL
                    projector.Material.SetVector("_UV10", projectorUVCorners[1]); // BR
                    projector.Material.SetVector("_UV01", projectorUVCorners[2]); // TL                   
                    projector.Material.SetVector("_UV11", projectorUVCorners[3]); // TR
                }
                else
                {
                    projector.Material.SetVector("_UV00", Vector2.zero);         // LL
                    projector.Material.SetVector("_UV01", new Vector2(1, 0));    // LR
                    projector.Material.SetVector("_UV10", new Vector2(0, 1));    // UL
                    projector.Material.SetVector("_UV11", Vector2.one);          // UR
                }

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
        private Coordinate centerProjectorPosition;
        private double rotationProjector;
        private Vector2[] projectorUVCorners = new Vector2[4];
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

            BoundingBox boundingBox = null;
            CoordinateSystem foundCoordinateSystem = CoordinateSystems.FindCoordinateSystem(mapFilters.spatialReference);
            foundCRS = foundCoordinateSystem;
            if (foundCoordinateSystem == CoordinateSystem.CRS84)
            {
                var bottomRight = new Coordinate(CoordinateSystem.RD, tileChange.X + tileSize, tileChange.Y, 0);
                var topLeft = new Coordinate(CoordinateSystem.RD, tileChange.X, tileChange.Y + tileSize, 0);

                (double, double)[] cornersRD = new (double, double)[4]
                {
                    (bottomLeft.easting,  bottomLeft.northing),   // BL
                    (bottomRight.easting, bottomRight.northing),  // BR
                    (topLeft.easting,     topLeft.northing),        //TL
                    (topRight.easting,    topRight.northing)     // TR
                          
                };

                double minLon = double.MaxValue;
                double maxLon = double.MinValue;
                double minLat = double.MaxValue;
                double maxLat = double.MinValue;

                //lets calculate the width and height of the tile when projected with wgs84/crs84
                Coordinate bl = bottomLeft.Convert(CoordinateSystem.CRS84);
                Coordinate tr = topRight.Convert(CoordinateSystem.CRS84);
                const double earthRadius = 6378137; // meters
                double meanLat = (bl.northing + tr.northing) / 2.0;
                double latRad = Math.PI * meanLat / 180.0;
                double metersPerDegreeLon = Math.Cos(latRad) * (Math.PI * earthRadius / 180.0);
                double metersPerDegreeLat = Math.PI * earthRadius / 180.0;
                widthMeters = Math.Abs(tr.easting - bl.easting) * metersPerDegreeLon;
                heightMeters = Math.Abs(tr.northing - bl.northing) * metersPerDegreeLat;

                //lets calculate the corners and minmax bounds in wgs84
                Coordinate[] corners = new Coordinate[4];
                for (int i = 0; i < 4; i++)
                {
                    Coordinate rdCorner = new Coordinate(CoordinateSystem.RD, cornersRD[i].Item1, cornersRD[i].Item2, 0);
                    corners[i] = rdCorner.Convert(CoordinateSystem.CRS84);
                    if (corners[i].easting < minLon) minLon = corners[i].easting;
                    if (corners[i].easting > maxLon) maxLon = corners[i].easting;
                    if (corners[i].northing < minLat) minLat = corners[i].northing;
                    if (corners[i].northing > maxLat) maxLat = corners[i].northing;
                }

                //the projector rotation
                double drx = corners[2].northing - corners[0].northing;
                double dry = corners[2].easting - corners[0].easting;
                double angleRadians = Math.Atan2(dry, drx);
                double angleDegrees = angleRadians * (180.0 / Math.PI);
                
                //lets calculate a compensated extra area because of the rotation its not a north oriented rectangle and should encapsulate the rotated rectangle
                double rotationRadians = -angleDegrees * Mathf.Deg2Rad;
                double compensation = 1 + Mathf.Sin((float)rotationRadians);
                double scaleX = widthMeters / 1000;
                double scaleY = heightMeters / 1000;
                double aspect = (1000 / scaleY * scaleX) / 1000;

                widthMeters = 1000;
                heightMeters = 1000;

                //calculate the centroid in wgs84
                double centerLon = 0;
                double centerLat = 0;
                for (int i = 0; i < 4; i++)
                {
                    centerLon += corners[i].easting;
                    centerLat += corners[i].northing;
                }
                centerLon /= 4.0;
                centerLat /= 4.0;
                Coordinate centerCoordinate = new Coordinate(CoordinateSystem.CRS84, centerLat, centerLon, 0);
               
                //now we can calculate the 4 corners in uv space in uv coordinates
                double centerU = 0;
                double centerV = 0;
                (double u, double v)[] uvCorners = new (double u, double v)[4];
                for (int i = 0; i < 4; i++)
                {
                    uvCorners[i].u = (corners[i].easting - minLon) / (maxLon - minLon);
                    uvCorners[i].v = (corners[i].northing - minLat) / (maxLat - minLat);
                    centerU += uvCorners[i].u;
                    centerV += uvCorners[i].v;
                }
                centerU /= 4.0;
                centerV /= 4.0;

                for (int i = 0; i < 4; i++)
                {
                    double deltaU = (uvCorners[i].u - centerU) / aspect;
                    double finalU = centerU + deltaU;
                    double deltaV = (uvCorners[i].v - centerV) / 1;
                    double finalV = centerV + deltaV;
                    projectorUVCorners[i] = new Vector2((float)finalU, (float)finalV);
                }
                centerProjectorPosition = centerCoordinate;

                //Coordinate newBL = CalculateScaledBboxCorner(centerCoordinate, bottomLeft, compensation, compensation, CoordinateSystem.RD);
                //Coordinate newTR = CalculateScaledBboxCorner(centerCoordinate, bottomLeft, compensation, compensation, CoordinateSystem.RD);

                double offset = compensation - 1;
                Coordinate newBL = new Coordinate(CoordinateSystem.RD, tileChange.X - tileSize * offset, tileChange.Y, 0);
                Coordinate newTR = new Coordinate(CoordinateSystem.RD, tileChange.X + tileSize + tileSize * offset, tileChange.Y, 0);
                //boundingBox = new BoundingBox(newBL, newTR);
                boundingBox = new BoundingBox(bottomLeft, topRight);
                //boundingBox.Encapsulate(newBL);
                //boundingBox.Encapsulate(newTR);
                testBB = boundingBox;
                boundingBox.Convert(foundCoordinateSystem);
            }
            else
            {
                boundingBox = new BoundingBox(bottomLeft, topRight);
                boundingBox.Convert(foundCoordinateSystem);
            }
            return boundingBox;
        }


        BoundingBox testBB;
#if UNITY_EDITOR
        public void OnDrawGizmos()
        {
            testBB?.Debug(Color.green);
        }
#endif

        private Coordinate CalculateScaledBboxCorner(Coordinate centerBbox, Coordinate corner, double scaleX, double scaleY, CoordinateSystem crs)
        {
            centerBbox = centerBbox.Convert(CoordinateSystem.WGS84);
            corner = corner.Convert(CoordinateSystem.WGS84);

            double deltaLat = corner.northing - centerBbox.northing;
            double deltaLon = corner.easting - centerBbox.easting;
            double scaledDeltaLat = deltaLat * scaleX;
            double scaledDeltaLon = deltaLon * scaleY;
            double scaledLon = centerBbox.easting + scaledDeltaLon;
            double scaledLat = centerBbox.northing + scaledDeltaLat;
            Coordinate result = new Coordinate(CoordinateSystem.WGS84, scaledLat, scaledLon);
            result = result.Convert(crs);
            return result;
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