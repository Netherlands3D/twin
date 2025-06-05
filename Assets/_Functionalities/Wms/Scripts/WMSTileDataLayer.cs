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
                    //{
                        
                    //    projector.transform.position = projectorBoundsMin.ToUnity();
                    //    projector.SetSize((float)widthMeters, (float)heightMeters, ProjectorMinDepth);
                    //}
                    //else
                    {
                        projector.SetSize(tileSize, tileSize, tileSize);
                    }


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
        private Coordinate centerProjectorPosition, projectorBoundsMin;
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
            CoordinateSystem foundCoordinateSystem = CoordinateSystems.FindCoordinateSystem(coordinateSystemAsString);
            foundCRS = foundCoordinateSystem;
            if(foundCRS == CoordinateSystem.CRS84)
            {
                bottomLeft = RDToCRS84WithHelmert(bottomLeft);
                topRight = RDToCRS84WithHelmert(topRight);


                //Coordinate centerInRD = new Coordinate(CoordinateSystem.RD, tileChange.X + tileSize * 0.5f, tileChange.Y + tileSize * 0.5f, 0);
                ////Coordinate centerInCRS84 = RDToCRS84WithHelmert(centerInRD);

                ////// Step 2: Calculate the bounding box dimensions in CRS:84
                ////double halfTileSizeInDegrees = (double)500 / 111320; // Approximate conversion from meters to degrees
                ////bottomLeft = new Coordinate(CoordinateSystem.CRS84, centerInCRS84.easting - halfTileSizeInDegrees, centerInCRS84.northing - halfTileSizeInDegrees);
                ////topRight = new Coordinate(CoordinateSystem.CRS84, centerInCRS84.easting + halfTileSizeInDegrees, centerInCRS84.northing + halfTileSizeInDegrees);



                //Coordinate center = centerInRD.Convert(CoordinateSystem.WGS84_PseudoMercator);


                //var min = new Coordinate(CoordinateSystem.WGS84_PseudoMercator, bottomLeft.value1, bottomLeft.value2, 0);
                //var max = new Coordinate(CoordinateSystem.WGS84_PseudoMercator, topRight.value1, topRight.value2, 0);

                //var centerDistance = (max - min).ToVector3() * 0.5f;
                //var tilecenter = new Coordinate(
                //    min.CoordinateSystem,
                //    min.value1 + centerDistance.x,
                //    min.value2 + centerDistance.y,
                //    min.value3 + centerDistance.z
                //);

                //var minBoundRd = min.Convert(CoordinateSystem.RD);
                //var maxBoundRd = max.Convert(CoordinateSystem.RD);
                //widthMeters = maxBoundRd.easting - minBoundRd.easting;
                //heightMeters = maxBoundRd.northing - minBoundRd.northing;
                //projectorBoundsMin = min;

            }
            boundingBox = new BoundingBox(bottomLeft, topRight);
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


        private Coordinate RDToCRS84WithHelmert(Coordinate rdCoord)
        {
            // 1. Convert RD XY to Amersfoort geodetic (lat/lon) - you need full formulas here
            // For simplicity assume you have a method:
            var amersfoortLatLon = RDToGeodetic(rdCoord.easting, rdCoord.northing);

            // 2. Convert geodetic Amersfoort (Bessel ellipsoid) lat/lon to ECEF (Bessel)
            GeodeticToECEF(amersfoortLatLon.lat, amersfoortLatLon.lon, 0, out double Xb, out double Yb, out double Zb, ellipsoid: "bessel");

            // 3. Apply inverse Helmert transformation (Amersfoort -> WGS84)
            ApplyInverseHelmert(Xb, Yb, Zb, out double Xw, out double Yw, out double Zw);

            // 4. Convert ECEF WGS84 XYZ back to geodetic WGS84 lat/lon
            var wgs84LatLon = ECEFToGeodetic(Xw, Yw, Zw, ellipsoid: "wgs84");

            return new Coordinate(CoordinateSystem.CRS84, wgs84LatLon.lon, wgs84LatLon.lat);
        }

        public static (double lat, double lon) RDToGeodetic(double x, double y)
        {
            // Constants from official RD New inverse formulas
            double X0 = 155000.0;  // RD X false easting
            double Y0 = 463000.0;  // RD Y false northing
            double phi0 = 52.15517440;  // Reference latitude in degrees (Amersfoort)
            double lambda0 = 5.38720621; // Reference longitude in degrees (Amersfoort)

            // Normalize coordinates relative to false origin
            double dx = (x - X0) * 1e-5; // scale to 10^-5 meters
            double dy = (y - Y0) * 1e-5;

            // Coefficients for latitude (phi) polynomial (in seconds of arc)
            int[] Kp = { 0, 1, 2, 0, 1, 3, 0, 1, 3, 0, 1, 3, 0, 2, 5 };
            int[] Lp = { 1, 0, 0, 2, 1, 0, 3, 3, 1, 5, 1, 0, 4, 4, 0 };
            double[] Rp = {
                3235.65389,
                -32.58297,
                -0.2475,
                -0.84978,
                -0.0655,
                -0.01709,
                -0.00738,
                0.0053,
                -0.00039,
                0.00033,
                -0.00012,
                -0.00012,
                0.00011,
                -0.00011,
                -0.00005
            };

            // Coefficients for longitude (lambda) polynomial (in seconds of arc)
            int[] Kl = { 1, 1, 1, 3, 0, 2, 5, 1, 4, 0, 2, 5, 0, 4, 1 };
            int[] Ll = { 0, 1, 2, 0, 1, 1, 0, 3, 1, 4, 0, 0, 5, 5, 4 };
            double[] Rl = {
                5260.52916,
                105.94684,
                2.45656,
                -0.81885,
                0.05594,
                -0.05607,
                0.01199,
                -0.00256,
                0.00128,
                0.00022,
                -0.00022,
                0.00026,
                -0.00022,
                0.00026,
                -0.00022
            };

            // Calculate latitude in seconds of arc
            double phiSec = 0;
            for (int i = 0; i < Rp.Length; i++)
            {
                phiSec += Rp[i] * Math.Pow(dx, Kp[i]) * Math.Pow(dy, Lp[i]);
            }

            // Calculate longitude in seconds of arc
            double lambdaSec = 0;
            for (int i = 0; i < Rl.Length; i++)
            {
                lambdaSec += Rl[i] * Math.Pow(dx, Kl[i]) * Math.Pow(dy, Ll[i]);
            }

            // Convert from seconds of arc to degrees
            double phi = phi0 + (phiSec / 3600.0);
            double lambda = lambda0 + (lambdaSec / 3600.0);

            return (phi, lambda);
        }

        public static void GeodeticToECEF(double latDeg, double lonDeg, double h, out double X, out double Y, out double Z, string ellipsoid = "wgs84")
        {
            // Select ellipsoid parameters
            double a, f;
            if (ellipsoid.ToLower() == "bessel")
            {
                a = 6377397.155;          // semi-major axis in meters
                f = 1.0 / 299.1528128;    // flattening
            }
            else // default to WGS84
            {
                a = 6378137.0;            // semi-major axis in meters
                f = 1.0 / 298.257223563;  // flattening
            }

            double lat = latDeg * Math.PI / 180.0;
            double lon = lonDeg * Math.PI / 180.0;

            double e2 = 2 * f - f * f; // eccentricity squared

            // Radius of curvature in the prime vertical
            double N = a / Math.Sqrt(1 - e2 * Math.Sin(lat) * Math.Sin(lat));

            // Calculate ECEF coordinates
            X = (N + h) * Math.Cos(lat) * Math.Cos(lon);
            Y = (N + h) * Math.Cos(lat) * Math.Sin(lon);
            Z = (N * (1 - e2) + h) * Math.Sin(lat);
        }

        public static (double lat, double lon, double height) ECEFToGeodetic(double X, double Y, double Z, string ellipsoid = "wgs84")
        {
            // Ellipsoid parameters
            double a, f;
            if (ellipsoid.ToLower() == "bessel")
            {
                a = 6377397.155;           // semi-major axis (meters)
                f = 1.0 / 299.1528128;     // flattening
            }
            else // default WGS84
            {
                a = 6378137.0;             // semi-major axis (meters)
                f = 1.0 / 298.257223563;   // flattening
            }

            double b = a * (1 - f);       // semi-minor axis
            double e2 = 2 * f - f * f;    // first eccentricity squared
            double ep2 = (a * a - b * b) / (b * b); // second eccentricity squared

            double p = Math.Sqrt(X * X + Y * Y);
            double theta = Math.Atan2(Z * a, p * b);

            // Latitude calculation
            double sinTheta = Math.Sin(theta);
            double cosTheta = Math.Cos(theta);

            double lat = Math.Atan2(Z + ep2 * b * sinTheta * sinTheta * sinTheta,
                                    p - e2 * a * cosTheta * cosTheta * cosTheta);

            // Longitude calculation
            double lon = Math.Atan2(Y, X);

            // Radius of curvature in the prime vertical
            double N = a / Math.Sqrt(1 - e2 * Math.Sin(lat) * Math.Sin(lat));

            // Height calculation
            double h = p / Math.Cos(lat) - N;

            // Convert radians to degrees
            double latDeg = lat * 180.0 / Math.PI;
            double lonDeg = lon * 180.0 / Math.PI;

            return (latDeg, lonDeg, h);
        }

        public static void ApplyInverseHelmert(double Xa, double Ya, double Za, out double Xw, out double Yw, out double Zw)
        {
            // Helmert parameters Amersfoort->WGS84 (positive direction)
            double Tx = 593.0248;
            double Ty = 26.0037;
            double Tz = 478.7534;

            double Rx_arcsec = 1.9342;
            double Ry_arcsec = -1.6677;
            double Rz_arcsec = 9.1019;

            double s_ppm = 4.0725;

            // Convert rotations from arcseconds to radians
            double secToRad = (Math.PI / 180.0) / 3600.0;
            double Rx = Rx_arcsec * secToRad;
            double Ry = Ry_arcsec * secToRad;
            double Rz = Rz_arcsec * secToRad;

            // Convert scale from ppm to scale factor
            double s = s_ppm * 1e-6;

            // Since we want the inverse transform (WGS84 -> Amersfoort is forward),
            // invert signs of translation, rotation and scale
            double invTx = -Tx;
            double invTy = -Ty;
            double invTz = -Tz;
            double invRx = -Rx;
            double invRy = -Ry;
            double invRz = -Rz;
            double invS = -s;

            // Apply inverse Helmert transform
            Xw = invTx + (1 + invS) * Xa + invRz * Ya - invRy * Za;
            Yw = invTy - invRz * Xa + (1 + invS) * Ya + invRx * Za;
            Zw = invTz + invRy * Xa - invRx * Ya + (1 + invS) * Za;
        }
    }
}