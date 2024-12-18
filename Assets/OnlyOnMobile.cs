using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Netherlands3D.Twin
{
    public class OnlyOnMobile : MonoBehaviour
    {
        void Start()
        {
            gameObject.SetActive(Application.isMobilePlatform);
        }
    }
}
