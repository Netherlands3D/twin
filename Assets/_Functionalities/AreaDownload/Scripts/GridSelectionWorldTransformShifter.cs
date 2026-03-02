using System;
using Netherlands3D.Coordinates;
using Netherlands3D.Twin.FloatingOrigin;
using Netherlands3D.Twin.Projects;
using UnityEngine;

namespace Netherlands3D.Functionalities.AreaDownload
{
    public class GridSelectionWorldTransformShifter : WorldTransformShifter
    {
        private SelectionTools.AreaSelection areaSelection;

        private void Awake()
        {
            areaSelection = GetComponent<SelectionTools.AreaSelection>();
        }
        
        private void Start()
        {
            UpdateGridOffset();
        }

        private void OnEnable()
        {
            UpdateGridOffset();
        }

        private Vector3 CalculateOffset(double easting, double northing)
        {
            var offsetX = easting % areaSelection.GridSize;
            var offsetY = northing % areaSelection.GridSize;
            return new Vector3((float)-offsetX, 0, (float)-offsetY);
        }

        public override void PrepareToShift(WorldTransform worldTransform, Coordinate fromOrigin, Coordinate toOrigin)
        {
            areaSelection.SetSelectionVisualEnabled(false);
        }

        public override void ShiftTo(WorldTransform worldTransform, Coordinate fromOrigin, Coordinate toOrigin)
        {            
            UpdateGridOffset();
        }

        private void UpdateGridOffset()
        {
            areaSelection.GridOffset = CalculateOffset(Origin.current.Coordinate.easting, Origin.current.Coordinate.northing);
        }
    }
}
