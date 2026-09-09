using System;
using Netherlands3D.Coordinates;
using Netherlands3D.Twin.FloatingOrigin;
using Netherlands3D.Twin.Projects;
using UnityEngine;

namespace Netherlands3D.Functionalities.AreaDownload
{
    public class GridSelectionWorldTransformShifter : WorldTransformShifter
    {
        private SelectionTools.GridInput _gridInput;

        private void Awake()
        {
            _gridInput = GetComponent<SelectionTools.GridInput>();
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
            var offsetX = easting % _gridInput.GridSize;
            var offsetY = northing % _gridInput.GridSize;
            return new Vector3((float)-offsetX, 0, (float)-offsetY);
        }

        public override void PrepareToShift(WorldTransform worldTransform, Coordinate fromOrigin, Coordinate toOrigin)
        {
            _gridInput.SetSelectionVisualEnabled(false);
        }

        public override void ShiftTo(WorldTransform worldTransform, Coordinate fromOrigin, Coordinate toOrigin)
        {            
            UpdateGridOffset();
        }

        private void UpdateGridOffset()
        {
            _gridInput.GridOffset = CalculateOffset(Origin.current.Coordinate.easting, Origin.current.Coordinate.northing);
        }
    }
}
