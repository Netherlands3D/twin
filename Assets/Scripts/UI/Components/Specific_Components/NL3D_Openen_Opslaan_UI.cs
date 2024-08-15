using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace Netherlands3D.Twin
{
    public class NL3D_Openen_Opslaan_UI : MonoBehaviour
    {
        public Slider Loadingbar;
        public GameObject[] objectsToHideWhenDone;
        public GameObject[] objectsToShowWhenDone;

        private void Update()
        {
            if (Loadingbar.value < Loadingbar.maxValue)
            {
                foreach (GameObject obj in objectsToHideWhenDone)
                {
                    obj.SetActive(true);
                }

                foreach (GameObject obj in objectsToShowWhenDone)
                {
                    obj.SetActive(false);
                }
            }
            else
            {
                foreach (GameObject obj in objectsToHideWhenDone)
                {
                    obj.SetActive(false);
                }

                foreach (GameObject obj in objectsToShowWhenDone)
                {
                    obj.SetActive(true);
                }
            }
        }
    }
}
