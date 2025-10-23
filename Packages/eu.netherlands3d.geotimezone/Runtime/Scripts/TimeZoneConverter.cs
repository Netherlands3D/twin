using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using Newtonsoft.Json.Linq;
using UnityEngine;

//This is needed because TimeZoneInfo.FindSystemTimeZoneById does not work in WebGL and we need to make our own conversion table to be able to convert from local time to UTC
namespace GeoTimeZone
{
    public class TimeZoneConversionInfo
    {
        public long?[] untils;
        public double[] offsets;
        public int[] isdsts;

        public TimeZoneConversionInfo(JObject zoneInfo)
        {
            untils = zoneInfo["untils"].ToObject<long?[]>();
            offsets = zoneInfo["offsets"].ToObject<double[]>();
            isdsts = zoneInfo["isdsts"].ToObject<int[]>();
        }
    }

    public static class TimeZoneConverter
    {
        private static string currentTimeZoneId;
        private static TimeZoneConversionInfo currentTimeZoneInfo;
        private static JObject timeZoneData;

        private static bool cacheDatabase = false;

        public static bool CacheDatabase
        {
            get => cacheDatabase;
            set
            {
                if (value == cacheDatabase)
                    return;

                if (value)
                {
                    timeZoneData = ParseTimeZoneFile();
                    cacheDatabase = value;
                    return;
                }

                timeZoneData = null;
                cacheDatabase = value;
            }
        } //set to false if your time zone does not change often to free up memory and to only re-parse the file when the time zone changes.
        
        private static JObject ParseTimeZoneFile()
        {
            var compressedData = Resources.Load<TextAsset>("iana-tz-data.json.gz"); //The Time zone data is stored as GZ files, but since Unity's Resources.Load cannot recognize these, a .txt extension is added as a workaround.
            using var compressedStream = new MemoryStream(compressedData.bytes);

            using var stream = new GZipStream(compressedStream!, CompressionMode.Decompress);
            using var reader = new StreamReader(stream);
            Resources.UnloadAsset(compressedData);
            
            return JObject.Parse(reader.ReadToEnd());
        }

        public static DateTime ConvertToUTC(DateTime localTime, string localTimeZone)
        {
            if (localTimeZone != currentTimeZoneId)
            {
                currentTimeZoneInfo = GetZoneInfo(localTimeZone);
            }

            if (currentTimeZoneInfo == null)
            {
                throw new ArgumentException("Invalid time zone");
            }

            var offset = GetUTCOffset(localTime, currentTimeZoneInfo);
            return localTime + offset;
        }
        
        private static TimeZoneConversionInfo GetZoneInfo(string localTimeZone)
        {
            var data = timeZoneData;
            
            if(data == null)
                data = ParseTimeZoneFile();
            
            currentTimeZoneId = localTimeZone;
            
            var zones = data["zoneData"] as JObject;
            if (zones == null)
            {
                throw new ArgumentException("Invalid zone data format.");
            }

            // Try to split the full timezone name into continent and city if it's of the form "Europe/London"
            var timeZoneParts = localTimeZone.Split('/');
            if (timeZoneParts.Length == 2)
            {
                var continent = timeZoneParts[0]; // "Europe"
                var city = timeZoneParts[1]; // "London"

                // Check if the continent exists and contains the city
                if (zones.ContainsKey(continent))
                {
                    var continentData = zones[continent] as JObject;
                    if (continentData != null && continentData.ContainsKey(city))
                    {
                        return new TimeZoneConversionInfo(continentData[city] as JObject);
                    }
                }
            }

            // If the input is not in continent/city format, check for abbreviations and other timezone names
            if (zones.ContainsKey(localTimeZone))
            {
                return new TimeZoneConversionInfo(zones[localTimeZone] as JObject);
            }

            // If no match found, search for abbreviations or other variants in all zone data
            foreach (var continent in zones)
            {
                var continentData = continent.Value as JObject;
                if (continentData != null)
                {
                    foreach (var key in continentData)
                    {
                        var zoneData = key.Value as JObject;
                        var abbrs = zoneData["abbrs"].ToObject<List<string>>();
                        if (abbrs.Contains(localTimeZone))
                        {
                            return new TimeZoneConversionInfo(zoneData); // Return the matching zone data
                        }
                    }
                }
            }

            // If no matching time zone is found, throw an exception
            throw new ArgumentException("Invalid time zone: " + localTimeZone);
        }

        public static TimeSpan GetUTCOffset(DateTime localTime, TimeZoneConversionInfo zoneInfo)
        {
            long unixMilliseconds = new DateTimeOffset(localTime).ToUnixTimeMilliseconds();
            
            for (int i = 0; i < zoneInfo.untils.Length; i++)
            {
                if (zoneInfo.untils[i] == null || zoneInfo.untils[i].Value >= unixMilliseconds)
                {
                    return TimeSpan.FromMinutes(zoneInfo.offsets[i]);
                }
            }

            throw new InvalidOperationException("Unable to determine the time zone offset.");
        }
    }
}