using System;
using Netherlands3D.Events;
using UnityEngine;
using UnityEngine.Events;

public class TimeParser : MonoBehaviour
{
    public UnityEvent<int> hourParsed;
    public UnityEvent<int> minuteParsed;
    public UnityEvent<int> secondParsed;

    public void ParseTime(string timeString)
    {
        Debug.Log("parsing: " + timeString);
        string[] formats = { "HH:mm", "HH.mm", "HH;mm", "HH,mm", "H:mm", "H.mm", "H;mm", "H,mm", "H:m", "H.m", "H;m", "H,m" };
        if (DateTime.TryParseExact(timeString, formats, null, System.Globalization.DateTimeStyles.None, out DateTime parsedTime))
        {
            Debug.Log("parsed time using custom format: " + parsedTime);
            hourParsed.Invoke(parsedTime.Hour);
            minuteParsed.Invoke(parsedTime.Minute);
            secondParsed.Invoke(parsedTime.Second);
        }
        else if (DateTime.TryParse(timeString, out parsedTime))
        {
            Debug.Log("parsed time: " + parsedTime);
            hourParsed.Invoke(parsedTime.Hour);
            minuteParsed.Invoke(parsedTime.Minute);
            secondParsed.Invoke(parsedTime.Second);
        }
    }
}