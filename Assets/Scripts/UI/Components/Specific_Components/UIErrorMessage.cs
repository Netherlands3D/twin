using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Netherlands3D.Twin
{
    public class UIErrorMessage : MonoBehaviour
    {

        public GameObject [] ToHide;
        public GameObject [] ToShow;
        // Start is called before the first frame update
        void Awake()
        {
            foreach(GameObject hide in ToHide)
            {
                hide.SetActive(false);
            }

            foreach (GameObject show in ToShow)
            {
                show.SetActive(false);
            }

        }

      
    }
}
