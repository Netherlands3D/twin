using Netherlands3D.Coordinates;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Netherlands3D.Twin
{
    public class WorldMovingAsset : WorldAsset
    {
        public double latTarget, lonTarget;

        private Vector3 target;
        private bool moveToTarget = false;
        public float moveSpeed = 1;
        

        protected Coordinate targetCoord;

        public override void Start()
        {
            base.Start();
            
            
        }

        protected override void OnSetStartPosition(Vector3 startPosition)
        {
            base.OnSetStartPosition(startPosition);
            targetCoord = new Coordinate(CoordinateSystem.WGS84, latTarget, lonTarget, 0);
            Vector3 unityCoord = targetCoord.ToUnity();
            unityCoord.y = startPosition.y;
            target = unityCoord;
        }

        private void UpdateCoords()
        {
            //startPosition = 
        }

        public override void Update() 
        {
            base.Update();
            if (moveToTarget)
            {
                float dist = Vector3.Distance(prefab.transform.position, target);
                if (dist > 1f)
                {
                    prefab.transform.position = Vector3.Slerp(prefab.transform.position, target, Time.deltaTime * moveSpeed);
                }
                else
                    moveToTarget = false;
            }
            else
            {
                float dist = Vector3.Distance(prefab.transform.position, startPosition);
                if (dist > 1f)
                {
                    prefab.transform.position = Vector3.Slerp(prefab.transform.position, startPosition, Time.deltaTime * moveSpeed);
                }
                else
                    moveToTarget = true;
            }
        }
    }
}
