using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Netherlands3D.Twin
{
    public class targetSucces : MonoBehaviour
    {
        public DistanceCalculator sleeVlieger;

        public void StartParty()
        {
            GetComponent<AudioSource>().Play();      
            StartCoroutine(party());
        }

        public IEnumerator party()
        {
            yield return new WaitForSeconds(4);
            sleeVlieger.NextTarget();

        }

        // Update is called once per frame
        void Update()
        {
        
        }
    }
}
