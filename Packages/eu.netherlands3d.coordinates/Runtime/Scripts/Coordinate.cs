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
        
        public readonly int CoordinateSystem;

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
        public double[] Points;

        public Coordinate(CoordinateSystem coordinateSystem, params double[] points)
        {
            CoordinateSystem = (int)coordinateSystem;
            Points = points;
            extraLongitudeRotation = 0;
            extraLattitudeRotation = 0;
        }

        public Coordinate(int coordinateSystem, params double[] points)
        {
            CoordinateSystem = coordinateSystem;
            Points = points;
            extraLongitudeRotation = 0;
            extraLattitudeRotation = 0;
        }

        public Coordinate(Vector3 unityPosition)
        {
            extraLattitudeRotation = 0;
            extraLongitudeRotation = 0;
            Vector3 unrotatedRelativePosition = Quaternion.Inverse(CoordinateSystems.connectedCRSToUnityUp) * unityPosition;

            if (CoordinateSystems.operators[CoordinateSystems.connectedCoordinateSystem].GetCoordinateSystemType() == CoordinateSystemType.Geocentric)
            {
                //unity.x = -deltaX;
                //unity.y = deltaZ;
                //unity.z = -deltaY;

                Points = new double[3] { -unrotatedRelativePosition.x, -unrotatedRelativePosition.z, unrotatedRelativePosition.y };
            }
            else
            {
                //cartesian X = unity X;
                //cartesian Y = unity Z;
                //cartesian Z = unity Y;

                Points = new double[3] { unrotatedRelativePosition.x, unrotatedRelativePosition.z, unrotatedRelativePosition.y };
            }
            CoordinateSystem = (int)CoordinateSystems.connectedCoordinateSystem;
            Points = (CoordinateSystems.CoordinateAtUnityOrigin + new Coordinate(CoordinateSystem, Points)).Points;
        }

        public static Coordinate operator +(Coordinate a, Coordinate b)
        {
            int maxcoordinatecount = a.Points.Length;
            int mincoordinatecount = b.Points.Length;
            Coordinate longestCoordainte = a;
            if (b.Points.Length>maxcoordinatecount)
            {
                maxcoordinatecount = b.Points.Length;
                mincoordinatecount = a.Points.Length;
                longestCoordainte = b;
            }
            double[] points = new double[maxcoordinatecount];
            for (int i = 0; i < mincoordinatecount; i++)
            {
                points[i] = a.Points[i] + b.Points[i];
            }
            for (int i = mincoordinatecount; i < maxcoordinatecount; i++)
            {
                points[i] = longestCoordainte.Points[i];
            }
            return new Coordinate(a.CoordinateSystem, points);
        }
        public static Coordinate operator -(Coordinate a, Coordinate b)
        {
            int maxcoordinatecount = a.Points.Length;
            int mincoordinatecount = b.Points.Length;
            Coordinate longestCoordainte = a;
            double remainMultiplier = 1;
            if (b.Points.Length > maxcoordinatecount)
            {
                maxcoordinatecount = b.Points.Length;
                mincoordinatecount = a.Points.Length;
                longestCoordainte = b;
                remainMultiplier = -1;
            }
            double[] points = new double[maxcoordinatecount];
            for (int i = 0; i < mincoordinatecount; i++)
            {
                points[i] = a.Points[i] - b.Points[i];
            }
            for (int i = mincoordinatecount; i < maxcoordinatecount; i++)
            {
                points[i] = longestCoordainte.Points[i]*remainMultiplier;
            }
            return new Coordinate(a.CoordinateSystem, points);
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
                Vector3 vector3 = new Vector3((float)Points[0], (float)Points[1], (float)Points[2]);
                Coordinate coord= new Coordinate(vector3);
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
            /// we want to find out how much we have to rotate to make the localUpDirection in our coordinateSystem 
            /// align with the localUpDirection in the coordinateSystem that is connected to Unity

            /// if our coordinateSystem is the same as the one connected to Unity, it is easy and we are alrady done.
            if (this.CoordinateSystem == (int)CoordinateSystems.connectedCoordinateSystem)
            {
                return CoordinateSystems.connectedCRSToUnityUp;
            }


            CoordinateSystemOperation myConverter = CoordinateSystems.operators[(CoordinateSystem)this.CoordinateSystem];
            CoordinateSystemOperation connectedConverter = CoordinateSystems.operators[CoordinateSystems.connectedCoordinateSystem];

            ///to figure out how much we have to rotate, we need to combine a couple of angles
            ///first we calculate how much the point we try to align to is rotated relative to the unity-Origin 
            ///we can calculate this bij looking at the localRotation in the connectedCrs for the origin and the point we are looking at

            Coordinate thisCoordinateInConnectedCRS = this.Convert(CoordinateSystems.connectedCoordinateSystem);
            Vector3WGS ConnectedCRSLocalUpAtTHisPoint = connectedConverter.LocalUpDirection(thisCoordinateInConnectedCRS);
            Vector3WGS ConnectedCRSLocalUpAtOrigin = connectedConverter.LocalUpDirection(CoordinateSystems.CoordinateAtUnityOrigin);
            Vector3WGS inUnityRotation = ConnectedCRSLocalUpAtTHisPoint - ConnectedCRSLocalUpAtOrigin;

            /// we also need to know how much our CRS is rotated at the origin
            /// we can calculate this by calculating how much rotation is needed to align with the connectedCoordinateSystem
            /// 
            Vector3WGS crsRotation = connectedConverter.Orientation() - myConverter.Orientation();


            //don't touch these work
            Coordinate myCRSposAtOrigin = CoordinateSystems.CoordinateAtUnityOrigin.Convert((CoordinateSystem)this.CoordinateSystem);
            Vector3WGS localUpAtOrigin = myConverter.LocalUpDirection(myCRSposAtOrigin);
            Vector3WGS wgsUpAtPosition = connectedConverter.GlobalUpDirection(this.Convert(CoordinateSystems.connectedCoordinateSystem));

            Vector3WGS myLocalUPAtPosition = myConverter.LocalUpDirection(this);

            Vector3WGS wgsUpAtOrigin = CoordinateSystems.wgsAtUp;

            Vector3WGS extraRotation = (wgsUpAtPosition - myLocalUPAtPosition) - (wgsUpAtOrigin - localUpAtOrigin);

            Vector3WGS rotation = wgsUpAtOrigin - extraRotation;



            //Apply the rotation
            Quaternion rotationToEast = Quaternion.AngleAxis((float)rotation.lon - (float)myConverter.Orientation().lon, Vector3.up);
            if (myConverter.GetCoordinateSystemType() == CoordinateSystemType.Geocentric)
            {
                rotationToEast = rotationToEast * Quaternion.AngleAxis(-90, Vector3.up);
            }
            Quaternion rotationToFlat = Quaternion.AngleAxis((float)myConverter.Orientation().lat - (float)rotation.lat, Vector3.right);
            Quaternion result = rotationToFlat * rotationToEast;

            


            return result;





            //return rotation;
        }
       
        public Vector3 ToUnity()
        {
            
            Coordinate connectionCoordinate = CoordinateSystems.CoordinateAtUnityOrigin;
            //transform current coordinate to connectioncoordinate;

            Coordinate inConnecedCRS = this.Convert(CoordinateSystems.connectedCoordinateSystem);
            
            //get position relative to origin
            Coordinate difference = inConnecedCRS - connectionCoordinate;
            Vector3 relativePosition = new Vector3((float)difference.Points[0], (float)difference.Points[1], (float)difference.Points[2]);
            
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
