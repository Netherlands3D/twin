using System.IO;
using UnityEngine;

namespace Netherlands3D.CityJson.Structure
{
    public static class HandleTextFile
    {
        //Write some text to a file
        public static void WriteString(string fileName, string content)
        {
            StreamWriter writer = new StreamWriter(fileName, false);
            writer.WriteLine(content);
            writer.Close();
            Debug.Log("saved file to: " + fileName);
        }
    }
}
