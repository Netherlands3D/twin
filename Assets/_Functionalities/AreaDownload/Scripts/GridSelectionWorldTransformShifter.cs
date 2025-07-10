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
            var offsetX = Origin.current.Coordinate.easting % areaSelection.GridSize;
            var offsetY = Origin.current.Coordinate.northing % areaSelection.GridSize;
            areaSelection.GridOffset = new Vector3((float)-offsetX, 0, (float)-offsetY);
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
            
            var offsetX = diffX % areaSelection.GridSize;
            var offsetY = diffY % areaSelection.GridSize;
            
            areaSelection.GridOffset += new Vector3((float)-offsetX, 0, (float)-offsetY);
        }
    }
}
