using KindMen.Uxios;
using netDxf.Entities;
using Netherlands3D.CartesianTiles;
using Netherlands3D.Coordinates;
using Netherlands3D.Credentials.StoredAuthorization;
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

                    if (!tile.gameObject.TryGetComponent<TextureProjectorBase>(out var projector))
                    {
                        Destroy(tex);
                        return;
                    }
                    if (foundCRS == CoordinateSystem.CRS84)
                    {
                        projector.SetSize((float)widthMeters, (float)heightMeters, ProjectorMinDepth);
                        projector.transform.position = centerProjectorPosition.ToUnity();
                    }
                    else
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
        private Coordinate centerProjectorPosition;
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

            CoordinateSystem foundCoordinateSystem = CoordinateSystems.FindCoordinateSystem(mapFilters.spatialReference);
            foundCRS = foundCoordinateSystem;
            if(foundCoordinateSystem == CoordinateSystem.CRS84)
            {
                Coordinate center = new Coordinate(CoordinateSystem.RD, tileChange.X + tileSize * 0.5f, tileChange.Y + tileSize * 0.5f, 0);
                // (double rdx, double rdy) = RdOffsetCalculator.ComputeRdOffset(centerCRS84.value1, centerCRS84.value2);
                //(double lat, double lon) = RDProjection.RDToWGS84(center.value1, center.value2);

                //(double rdx, double rdy) = RDProjection.WGS84ToRD(lat, lon);


                //(var lat, var lon) = RDProjection.RDToWGS84(center.value1, center.value2);
                //(var newX, var newY) = RDProjection.WGS84ToRD(lat, lon);



                centerProjectorPosition = new Coordinate(CoordinateSystem.RDNAP, center.value1, center.value2, 43);
                widthMeters = 1000;
                heightMeters = 1000;

                //var mainShifterPosition = Origin.current.mainShifter.position;
                //Coordinate mainShifterCoordinate = new Coordinate(mainShifterPosition).Convert(CoordinateSystem.RD);

                //Vector3 uPos = RDToUnity(center.value1, center.value2, mainShifterCoordinate.value1, mainShifterCoordinate.value2);
                //Debug.Log(uPos);

                //Coordinate bl = bottomLeft.Convert(CoordinateSystem.CRS84);
                //Coordinate tr = topRight.Convert(CoordinateSystem.CRS84);

                //centerProjectorPosition = new Coordinate(CoordinateSystem.RD, tileChange.X + tileSize * 0.5f, tileChange.Y + tileSize * 0.5f, 0);

                //// double minLon = Math.Min(bl.value1, tr.value1);
                //// double minLat = Math.Min(bl.value2, tr.value2);
                //// double maxLon = Math.Max(bl.value1, tr.value1);
                //// double maxLat = Math.Max(bl.value2, tr.value2);

                //// bottomLeft = new Coordinate(CoordinateSystem.CRS84, minLon, minLat).Convert(CoordinateSystem.RD);
                //// topRight = new Coordinate(CoordinateSystem.CRS84, maxLon, maxLat).Convert(CoordinateSystem.RD);

                //// // Bereken breedte en hoogte in meters
                //// widthMeters = Math.Abs(topRight.value1 - bottomLeft.value1);
                //// heightMeters = Math.Abs(topRight.value2 - bottomLeft.value2);
                //// Debug.Log("WIDTH:" + widthMeters + "HEIGHT:" + heightMeters);
                //const double earthRadius = 6378137; // meters

                //double meanLat = (bl.value2 + tr.value2) / 2.0;
                //double latRad = Math.PI * meanLat / 180.0;

                //double metersPerDegreeLon = Math.Cos(latRad) * (Math.PI * earthRadius / 180.0);
                //double metersPerDegreeLat = Math.PI * earthRadius / 180.0;

                //widthMeters = Math.Abs(tr.value1 - bl.value1) * metersPerDegreeLon;
                //heightMeters = Math.Abs(tr.value2 - bl.value2) * metersPerDegreeLat;

                //// Verhoudingsfactoren tov 1000 meter
                //double scaleX = 1000.0 / widthMeters;
                //double scaleY = 1000.0 / heightMeters;

                //// Originele center in graden
                //double centerLon = (bl.value1 + tr.value1) / 2.0;
                //double centerLat = (bl.value2 + tr.value2) / 2.0;

                //double halfWidthDeg = Math.Abs(tr.value1 - bl.value1) / 2.0 * scaleX;
                //double halfHeightDeg = Math.Abs(tr.value2 - bl.value2) / 2.0 * scaleY;

                //double minLon = centerLon - halfWidthDeg;
                //double maxLon = centerLon + halfWidthDeg;
                //double minLat = centerLat - halfHeightDeg;
                //double maxLat = centerLat + halfHeightDeg;

                //// Update bottomLeft en topRight met geschaalde coördinaten
                //bottomLeft = new Coordinate(CoordinateSystem.CRS84, minLon, minLat);
                //topRight = new Coordinate(CoordinateSystem.CRS84, maxLon, maxLat);
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

        public static class RDProjection
        {
            // Convert RD (Amersfoort) to WGS84 (with Helmert correction)
            public static (double lat, double lon) RDToWGS84(double x, double y)
            {
                double dX = (x - 155000.0) * 1e-5;
                double dY = (y - 463000.0) * 1e-5;

                double phi0 = 52.15517440;
                double lambda0 = 5.38720621;

                double[,] Kpq = new double[,]
                {
            { 0, 1, 3235.65389 }, { 2, 0, -32.58297 }, { 0, 2, -0.24750 }, { 2, 1, -0.84978 },
            { 0, 3, -0.06550 }, { 2, 2, -0.01709 }, { 1, 0, -0.00738 }, { 4, 0, 0.00530 },
            { 2, 3, -0.00039 }, { 4, 1, 0.00033 }, { 1, 1, -0.00012 }, { 5, 0, 0.00026 }
                };

                double[,] Lpq = new double[,]
                {
            { 1, 0, 5260.52916 }, { 1, 1, 105.94684 }, { 1, 2, 2.45656 }, { 3, 0, -0.81885 },
            { 1, 3, 0.05594 }, { 3, 1, -0.05607 }, { 0, 1, 0.01199 }, { 3, 2, -0.00256 },
            { 1, 4, 0.00128 }, { 0, 2, 0.00022 }, { 2, 0, -0.00022 }
                };

                double dPhi = 0, dLambda = 0;

                for (int i = 0; i < Kpq.GetLength(0); i++)
                {
                    int p = (int)Kpq[i, 0];
                    int q = (int)Kpq[i, 1];
                    double coeff = Kpq[i, 2];
                    dPhi += coeff * Math.Pow(dX, p) * Math.Pow(dY, q);
                }

                for (int i = 0; i < Lpq.GetLength(0); i++)
                {
                    int p = (int)Lpq[i, 0];
                    int q = (int)Lpq[i, 1];
                    double coeff = Lpq[i, 2];
                    dLambda += coeff * Math.Pow(dX, p) * Math.Pow(dY, q);
                }

                double latBessel = phi0 + (dPhi / 3600.0);
                double lonBessel = lambda0 + (dLambda / 3600.0);

                return BesselToWGS84(latBessel, lonBessel);
            }

            // Convert WGS84 to RD (via Bessel datum)
            public static (double x, double y) WGS84ToRD(double lat, double lon)
            {
                (double latBessel, double lonBessel) = WGS84ToBessel(lat, lon);

                double phi0 = 52.15517440;
                double lambda0 = 5.38720621;
                double dPhi = (latBessel - phi0) * 3600.0;
                double dLambda = (lonBessel - lambda0) * 3600.0;

                double[,] Rpq = new double[,]
                {
            { 0, 1, 190094.945 }, { 1, 1, -11832.228 }, { 2, 1, -114.221 }, { 0, 3, -32.391 },
            { 1, 0, -0.705 }, { 3, 1, -2.340 }, { 1, 3, -0.608 }, { 0, 2, -0.008 }, { 2, 3, 0.148 }
                };

                double[,] Spq = new double[,]
                {
            { 1, 0, 309056.544 }, { 0, 2, 3638.893 }, { 2, 0, 73.077 }, { 1, 2, -157.984 },
            { 3, 0, 59.788 }, { 0, 1, 0.433 }, { 2, 2, -6.439 }, { 1, 1, -0.032 }, { 0, 4, 0.092 }
                };

                double x = 155000.0;
                double y = 463000.0;

                for (int i = 0; i < Rpq.GetLength(0); i++)
                {
                    int p = (int)Rpq[i, 0];
                    int q = (int)Rpq[i, 1];
                    double coeff = Rpq[i, 2];
                    x += coeff * Math.Pow(dPhi, p) * Math.Pow(dLambda, q);
                }

                for (int i = 0; i < Spq.GetLength(0); i++)
                {
                    int p = (int)Spq[i, 0];
                    int q = (int)Spq[i, 1];
                    double coeff = Spq[i, 2];
                    y += coeff * Math.Pow(dPhi, p) * Math.Pow(dLambda, q);
                }

                return (x, y);
            }

            // Apply Helmert transformation (RD/Bessel → WGS84)
            private static (double lat, double lon) BesselToWGS84(double lat, double lon)
            {
                const double aBessel = 6377397.155;
                const double eBessel = 0.081696831215303;

                double phi = DegreeToRadian(lat);
                double lambda = DegreeToRadian(lon);

                double sinPhi = Math.Sin(phi);
                double cosPhi = Math.Cos(phi);
                double sinLambda = Math.Sin(lambda);
                double cosLambda = Math.Cos(lambda);

                double v = aBessel / Math.Sqrt(1 - Math.Pow(eBessel * sinPhi, 2));

                // Cartesian Bessel coordinates
                double x = v * cosPhi * cosLambda;
                double y = v * cosPhi * sinLambda;
                double z = v * (1 - Math.Pow(eBessel, 2)) * sinPhi;

                // Helmert params from Bessel → WGS84
                double dx = 565.2369;
                double dy = 50.0087;
                double dz = 465.658;
                double ds = -4.0772e-6; // scale
                double rx = DegreeToRadian(0.0000414);
                double ry = DegreeToRadian(0.0000681);
                double rz = DegreeToRadian(0.0002336);

                double x2 = dx + (1 + ds) * (x + rz * y - ry * z);
                double y2 = dy + (1 + ds) * (-rz * x + y + rx * z);
                double z2 = dz + (1 + ds) * (ry * x - rx * y + z);

                // Convert back to lat/lon (WGS84)
                const double aWGS = 6378137.0;
                const double eWGS = 0.0818191910428;

                double p = Math.Sqrt(x2 * x2 + y2 * y2);
                double theta = Math.Atan2(z2 * aWGS, p * (1 - Math.Pow(eWGS, 2)) * aWGS);

                double latWGS = Math.Atan2(z2 + Math.Pow(eWGS, 2) * (1 - Math.Pow(eWGS, 2)) * aWGS * Math.Pow(Math.Sin(theta), 3),
                                           p - Math.Pow(eWGS, 2) * aWGS * Math.Pow(Math.Cos(theta), 3));
                double lonWGS = Math.Atan2(y2, x2);

                return (RadianToDegree(latWGS), RadianToDegree(lonWGS));
            }

            // Inverse Helmert: WGS84 → Bessel
            private static (double lat, double lon) WGS84ToBessel(double lat, double lon)
            {
                const double aWGS = 6378137.0;
                const double eWGS = 0.0818191910428;

                double phi = DegreeToRadian(lat);
                double lambda = DegreeToRadian(lon);

                double sinPhi = Math.Sin(phi);
                double cosPhi = Math.Cos(phi);
                double sinLambda = Math.Sin(lambda);
                double cosLambda = Math.Cos(lambda);

                double v = aWGS / Math.Sqrt(1 - Math.Pow(eWGS * sinPhi, 2));

                double x = v * cosPhi * cosLambda;
                double y = v * cosPhi * sinLambda;
                double z = v * (1 - Math.Pow(eWGS, 2)) * sinPhi;

                // Inverse Helmert: WGS84 → Bessel
                double dx = -565.2369;
                double dy = -50.0087;
                double dz = -465.658;
                double ds = 4.0772e-6;
                double rx = -DegreeToRadian(0.0000414);
                double ry = -DegreeToRadian(0.0000681);
                double rz = -DegreeToRadian(0.0002336);

                double x2 = dx + (1 + ds) * (x + rz * y - ry * z);
                double y2 = dy + (1 + ds) * (-rz * x + y + rx * z);
                double z2 = dz + (1 + ds) * (ry * x - rx * y + z);

                // Convert back to lat/lon (Bessel)
                const double aBessel = 6377397.155;
                const double eBessel = 0.081696831215303;

                double p = Math.Sqrt(x2 * x2 + y2 * y2);
                double theta = Math.Atan2(z2 * aBessel, p * (1 - Math.Pow(eBessel, 2)) * aBessel);

                double latBessel = Math.Atan2(z2 + Math.Pow(eBessel, 2) * (1 - Math.Pow(eBessel, 2)) * aBessel * Math.Pow(Math.Sin(theta), 3),
                                              p - Math.Pow(eBessel, 2) * aBessel * Math.Pow(Math.Cos(theta), 3));
                double lonBessel = Math.Atan2(y2, x2);

                return (RadianToDegree(latBessel), RadianToDegree(lonBessel));
            }

            private static double DegreeToRadian(double degree) => degree * Math.PI / 180.0;
            private static double RadianToDegree(double radian) => radian * 180.0 / Math.PI;
        }
    }
}