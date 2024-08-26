using System;
using Netherlands3D.Twin.Projects;
using UnityEngine;
using UnityEngine.Events;

namespace Netherlands3D.Twin
{
    public class ResetSunTimeToNow : MonoBehaviour
    {
        public UnityEvent<bool> useCurrentTime;

        public void SetDateTimeToNow()
        {
            useCurrentTime.Invoke(true);
            ProjectData.Current.UseCurrentTime = true;
        }
    }
}