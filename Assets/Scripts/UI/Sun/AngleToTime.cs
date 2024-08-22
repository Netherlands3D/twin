using System;
using System.Collections;
using System.Collections.Generic;
using Netherlands3D.TileHandler;
using UnityEngine;
using UnityEngine.Events;

namespace Netherlands3D.Twin
{
    public class AngleToTime : MonoBehaviour
    {
        public UnityEvent<int> onConvertedToHour;
        public UnityEvent<int> onConvertedToMinute;
        
        public void To12HourTime(float angle)
        {
            angle %= 360;
            if (angle < 0) angle += 360;

            // Calculate the hour by dividing the angle by 30 degrees per hour
            int hour = (int)(angle / 30) % 12;
            int minutes = (int)((angle % 30) * 2);
            
            onConvertedToHour.Invoke(hour);
            onConvertedToMinute.Invoke(minutes);
        }

        public void To24HourTime(float angle)
        {
            angle %= 360;
            if (angle < 0) angle += 360;
            
            // Calculate the hour by dividing the angle by 15 degrees per hour
            int hour = (int)(angle / 15) % 24;
            int minutes = (int)((angle % 15) * 4);
            
            Debug.Log(angle + "\t" + hour +":" + minutes);

            onConvertedToHour.Invoke(hour);
            onConvertedToMinute.Invoke(minutes);
        }
    }
}
