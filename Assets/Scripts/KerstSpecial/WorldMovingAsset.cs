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
        public float colliderScale = 1;
        private RaceController raceController;
        public float SpeedPenalty = 0.15f;
        protected Coordinate targetCoord;

        public override void Start()
        {
            base.Start();
            raceController = FindObjectOfType<RaceController>();
            prefab.AddComponent<SphereCollider>().radius = colliderScale;

            GameObject triggerPenalty = new GameObject("penaltyTrigger");
            ZoneTrigger zTrigger = triggerPenalty.AddComponent<ZoneTrigger>();
            triggerPenalty.transform.SetParent(prefab.transform, false);
            triggerPenalty.transform.localPosition = Vector3.zero;
            SphereCollider trigger = triggerPenalty.AddComponent<SphereCollider>();
            trigger.radius = colliderScale;
            trigger.isTrigger = true;
            zTrigger.OnStay += OnHitPlayer;
            zTrigger.OnEnter += OnHitPlayerStart;

        }

        private void OnHitPlayerStart(Collider col, ZoneTrigger trigger)
        {
            if (col == raceController.playerCollider)
            {
                raceController.GivePenaltySpeedToPlayer(0);
            }
        }

        private void OnHitPlayer(Collider col, ZoneTrigger trigger)
        {
            if (col == raceController.playerCollider)
            {
                raceController.GivePenaltySpeedToPlayer(SpeedPenalty);
            }
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
                Vector3 forward = target - prefab.transform.position;
                Quaternion rot = Quaternion.LookRotation(forward) * Quaternion.Euler(XRotation, 0, 0);
                prefab.transform.rotation = Quaternion.Slerp(prefab.transform.rotation, rot, Time.deltaTime * moveSpeed);
                float dist = Vector3.Distance(prefab.transform.position, target);
                if (dist > 1f)
                {
                    prefab.transform.position += forward.normalized * Time.deltaTime * moveSpeed;
                    
                }
                else
                    moveToTarget = false;
            }
            else
            {
                Vector3 forward = startPosition - prefab.transform.position;
                Quaternion rot = Quaternion.LookRotation(forward) * Quaternion.Euler(XRotation, 0, 0);
                prefab.transform.rotation = Quaternion.Slerp(prefab.transform.rotation, rot, Time.deltaTime * moveSpeed);
                float dist = Vector3.Distance(prefab.transform.position, startPosition);
                if (dist > 1f)
                {
                    prefab.transform.position += forward.normalized * Time.deltaTime * moveSpeed;
                    
                }
                else
                    moveToTarget = true;
            }
        }
    }
}
