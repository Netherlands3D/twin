using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Netherlands3D.Coordinates
{

        public static class Settings
        {
            static CoordinateSystem _connectedCoordinateSystem;
            static Coordinate _unityOrigin;

            public static CoordinateSystem ConnectedCoordinateSystem
            {
                get { return _connectedCoordinateSystem; }
                set
                {
                    if (_connectedCoordinateSystem != value)
                    {
                        _connectedCoordinateSystem = value;
                        if ((CoordinateSystem)_unityOrigin.CoordinateSystem != CoordinateSystem.Unity)
                        {
                            SetOrigin(_unityOrigin);
                        }
                    }

                }
            }


            public static void SetOrigin(Coordinate coordinate)
            {
                if (_connectedCoordinateSystem == CoordinateSystem.Unity)
                {
                    _connectedCoordinateSystem = (CoordinateSystem)coordinate.CoordinateSystem;
                }
                //transform to connectedCoordinateSystem
                _unityOrigin = coordinate;
            }


        }

}
