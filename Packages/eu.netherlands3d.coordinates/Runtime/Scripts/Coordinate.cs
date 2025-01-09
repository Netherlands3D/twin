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
using UnityEngine;
#if NEWTONSOFT
using Newtonsoft.Json;
#endif

namespace Netherlands3D.Coordinates
{
    [Serializable]
    public struct Coordinate
    {
        /// <summary>
        /// EPSG Code defining which Coordinate Reference System (CRS) the provided points relate to.
        /// </summary>
        /// <remarks>
        /// The CoordinateSystem is defined as an int and not as CoordinateSystem enum so that third-parties can
        /// add their own EPSG conversions that are not (yet) included in the enum.
        /// </remarks>
        ///
#if NEWTONSOFT
        [JsonProperty]
#endif
        [SerializeField]
        private int coordinateSystem;
        public int CoordinateSystem => coordinateSystem;

        /// <summary>
        /// Array representing all points for this coordinate.
        ///
        /// Since some coordinate only feature 2 points and some 3, and because coordinate system uses a different
        /// unit and meaning for a point; we have chosen to abstract this into an array with either 2 or 3 points.
        /// </summary>
        /// 
        [HideInInspector]
        public double extraLongitudeRotation;
        [HideInInspector]
        public double extraLattitudeRotation;

        [Obsolete("deprecated convert to x y z")]
        public double[] Points
        {
            get
            {
                double[] points = new double[PointsLength];
                points[0] = x;
                points[1] = y;
                if (PointsLength > 2)
                    points[2] = z;
                return points;
            }
        }

        public double x;
        public double y;
        public double z;
        public int PointsLength;

        private CoordinateSystemOperation converter;

#if NEWTONSOFT
        [JsonIgnore]
#endif
        public double easting
        {
            get
            {
                if (converter == null)
                {
                    converter = CoordinateSystems.operators[(CoordinateSystem)CoordinateSystem];
                }
                //return Points[converter.EastingIndex()]; 
                switch (converter.EastingIndex())
                {
                    case 0: return x;
                    case 1: return y;
                    case 2: return z;
                    default: return x;
                }
            }
            set
            {
                if (converter == null)
                {
                    converter = CoordinateSystems.operators[(CoordinateSystem)CoordinateSystem];
                }
                //Points[converter.EastingIndex()] = value; 
                switch (converter.EastingIndex())
                {
                    case 0: x = value; break;
                    case 1: y = value; break;
                    case 2: z = value; break;
                }
            }
        }
#if NEWTONSOFT
        [JsonIgnore]
#endif
        public double northing
        {
            get
            {
                if (converter == null)
                {
                    converter = CoordinateSystems.operators[(CoordinateSystem)CoordinateSystem];
                }
                //return Points[converter.NorthingIndex()]; 
                switch (converter.NorthingIndex())
                {
                    case 0: return x;
                    case 1: return y;
                    case 2: return z;
                    default: return x;
                }
            }
            set
            {
                if (converter == null)
                {
                    converter = CoordinateSystems.operators[(CoordinateSystem)CoordinateSystem];
                }
                //Points[converter.NorthingIndex()] = value; 
                switch (converter.NorthingIndex())
                {
                    case 0: x = value; break;
                    case 1: y = value; break;
                    case 2: z = value; break;
                }
            }
        }
#if NEWTONSOFT
        [JsonIgnore]
#endif
        public double height
        {
            get
            {
                //if (Points.Length > 2)
                //{
                //    return Points[2];
                //}
                //return 0;
                return PointsLength > 2 ? y : 0;
            }
            set
            {
                //if (Points.Length > 2)
                //{
                //    Points[2] = value;
                //}
                if (PointsLength > 2)
                    y = value;
            }
        }

        [Obsolete("deprecated convert to x y z")]
        public Coordinate(CoordinateSystem coordinateSystem, params double[] points)
        {
            converter = null;
            this.coordinateSystem = (int)coordinateSystem;
            //Points = points;
            x = points[0];
            y = points.Length > 1 ? points[1] : 0;
            z = points.Length > 2 ? points[2] : 0;
            PointsLength = points.Length;
            extraLongitudeRotation = 0;
            extraLattitudeRotation = 0;
        }

