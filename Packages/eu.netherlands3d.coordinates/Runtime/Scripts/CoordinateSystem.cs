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
using System.Collections.Generic;
using UnityEngine;

namespace Netherlands3D.Coordinates
{
    
    internal enum CoordinateSystemGroup
    {
        unity = -1,
        None = 0,
        RD = 1,
        WGS84 =2,
        ETRS89 = 3

    }
    public enum CoordinateSystemType
    {
        Projected,
        Geographic,
        Geocentric,
        None
    }
    [Serializable]
   public enum CoordinateSystem
    {
        Undefined = 0,
        WGS84_PseudoMercator = 3857,
        ETRS89_LatLon = 4258,
        WGS84_LatLon = 4326, 
        ETRS89_ECEF = 4936,
        ETRS89_LatLonHeight = 4937,
        WGS84_ECEF = 4978,
        WGS84_NAP_ECEF = 3,
        WGS84_LatLonHeight = 4979,
        RDNAP = 2,
        EPSG_7415 = 7415,
        CRS84,

        RD = 28992,
        EPSG_3857 = WGS84_PseudoMercator,
        EPSG_4936 = ETRS89_ECEF,

        WGS84 = WGS84_LatLonHeight,
    }

    public static class CoordinateSystems
    {
         internal static Dictionary<CoordinateSystem, CoordinateSystemOperation> operators = new Dictionary<CoordinateSystem, CoordinateSystemOperation> {
            { CoordinateSystem.RDNAP, new RDNAP_Operations() } ,
            { CoordinateSystem.EPSG_7415, new RDNAP_Operations() },
            { CoordinateSystem.RD, new RD_Operations() },
            { CoordinateSystem.WGS84_ECEF, new WGS84_ECEF_Operations() },
            { CoordinateSystem.WGS84_LatLonHeight, new WGS84_LatLonHeight_Operations() },
            { CoordinateSystem.WGS84_LatLon, new WGS84_LatLon_Operations() },
            { CoordinateSystem.WGS84_PseudoMercator, new WGS84_PseudoMercator_Operations() },
            { CoordinateSystem.ETRS89_ECEF, new ETRS89_ECEF_Operations() },
            { CoordinateSystem.ETRS89_LatLon, new ETRS89_LatLon_Operations() },
            { CoordinateSystem.ETRS89_LatLonHeight, new ETRS89_LatLonHeight_Operations() },
            { CoordinateSystem.CRS84, new CRS84_LonLat_Operations() },
             { CoordinateSystem.WGS84_NAP_ECEF, new WGS84_NAP_ECEF_Operations()},
            { CoordinateSystem.Undefined, new Undefined_Operations() }

        };

        static CoordinateSystem _connectedCoordinateSystem;
        
        public static Quaternion connectedCRSToUnityUp;
        public static Vector3WGS wgsAtUp;

                
        public static bool FindCoordinateSystem(string name, out CoordinateSystem result)
        {
            result = FindCoordinateSystem(name);            
            return result != CoordinateSystem.Undefined;
        }

        public static CoordinateSystem FindCoordinateSystem(string name)
        {
            //we remove the : in case it is present for CRS84(not an EPSG standard) because 1.the code of CRS84Converter is without the :
            //and 2.we want to avoid using simply looking for 84 in the code as this could conflict with CRS codes that have 84 as a substring of their code
            if (name.Contains("CRS:84"))
                name = name.Replace("CRS:84", "CRS84");

            foreach (var kvp in operators)
            {
                if (name.Contains(kvp.Value.Code()))
                {
                    return kvp.Key;
                }
            }
            return CoordinateSystem.Undefined;
        }        

        public static CoordinateSystem connectedCoordinateSystem
        {
            get { return _connectedCoordinateSystem; }
            set {
                _connectedCoordinateSystem = value;
                Debug.Log("coordinateSystem set: " + value.ToString());
                //if (operators.ContainsKey((CoordinateSystem)_coordinateAtOrigin.CoordinateSystem))
                //{
                //    SetOrigin(_coordinateAtOrigin);
                //}
            }
        }
       
