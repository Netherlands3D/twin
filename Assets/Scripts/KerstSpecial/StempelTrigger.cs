using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Netherlands3D.Twin
{
    public class StempelTrigger : ZoneTrigger
    {
        public bool IsCollected = false;
        private RaceController controller;
        private stempelkaart stempelkaart;

        private void Start()
        {
            controller = FindObjectOfType<RaceController>();
            stempelkaart = FindObjectOfType<stempelkaart>(true);
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


                
                stempelkaart.gameObject.SetActive(true);   
                stempelkaart.SetStampMarkerEnabled(false);
                StartCoroutine(WaitSeconds(10, () =>
                {
                    stempelkaart.gameObject.SetActive(false);
                }));
                StartCoroutine(WaitSeconds(1, () =>
                {
                    stempelkaart.SetStampMarkerEnabled(true);
                    string objName = transform.parent.transform.parent.name;
                    char numberString = objName[objName.Length - 1];
                    int num = int.Parse(numberString.ToString());
                    stempelkaart.SetStampEnabled(num, true);
                    StartCoroutine(WaitSeconds(1, () =>
                    {
                        stempelkaart.SetStampMarkerEnabled(false);
                    }));
                }));
            }
        }

        private IEnumerator WaitSeconds(float seconds, Action callBack)
        {
            yield return new WaitForSeconds(seconds);
            callBack?.Invoke();
        }

    }
}