        public Coordinate(CoordinateSystem coordinateSystem, double x, double y)
        {
            converter = null;
            this.coordinateSystem = (int)coordinateSystem;
            //Points = points;
            this.x = x;
            this.y = y;
            this.z = 0;
            PointsLength = 2;
            extraLongitudeRotation = 0;
            extraLattitudeRotation = 0;
        }

        public Coordinate(CoordinateSystem coordinateSystem, double x, double y, double z)
        {
            converter = null;
            this.coordinateSystem = (int)coordinateSystem;
            //Points = points;
            this.x = x;
            this.y = y;
            this.z = z;
            PointsLength = 3;
            extraLongitudeRotation = 0;
            extraLattitudeRotation = 0;
        }

#if NEWTONSOFT
        [JsonConstructor]
#endif
        [Obsolete("deprecated convert to x y z")]
        public Coordinate(int coordinateSystem, double[] Points, double extraLongitudeRotation, double extraLatitudeRotation)
        {
            converter = null;
            this.coordinateSystem = coordinateSystem;
            //this.Points = Points;
            x = Points[0];
            y = Points.Length > 1 ? Points[1] : 0;
            z = Points.Length > 2 ? Points[2] : 0;
            PointsLength = Points.Length;
            this.extraLongitudeRotation = extraLongitudeRotation;
            this.extraLattitudeRotation = extraLatitudeRotation;
        }

        public Coordinate(int coordinateSystem, double x, double y, double extraLongitudeRotation, double extraLatitudeRotation)
        {
            converter = null;
            this.coordinateSystem = coordinateSystem;
            //this.Points = Points;
            this.x = x;
            this.y = y;
            this.z = 0;
            PointsLength = 2;
            this.extraLongitudeRotation = extraLongitudeRotation;
            this.extraLattitudeRotation = extraLatitudeRotation;
        }

        public Coordinate(int coordinateSystem, double x, double y, double z, double extraLongitudeRotation, double extraLatitudeRotation)
        {
            converter = null;
            this.coordinateSystem = coordinateSystem;
            //this.Points = Points;
            this.x = x;
            this.y = y;
            this.z = z;
            PointsLength = 3;
            this.extraLongitudeRotation = extraLongitudeRotation;
            this.extraLattitudeRotation = extraLatitudeRotation;
        }

        [Obsolete("deprecated convert to x y z")]
        public Coordinate(int coordinateSystem, params double[] points)
        {
            converter = null;
            this.coordinateSystem = coordinateSystem;
            //Points = points;
            x = points[0];
            y = points.Length > 1 ? points[1] : 0;
            z = points.Length > 2 ? points[2] : 0;
            PointsLength = points.Length;
            extraLongitudeRotation = 0;
            extraLattitudeRotation = 0;
        }

        public Coordinate(int coordinateSystem, double x, double y)
        {
            converter = null;
            this.coordinateSystem = coordinateSystem;
            //Points = points;
            this.x = x;
            this.y = y;
            this.z = 0;
            PointsLength = 2;
            extraLongitudeRotation = 0;
            extraLattitudeRotation = 0;
        }

        public Coordinate(int coordinateSystem, double x, double y, double z)
        {
            converter = null;
            this.coordinateSystem = coordinateSystem;
            //Points = points;
            this.x = x;
            this.y = y;
            this.z = z;
            PointsLength = 3;
            extraLongitudeRotation = 0;
            extraLattitudeRotation = 0;
        }