        public static void SetOrigin(Coordinate coordinateAtUnityOrigin)
        {
            
            _coordinateAtOrigin = coordinateAtUnityOrigin.Convert(_connectedCoordinateSystem);
            CoordinateSystemOperation myConverter = CoordinateSystems.operators[_connectedCoordinateSystem];

            // Up-direction in the coordinateSystem at the coordinate
            wgsAtUp = myConverter.LocalUpDirection(_coordinateAtOrigin);
            

            /// we want to find out how much we have to rotate to make the localUpDirection align with the orientation of the coordinateSystem
            /// this is the amount we have to rotate the coordinateSystem to align with the UnityAxes.
            /// First we calculate the difference in longitude between de localUP at the coordinate and the orientation of the coordinateSystem 
            Quaternion rotationToEast = Quaternion.AngleAxis((float)wgsAtUp.lon- (float)myConverter.Orientation().lon, Vector3.up);
            if (myConverter.GetCoordinateSystemType() == CoordinateSystemType.Geocentric)
            {
                //rotate -90 degrees around the up-axis, to make sure east is in the X-direction;
                rotationToEast = rotationToEast * Quaternion.AngleAxis(-90, Vector3.up);
            }
            /// Now we calculate the difference in lattitude between de localUP at the coordinate and the orientation of the coordinateSystem  
            Quaternion rotationToFlat = Quaternion.AngleAxis((float)myConverter.Orientation().lat - (float)wgsAtUp.lat, Vector3.right);
            /// when we apply both rotations, we get the rotation required to get the coordinateSystem pointing Up and North at the Unity-Origin
                connectedCRSToUnityUp = rotationToFlat * rotationToEast;
            
            
            
        }

        static Coordinate _coordinateAtOrigin = new Coordinate(Vector3.zero);

        public static Coordinate CoordinateAtUnityOrigin => _coordinateAtOrigin;

        public static bool TryFindValidCoordinates(double value1, double value2, double value3, out List<Coordinate> results)
        {
            results = new List<Coordinate>(operators.Count);
            foreach (var kvp in operators)
            {
                if (kvp.Key == CoordinateSystem.Undefined)
                    continue;
                
                if(kvp.Value.AxisCount() != 3) //we only want 3d coordinate systems 
                    continue;
                
                var potentialCoordinate = new Coordinate(kvp.Key, value1, value2, value3);
                if (potentialCoordinate.IsValid())
                {
                    if(potentialCoordinate.Convert(connectedCoordinateSystem).IsValid()) 
                        results.Add(potentialCoordinate);
                }
            }

            return results.Count > 0;
        }
        
        public static bool TryFindValidCoordinates(double value1, double value2, out List<Coordinate> results)
        {
            results = new List<Coordinate>(operators.Count);
            foreach (var kvp in operators)
            {
                if (kvp.Key == CoordinateSystem.Undefined)
                    continue;
                
                if(kvp.Value.AxisCount() != 2) //we only want 3d coordinate systems 
                    continue;
                
                var potentialCoordinate = new Coordinate(kvp.Key, value1, value2);
                if (potentialCoordinate.IsValid())
                {
                    if(potentialCoordinate.Convert(connectedCoordinateSystem).IsValid()) 
                        results.Add(potentialCoordinate);
                }
            }

            return results.Count > 0;
        }

        public static CoordinateSystem To3D(CoordinateSystem crs)
        {
            var converter = operators[crs];
            if (converter.AxisCount() == 3)
                return crs;
            
            var group = converter.GetCoordinateSystemGroup();
            if (group == CoordinateSystemGroup.None)
            {
                Debug.LogError("Could not find 3D equivalent of crs: " + crs + " since it has no CoordinateSystemGroup");
                return crs;
            }
            
            foreach (var kvp in operators)
            {
                var c = kvp.Value;
                if(c.GetCoordinateSystemType() == CoordinateSystemType.Geocentric) // Geocentric CRS has no 2D equivalent
                    continue;
                
                if (c.GetCoordinateSystemGroup() == group)
                {
                    if (c.AxisCount() == 3)
                        return kvp.Key;
                }
            }
            Debug.LogError("Could not find 3D equivalent of crs: " + crs);
            return crs;
        }
        
        public static CoordinateSystem To2D(CoordinateSystem crs)
        {
            var converter = operators[crs];
            if (converter.GetCoordinateSystemType() == CoordinateSystemType.Geocentric) // Geocentric CRS has no 2D equivalent
            {
                Debug.LogError("Geocentric crs has no 2D equivalent: " + crs);
                return crs;
            }
            
            if (converter.AxisCount() == 2)
                return crs;
            
            var group = converter.GetCoordinateSystemGroup();
            if (group == CoordinateSystemGroup.None)
            {
                Debug.LogError("Could not find 3D equivalent of crs: " + crs + " since it has no CoordinateSystemGroup");
                return crs;
            }
            
            foreach (var kvp in operators)
            {
                var c = kvp.Value;
                if(c.GetCoordinateSystemType() == CoordinateSystemType.Geocentric) // Geocentric CRS has no 2D equivalent
                    continue;
                
                if (c.GetCoordinateSystemGroup() == group)
                {
                    if (c.AxisCount() == 2)
                        return kvp.Key;
                }
            }
            Debug.LogError("Could not find 3D equivalent of crs: " + crs);
            return crs;
        }

        public static CoordinateSystemType getCoordinateSystemType(CoordinateSystem crs)
        {
            var converter = operators[crs];
            return converter.GetCoordinateSystemType();
        }
    }

}
