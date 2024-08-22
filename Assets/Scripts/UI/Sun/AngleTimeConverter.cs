using System;
using System.Collections;
using System.Collections.Generic;
using Netherlands3D.TileHandler;
using UnityEngine;
using UnityEngine.Events;

namespace Netherlands3D.Twin
{
    public class AngleTimeConverter : MonoBehaviour
    {
        public UnityEvent<int> onConvertedToHour;
        public UnityEvent<int> onConvertedToMinute;

        public UnityEvent<float> onConvertedToAngle;
        
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
            
            onConvertedToHour.Invoke(hour);
            onConvertedToMinute.Invoke(minutes);
        }

        public void Time24ToAngle(DateTime dateTime)
        {
            //15 degrees is one hour, 0.25 degrees is one minute
            float angle = dateTime.Hour * 15 + dateTime.Minute * 0.25f;
            onConvertedToAngle.Invoke(angle);
        }
        
        public void Time12ToAngle(DateTime dateTime)
        {
            //30 degrees is one hour, 0.5 degrees is one minute
            float angle = dateTime.Hour * 30 + dateTime.Minute * 0.5f;
            onConvertedToAngle.Invoke(angle);
            
        }
    }
}