        public Coordinate(Vector3 unityPosition)
        {
            converter = CoordinateSystems.operators[CoordinateSystems.connectedCoordinateSystem];
            extraLattitudeRotation = 0;
            extraLongitudeRotation = 0;
            Vector3 unrotatedRelativePosition = Quaternion.Inverse(CoordinateSystems.connectedCRSToUnityUp) * unityPosition;

            if (CoordinateSystems.operators[CoordinateSystems.connectedCoordinateSystem].GetCoordinateSystemType() == CoordinateSystemType.Geocentric)
            {
                //unity.x = -deltaX;
                //unity.y = deltaZ;
                //unity.z = -deltaY;

                //Points = new double[3] { -unrotatedRelativePosition.x, -unrotatedRelativePosition.z, unrotatedRelativePosition.y };
                x = -unrotatedRelativePosition.x;
                y = -unrotatedRelativePosition.z;
                z = unrotatedRelativePosition.y;
            }
            else
            {
                //cartesian X = unity X;
                //cartesian Y = unity Z;
                //cartesian Z = unity Y;

                //Points = new double[3] { unrotatedRelativePosition.x, unrotatedRelativePosition.z, unrotatedRelativePosition.y };
                x = unrotatedRelativePosition.x;
                y = unrotatedRelativePosition.z;
                z = unrotatedRelativePosition.y;
            }
            coordinateSystem = (int)CoordinateSystems.connectedCoordinateSystem;          
            PointsLength = 3;
            //Points = (CoordinateSystems.CoordinateAtUnityOrigin + new Coordinate(CoordinateSystem, Points)).Points;
            Coordinate newCoordinate = CoordinateSystems.CoordinateAtUnityOrigin + new Coordinate(CoordinateSystem, x, y, z);            
            x = newCoordinate.x;
            y = newCoordinate.y;
            z = newCoordinate.z;
        }

        //public Coordinate(CoordinateSystem coordinateSystem)
        //{
        //    converter = CoordinateSystems.operators[coordinateSystem];
        //    Points = new double[converter.AxisCount()];
        //    this.coordinateSystem = (int)coordinateSystem;
        //    extraLongitudeRotation = 0;
        //    extraLattitudeRotation = 0;
        //}

        public Coordinate(CoordinateSystem coordinateSystem)
        {
            converter = CoordinateSystems.operators[coordinateSystem];
            //Points = new double[converter.AxisCount()];
            PointsLength = converter.AxisCount();
            x = 0;
            y = 0;
            z = 0;
            this.coordinateSystem = (int)coordinateSystem;
            extraLongitudeRotation = 0;
            extraLattitudeRotation = 0;
        }

        //public static Coordinate operator +(Coordinate a, Coordinate b)
        //{
        //    int maxcoordinatecount = a.Points.Length;
        //    int mincoordinatecount = b.Points.Length;
        //    Coordinate longestCoordainte = a;
        //    if (b.Points.Length > maxcoordinatecount)
        //    {
        //        maxcoordinatecount = b.Points.Length;
        //        mincoordinatecount = a.Points.Length;
        //        longestCoordainte = b;
        //    }
        //    double[] points = new double[maxcoordinatecount];
        //    for (int i = 0; i < mincoordinatecount; i++)
        //    {
        //        points[i] = a.Points[i] + b.Points[i];
        //    }
        //    for (int i = mincoordinatecount; i < maxcoordinatecount; i++)
        //    {
        //        points[i] = longestCoordainte.Points[i];
        //    }
        //    return new Coordinate(a.CoordinateSystem, points);
        //}

        public static Coordinate operator +(Coordinate a, Coordinate b)
        {
            int maxcoordinatecount = a.PointsLength;
            int mincoordinatecount = b.PointsLength;
            Coordinate longestCoordainte = a;
            if (b.PointsLength > maxcoordinatecount)
            {
                maxcoordinatecount = b.PointsLength;
                mincoordinatecount = a.PointsLength;
                longestCoordainte = b;
            }
            double newx = 0;
            double newy = 0;
            double newz = 0;
            for (int i = 0; i < mincoordinatecount; i++)
            {
                switch (i)
                {
                    case 0: newx = a.x + b.x; break;
                    case 1: newy = a.y + b.y; break;
                    case 2: newz = a.z + b.z; break;
                }
            }
            for (int i = mincoordinatecount; i < maxcoordinatecount; i++)
            {
                switch (i)
                {
                    case 0: newx = longestCoordainte.x; break;
                    case 1: newy = longestCoordainte.y; break;
                    case 2: newz = longestCoordainte.z; break;
                }
            }
            if (maxcoordinatecount > 2)
                return new Coordinate(a.CoordinateSystem, newx, newy, newz);
            else
                return new Coordinate(a.coordinateSystem, newx, newy);
        }

