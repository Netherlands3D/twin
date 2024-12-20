using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Netherlands3D.Twin
{
    public class StempelTrigger : ZoneTrigger
    {
        public bool IsCollected = false;
        private RaceController controller;

        private void Start()
        {
            controller = FindObjectOfType<RaceController>();
            GetComponent<BoxCollider>().isTrigger = true;
        }

        protected override void OnTriggerEnter(Collider other)
        {
            base.OnTriggerEnter(other);
            if (controller != null && controller.playerCollider == other)
            {
                IsCollected = true;

                StempelTrigger[] triggers = transform.parent.transform.GetComponentsInChildren<StempelTrigger>();
                foreach (StempelTrigger trigger in triggers) 
                    trigger.IsCollected = true;
            }
        }
    }
}
