using System;
using UnityEngine;
using UnityEngine.Events;

namespace Netherlands3D.Twin
{
    public class ResetSunTimeToNow : MonoBehaviour
    {
        public UnityEvent<DateTime> dateTimeNow;
        public void GetDateTimeNowAndSendEvent()
        {
            dateTimeNow.Invoke(DateTime.Now);
        }
    }
}