        //public static Coordinate operator -(Coordinate a, Coordinate b)
        //{
        //    int maxcoordinatecount = a.Points.Length;
        //    int mincoordinatecount = b.Points.Length;
        //    Coordinate longestCoordainte = a;
        //    double remainMultiplier = 1;
        //    if (b.Points.Length > maxcoordinatecount)
        //    {
        //        maxcoordinatecount = b.Points.Length;
        //        mincoordinatecount = a.Points.Length;
        //        longestCoordainte = b;
        //        remainMultiplier = -1;
        //    }
        //    double[] points = new double[maxcoordinatecount];
        //    for (int i = 0; i < mincoordinatecount; i++)
        //    {
        //        points[i] = a.Points[i] - b.Points[i];
        //    }
        //    for (int i = mincoordinatecount; i < maxcoordinatecount; i++)
        //    {
        //        points[i] = longestCoordainte.Points[i] * remainMultiplier;
        //    }
        //    return new Coordinate(a.CoordinateSystem, points);
        //}

        public static Coordinate operator -(Coordinate a, Coordinate b)
        {
            int maxcoordinatecount = a.PointsLength;
            int mincoordinatecount = b.PointsLength;
            Coordinate longestCoordainte = a;
            double remainMultiplier = 1;
            if (b.PointsLength > maxcoordinatecount)
            {
                maxcoordinatecount = b.PointsLength;
                mincoordinatecount = a.PointsLength;
                longestCoordainte = b;
                remainMultiplier = -1;
            }
            double newx = 0;
            double newy = 0;
            double newz = 0;
            for (int i = 0; i < mincoordinatecount; i++)
            {
                switch (i)
                {
                    case 0: newx = a.x - b.x; break;
                    case 1: newy = a.y - b.y; break;
                    case 2: newz = a.z - b.z; break;
                }
            }
            for (int i = mincoordinatecount; i < maxcoordinatecount; i++)
            {
                switch (i)
                {
                    case 0: newx = longestCoordainte.x * remainMultiplier; break;
                    case 1: newy = longestCoordainte.y * remainMultiplier; break;
                    case 2: newz = longestCoordainte.z * remainMultiplier; break;
                }
            }
            if (maxcoordinatecount > 2)
                return new Coordinate(a.CoordinateSystem, newx, newy, newz);
            else
                return new Coordinate(a.coordinateSystem, newx, newy);
        }

        public bool IsValid()
        {
            CoordinateSystemOperation converter = CoordinateSystems.operators[(CoordinateSystem)this.CoordinateSystem];

            return converter.CoordinateIsValid(this);
        }

        public Coordinate Convert(CoordinateSystem targetCoordinateSystem)
        {

            if ((int)targetCoordinateSystem == this.CoordinateSystem)
            {
                return this;
            }

            if ((CoordinateSystem)this.CoordinateSystem == Coordinates.CoordinateSystem.Unity)
            {
                //Vector3 vector3 = new Vector3((float)Points[0], (float)Points[1], (float)Points[2]);
                Vector3 vector3 = new Vector3((float)x, (float)y, (float)z);
                Coordinate coord = new Coordinate(vector3);
                return coord.Convert(targetCoordinateSystem);
            }

            CoordinateSystemOperation converter = CoordinateSystems.operators[(CoordinateSystem)this.CoordinateSystem];
            //if (converter.CoordinateIsValid(this)==false)
            //{
            //    Debug.LogWarning($"coordinate is not valid: epsg{this.CoordinateSystem} {Points.ToString()}");
            //}
            Coordinate result = converter.ConvertToWGS84LatLonH(this);
            converter = CoordinateSystems.operators[targetCoordinateSystem];
            result = converter.ConvertFromWGS84LatLonH(result);
            //if (converter.CoordinateIsValid(result) == false)
            //{
            //    Debug.LogWarning($"coordinate is not valid: epsg{result.CoordinateSystem} {result.Points.ToString()}");
            //}
            return result;
        }

