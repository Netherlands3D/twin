using System.Collections.Generic;
using Netherlands3D.Coordinates;
using Netherlands3D.Tiles3D;
using UnityEngine;

namespace Netherlands3D.Twin.FloatingOrigin
{
    public class ThreeDTilesWorldTransformShifter : WorldTransformShifter
    {
        private Dictionary<Transform, Coordinate> tilesToShift = new();
        private Read3DTileset tilesetReader;

        private void Awake() {
            tilesetReader = GetComponent<Read3DTileset>();
        }

        /// <summary>
        /// Because the 3D Tile handler dynamically creates and destroys tiles, we need to collect a list of transforms
        /// to reposition and calculate each their individual Coordinate, so that afetr shifting we can convert that
        /// coordinate back to a Unity position. Because the calculation to a Unity position includes taking the origin
        /// shift into account, it will thus reposition the tile to the correct location.
        /// </summary>
        public override void PrepareToShift(WorldTransform worldTransform, Coordinate fromOrigin, Coordinate toOrigin)
        {
            tilesToShift.Clear();
            
            var contentComponents = transform.GetComponentsInChildren<Content>();
            foreach (Content contentComponent in contentComponents)
            {
                foreach (Transform child in contentComponent.transform)
                {
                    var baseCoordinate = new Coordinate(child.position);
                    tilesToShift.Add(
                        child, 
                        baseCoordinate
                    );
                }
            }
        }
        
        public override void ShiftTo(WorldTransform worldTransform, Coordinate fromOrigin, Coordinate toOrigin)
        {
#if UNITY_EDITOR
            if (worldTransform.Origin.LogShifts) Debug.Log($"<color=grey>{gameObject.name}: Shifting {tilesToShift.Count} tiles</color>");
#endif

            foreach (KeyValuePair<Transform,Coordinate> tile in tilesToShift)
            {
                var coordinate = tile.Value;
                var newPosition = coordinate.ToUnity();
#if UNITY_EDITOR
                if (worldTransform.Origin.LogShifts) Debug.Log($"<color=grey>| Shifting {tile.Key.gameObject.name} from {tile.Key.position} to {newPosition}</color>");
#endif
                tile.Key.position = newPosition;
            }

            //Refresh 3D Tiles internal Bounds calculations
            tilesetReader.RecalculateBounds();
        }
    }
}