/*
*  Copyright (C) X Gemeente
*                X Amsterdam
*                X Economic Services Departments
*
*  Licensed under the EUPL, Version 1.2 or later (the "License");
*  You may not use this work except in compliance with the License.
*  You may obtain a copy of the License at:
*
*    https://github.com/Amsterdam/3DAmsterdam/blob/master/LICENSE.txt
*
*  Unless required by applicable law or agreed to in writing, software
*  distributed under the License is distributed on an "AS IS" basis,
*  WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or
*  implied. See the License for the specific language governing
*  permissions and limitations under the License.
*/

using System;

namespace Netherlands3D.Coordinates
{
    /// <summary>
	/// CRS converter for EPSG:3857 (WGS 84 / Pseudo-Mercator -- Spherical Mercator, Google Maps, OpenStreetMap, Bing, ArcGIS, ESRI).
    /// </summary>
    /// <see href="https://epsg.io/3857">https://epsg.io/3857</see>
    public static class EPSG3857
    {
        public static Coordinate ConvertTo(Coordinate coordinate, int targetCrs)
        {
            if (coordinate.CoordinateSystem != 3857)
            {
                throw new ArgumentOutOfRangeException(
                    $"Invalid coordinate received, this class cannot convert CRS {coordinate.CoordinateSystem}"
                );
            }

            if(targetCrs == 4326)
            {
                return ToEPSG4326(coordinate);
            }

            throw new ArgumentOutOfRangeException(
                $"Conversion between CRS {coordinate.CoordinateSystem} and {targetCrs} is not yet supported"
            );
        }

        // See: https://developers.auravant.com/en/blog/2022/09/09/post-3/#epsg3857-to-epsg4326
        private static Coordinate ToEPSG4326(Coordinate coordinate)
        {
            var x = coordinate.value1;
            var y = coordinate.value2;
            x = (x * 180d) / 20037508.34d;
            y = (y * 180d) / 20037508.34d;
            y = (Math.Atan(Math.Exp(y * (Math.PI / 180d))) * 360d) / Math.PI - 90d;
            
            return new Coordinate(CoordinateSystem.WGS84_LatLon, x, y, coordinate.value3);
        }
    }
}
