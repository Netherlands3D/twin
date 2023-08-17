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

using Netherlands3D.Coordinates;
using UnityEngine;
using UnityEngine.Events;

namespace Netherlands3D.SelectionTools
{
    public class BoundsToRDBoundingBox : MonoBehaviour
    {
        public UnityEvent<double[]> boundingBoxRDEvent;

        public void ConvertToRD(Bounds bounds)
        {
            //Convert bounds to RD coordinates on a 2D map
            Coordinate min = new Coordinate(CoordinateSystem.Unity, bounds.min.x, bounds.min.y, bounds.min.z);
            var bottomLeft = CoordinateConverter.ConvertTo(min, CoordinateSystem.RD).ToVector3(); 
            Coordinate max = new Coordinate(CoordinateSystem.Unity, bounds.max.x, bounds.max.y, bounds.max.z);
            var topRight = CoordinateConverter.ConvertTo(max, CoordinateSystem.RD).ToVector3();

            boundingBoxRDEvent.Invoke(new double[] { bottomLeft.x, bottomLeft.y, topRight.x, topRight.y });
        }
    }
}
