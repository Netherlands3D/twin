using Netherlands3D.Rendering;
using UnityEngine;

namespace Netherlands3D.Twin
{
    public class WorldDecalAsset : WorldAsset
    {
        public Texture2D texture;
        public float SpeedPenalty = 0.15f;
        private SphereCollider sphereCollider;
        private ZoneTrigger penaltyTrigger;
        private RaceController raceController;
        public bool hasTrigger = false;

        public Vector2 decalSize;

        public override void Start()
        {
            base.Start();
            prefab.transform.rotation = Quaternion.Euler(90, YRotation, 0);
            TextureDecalProjector projector = prefab.GetComponent<TextureDecalProjector>();
            projector.SetSize(new Vector3(decalSize.x, decalSize.y, 100));
            projector.SetTexture(texture);
            projector.transform.SetParent(transform);
            raceController = FindObjectOfType<RaceController>();           
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
            if(col == raceController.playerCollider)
            {
                raceController.GivePenaltySpeedToPlayer(SpeedPenalty);
            }
        }

        protected override void OnSetStartPosition(Vector3 startPosition)
        {
            prefab.transform.position = startPosition + Vector3.up * 10;
            if (hasTrigger)
            {
                GameObject triggerObject = new GameObject("penaltyTrigger");
                sphereCollider = triggerObject.AddComponent<SphereCollider>();
                sphereCollider.radius = size * 0.5f;
                triggerObject.transform.position = startPosition;
                triggerObject.transform.SetParent(transform, true);
                sphereCollider.isTrigger = true;
                penaltyTrigger = triggerObject.AddComponent<ZoneTrigger>();
                penaltyTrigger.OnEnter += OnHitPlayerStart;
                penaltyTrigger.OnStay += OnHitPlayer;
            }
        }
    }
}
