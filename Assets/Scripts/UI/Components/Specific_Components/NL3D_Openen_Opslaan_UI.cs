using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace Netherlands3D.Twin
{
    public class NL3D_Openen_Opslaan_UI : MonoBehaviour
    {
        [SerializeField] private Slider Loadingbar;
        [SerializeField] private GameObject[] objectsToHideWhenDone;
        [SerializeField] private GameObject[] objectsToShowWhenDone;

        private void Update()
        {
            var isLoading = Loadingbar.value < Loadingbar.maxValue;
            foreach (GameObject obj in objectsToHideWhenDone)
            {
                obj.SetActive(isLoading);
            }

            foreach (GameObject obj in objectsToShowWhenDone)
            {
                obj.SetActive(!isLoading);
            }
        }
    }
}
