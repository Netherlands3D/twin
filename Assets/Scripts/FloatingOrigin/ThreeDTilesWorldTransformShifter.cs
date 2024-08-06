using System.Collections.Generic;
using Netherlands3D.Coordinates;
using Netherlands3D.Tiles3D;
using UnityEngine;

namespace Netherlands3D.Twin.FloatingOrigin
{
    /// <summary>
    /// when the 3DTileHandler needs to load geometry for the content of a tile, it creates a child-gameobject containing
    /// a Content-component. this component takes care of the loading and positioning of the geometry. 
    /// the child-gameobject is destroyed when it is no longer needed. 
    /// Depending on the loaded geometry, these child-gameobjects can have multiple children itself.
    /// to move all the 3dtile-geometry, we need to move all the gameobjects containing a Content-component.
    /// </summary>
    public class ThreeDTilesWorldTransformShifter : WorldTransformShifter
    {
        private Dictionary<Transform, PositionAndRotation> tilesToShift = new();
        private Read3DTileset tilesetReader;
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
        

        private void Awake() {
            tilesetReader = GetComponent<Read3DTileset>();
        }

    
        public override void PrepareToShift(WorldTransform worldTransform, Coordinate fromOrigin, Coordinate toOrigin)
        {
            tilesToShift.Clear();
            
            var contentComponents = transform.GetComponentsInChildren<Content>();
            foreach (Content contentComponent in contentComponents)
            {
                Coordinate position = new Coordinate(contentComponent.transform.position);
                Quaternion rotation = Quaternion.Inverse(position.RotationToLocalGravityUp()) * transform.rotation;

                tilesToShift.Add(
                    contentComponent.transform,
                    new PositionAndRotation(position, rotation)
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

        //Refresh 3D Tiles internal Bounds calculations
        tilesetReader.InvalidateBounds();
        }
    }
}