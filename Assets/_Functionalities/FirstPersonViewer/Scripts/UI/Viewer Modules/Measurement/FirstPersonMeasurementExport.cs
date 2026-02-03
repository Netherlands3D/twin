using System.Globalization;
using System.Runtime.InteropServices;
using System.Text;
using UnityEngine;

namespace Netherlands3D.FirstPersonViewer.Measurement
{
    public class FirstPersonMeasurementExport : MonoBehaviour
    {
        [DllImport("__Internal")]
        private static extern void DownloadFileImmediate(string gameObjectName, string callbackMethodName, string filename, byte[] data, int dataSize);

        [SerializeField] private FirstPersonMeasurement measurement;
        
        public string GetCSVOutputWithTotal()
        {
            var sb = new StringBuilder();
            float totalDst = 0;

            foreach (var measurement in measurement.Segments)
            {
                if (measurement.PointB != null)
                {
                    totalDst += measurement.LineDistance;

                    sb.Append(measurement.PointA.GetLetter());
                    sb.Append(';').Append(measurement.PointB.GetLetter());
                    sb.Append(';').Append(measurement.LineDistance.ToString("0.##", CultureInfo.InvariantCulture).Replace('.', ','));
                    sb.Append(';').Append(totalDst.ToString("0.##", CultureInfo.InvariantCulture).Replace('.', ','));
                    sb.AppendLine();
                }
            }

            return sb.ToString();
        }

        public void ExportToCSV()
        {
            string timestamp = System.DateTime.Now.ToString("yyyy-MM-dd_HHmm");
            string filename = $"meting_export_{timestamp}.csv";

            string csv = "Punt 1; Punt 2; Afstand in meters; Afstand vanaf A\n" + GetCSVOutputWithTotal();
            byte[] bytes = System.Text.Encoding.UTF8.GetBytes(csv);

#if UNITY_WEBGL && !UNITY_EDITOR
            DownloadFileImmediate(gameObject.name, "", filename, bytes, bytes.Length);
#elif UNITY_EDITOR
            string path = UnityEditor.EditorUtility.SaveFilePanel("Export CSV", "", filename, "csv");

            if (!string.IsNullOrEmpty(path))
            {
                System.IO.File.WriteAllBytes(path, bytes);
            }
#endif
        }
    }
}
