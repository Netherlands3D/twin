using System;
using UnityEngine;
using UnityEngine.Events;

namespace Netherlands3D.Functionalities.Sun
{
    public class TimeParser : MonoBehaviour
    {
        public UnityEvent<int> hourParsed;
        public UnityEvent<int> minuteParsed;
        public UnityEvent<int> secondParsed;
        public UnityEvent<DateTime> dateTimeParsed;

        public void ParseTimeAndInvokeEvents(string timeString)
        {
            //replace separation characters so parser will work 
            timeString = timeString.Replace('.', ':');
            timeString = timeString.Replace(';', ':');
            timeString = timeString.Replace(',', ':');

            if (DateTime.TryParse(timeString, out var parsedTime))
            {
                hourParsed.Invoke(parsedTime.Hour);
                minuteParsed.Invoke(parsedTime.Minute);
                secondParsed.Invoke(parsedTime.Second);
                dateTimeParsed.Invoke(parsedTime);
            }
        }

        public DateTime ParseTime(string timeString)
        {
            //replace separation characters so parser will work 
            timeString = timeString.Replace('.', ':');
            timeString = timeString.Replace(';', ':');
            timeString = timeString.Replace(',', ':');

            if (DateTime.TryParse(timeString, out var parsedTime))
            {
                return parsedTime;
            }

            Debug.LogError("Could not parse time string: " + timeString);
            return new DateTime();
        }
    }
}
