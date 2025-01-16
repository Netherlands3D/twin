using System;
using System.Collections;
using Netherlands3D.Sun;
using UnityEngine;
using UnityEngine.Events;

namespace Netherlands3D.Functionalities.Sun
{
    //this is needed because when UI activates, it needs to set the field values to the currently active time without SunTime sending an event.
    public class UpdateSunUIFields : MonoBehaviour
    {
        public UnityEvent<DateTime> LoadInitialTime;
        public UnityEvent<bool> LoadInitialAnimatingState;
        
        private IEnumerator Start()
        {
            yield return null; //wait a frame so all UI is initialized
            var st = FindAnyObjectByType<SunTime>();
            LoadInitialTime.Invoke(st.Time);
            LoadInitialAnimatingState.Invoke(st.IsAnimating);
        }
    }
}