        public Quaternion RotationToLocalGravityUp()
        {

            if (this.CoordinateSystem == (int)CoordinateSystems.connectedCoordinateSystem)
            {
                return CoordinateSystems.connectedCRSToUnityUp;
            }

            //return RotationToUnityUP();
            CoordinateSystemOperation myConverter = CoordinateSystems.operators[(CoordinateSystem)this.CoordinateSystem];
            CoordinateSystemOperation connectedConverter = CoordinateSystems.operators[CoordinateSystems.connectedCoordinateSystem];

            Vector3WGS orientationDifference = connectedConverter.Orientation() - myConverter.Orientation();

            Coordinate inConnectedCrs = this.Convert(CoordinateSystems.connectedCoordinateSystem);

            Vector3WGS extraRotation = new Vector3WGS(inConnectedCrs.extraLongitudeRotation, inConnectedCrs.extraLattitudeRotation, 0);

            orientationDifference += extraRotation;

            //calculate the exrtaRotation in the connected coordainteSystem at the UnityOrigin
            Coordinate pointAtOrigin = CoordinateSystems.CoordinateAtUnityOrigin.Convert(Coordinates.CoordinateSystem.WGS84_LatLon);
            Vector3WGS ExtraRotationAtOrigin = new Vector3WGS(-pointAtOrigin.extraLongitudeRotation, -pointAtOrigin.extraLattitudeRotation, 0);
            // ExtraRotationAtOrigin = new Vector3WGS(0, -pointAtOrigin.extraLattitudeRotation, 0);
            // orientationDifference += ExtraRotationAtOrigin;


            Quaternion rotationToEast = Quaternion.AngleAxis((float)orientationDifference.lon, Vector3.up);
            if (myConverter.GetCoordinateSystemType() == CoordinateSystemType.Geocentric)
            {
                //rotate -90 degrees around the up-axis, to make sure east is in the X-direction;
                rotationToEast = rotationToEast * Quaternion.AngleAxis(-90, Vector3.up);
            }
            /// Now we calculate the difference in lattitude between de localUP at the coordinate and the orientation of the coordinateSystem  
            Quaternion rotationToFlat = Quaternion.AngleAxis(-(float)orientationDifference.lat, Vector3.right);

            /// when we apply both rotations, we get the rotation required to get the coordinateSystem pointing Up and North at the Unity-Origin
            Quaternion result = Quaternion.AngleAxis(-(float)ExtraRotationAtOrigin.lon, Vector3.up) * CoordinateSystems.connectedCRSToUnityUp * rotationToFlat * rotationToEast;

            return result;

        }

        public Vector3 ToUnity()
        {

            Coordinate connectionCoordinate = CoordinateSystems.CoordinateAtUnityOrigin;
            //transform current coordinate to connectioncoordinate;

            Coordinate inConnecedCRS = this.Convert(CoordinateSystems.connectedCoordinateSystem);

            //get position relative to origin
            Coordinate difference = inConnecedCRS - connectionCoordinate;
            //Vector3 relativePosition = new Vector3((float)difference.Points[0], (float)difference.Points[1], (float)difference.Points[2]);
            Vector3 relativePosition = new Vector3((float)difference.x, (float)difference.y, (float)difference.z);

            //move axes to unity-equivlent axes
            if (CoordinateSystems.operators[CoordinateSystems.connectedCoordinateSystem].GetCoordinateSystemType() == CoordinateSystemType.Geocentric)
            {
                //unity.x = -deltaX;
                //unity.y = deltaZ;
                //unity.z = -deltaY;

                relativePosition = new Vector3(-relativePosition.x, relativePosition.z, -relativePosition.y);
            }
            else
            {
                //cartesian X = unity X;
                //cartesian Y = unity Z;
                //cartesian Z = unity Y;

                relativePosition = new Vector3(relativePosition.x, relativePosition.z, relativePosition.y);
            }
            //apply rotation from connectedCoordinateSystem to Unity

            Vector3 rotatedRelativePosition = CoordinateSystems.connectedCRSToUnityUp * relativePosition;

            return rotatedRelativePosition;
        }
    }
}
