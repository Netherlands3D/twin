using System;
using Netherlands3D.Coordinates;
using Netherlands3D.Twin.FloatingOrigin;
using UnityEngine;

namespace Netherlands3D.Functionalities.AreaDownload
{
    public class GridSelectionWorldTransformShifter : WorldTransformShifter
    {
        private Netherlands3D.SelectionTools.AreaSelection areaSelection;

        private void Awake()
        {
            areaSelection = GetComponent<Netherlands3D.SelectionTools.AreaSelection>();
        }
        
        private void Start()
        {

            areaSelection.GridOffset = CalculateOffset(Origin.current.Coordinate.easting, Origin.current.Coordinate.northing);
        }

        private Vector3 CalculateOffset(double easting, double northing)
        {
            var offsetX = easting % areaSelection.GridSize;
            var offsetY = northing % areaSelection.GridSize;
            return new Vector3((float)-offsetX, 0, (float)-offsetY);
        }

        public override void PrepareToShift(WorldTransform worldTransform, Coordinate fromOrigin, Coordinate toOrigin)
        {
            areaSelection.ClearSelection();
        }

        public override void ShiftTo(WorldTransform worldTransform, Coordinate fromOrigin, Coordinate toOrigin)
        {
            var from = fromOrigin.Convert(CoordinateSystems.connectedCoordinateSystem);
            var to = toOrigin.Convert(CoordinateSystems.connectedCoordinateSystem);

            var diffX = to.easting - from.easting;
            var diffY = to.northing - from.northing;

            areaSelection.GridOffset += CalculateOffset(diffX, diffY);
        }
    }
}
