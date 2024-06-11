using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Netherlands3D.Twin
{
    public class OverlayInspector : MonoBehaviour
    {
        public void CloseOverlay()
        {
            ContentOverlay.Instance.CloseOverlay();
        }
    }
}
