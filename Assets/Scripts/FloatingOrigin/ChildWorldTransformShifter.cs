using Netherlands3D.Coordinates;
using UnityEngine;
using System.Collections.Generic;

namespace Netherlands3D.Twin.FloatingOrigin
{
    public class ChildWorldTransformShifter : WorldTransformShifter
    {
        private Dictionary<Transform, PositionAndRotation> tilesToShift = new();
        
        private struct PositionAndRotation
        {
            public Coordinate coordinateInConnectedCRS;
            public Quaternion rotationInConnectedCRS;
            public PositionAndRotation(Coordinate coordinate, Quaternion rotation)
            {
                coordinateInConnectedCRS = coordinate;
                rotationInConnectedCRS = rotation;
            }
        }

        /// <summary>
        /// Because some libraries dynamically create and destroy objects, we need to collect a list of transforms
        /// to reposition and calculate each their individual Coordinate, so that afetr shifting we can convert that
        /// coordinate back to a Unity position. Because the calculation to a Unity position includes taking the origin
        /// shift into account, it will thus reposition the tile to the correct location.
        /// </summary>
        public override void PrepareToShift(WorldTransform worldTransform, Coordinate fromOrigin, Coordinate toOrigin)
        {
            tilesToShift.Clear();
            
            foreach (Transform child in transform)
            {
                Coordinate position = new Coordinate(transform.position);
                Quaternion rotation = Quaternion.Inverse(position.RotationToLocalGravityUp()) * transform.rotation;
               
                tilesToShift.Add(
                    child, 
                    new PositionAndRotation(position,rotation)
                );
            }
        }

        public override void ShiftTo(WorldTransform worldTransform, Coordinate fromOrigin, Coordinate toOrigin)
        {
#if UNITY_EDITOR
            if (worldTransform.Origin.LogShifts) Debug.Log($"<color=grey>{gameObject.name}: Shifting {tilesToShift.Count} children</color>");
#endif
            foreach (KeyValuePair<Transform, PositionAndRotation> tile in tilesToShift)
            {
                var newPosition = tile.Value.coordinateInConnectedCRS.ToUnity();
                var newRotation = tile.Value.coordinateInConnectedCRS.RotationToLocalGravityUp() * tile.Value.rotationInConnectedCRS;
#if UNITY_EDITOR
                if (worldTransform.Origin.LogShifts) Debug.Log($"<color=grey>| Shifting {tile.Key.gameObject.name} from {tile.Key.position} to {newPosition}</color>");
#endif
                tile.Key.position = newPosition;
                tile.Key.rotation = newRotation;
            }
        }
    }
}