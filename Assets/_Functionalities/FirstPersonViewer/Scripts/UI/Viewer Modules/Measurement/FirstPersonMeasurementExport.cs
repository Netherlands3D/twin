using System.Globalization;
using System.Runtime.InteropServices;
using UnityEngine;

namespace Netherlands3D.FirstPersonViewer.Measurement
{
    public class FirstPersonMeasurementExport : MonoBehaviour
    {
        [DllImport("__Internal")]
        private static extern void DownloadFileImmediate(string gameObjectName, string callbackMethodName, string filename, byte[] data, int dataSize);

        [SerializeField] private FirstPersonMeasurement measurement;

        public void ExportToCSV()
        {
            string timestamp = System.DateTime.Now.ToString("yyyy-MM-dd_HHmm");
            string filename = $"meting_export_{timestamp}.csv";

            string csv = "Punt 1; Punt 2; Afstand in meters; Afstand vanaf A\n";

            float totalDst = 0;
            measurement.GetMeasurementSegments().ForEach(measurement =>
            {
                if (measurement.pointB != null)
                {
                    totalDst += measurement.LineDistance;
                    csv += measurement.GetCSVOutput() + $";{totalDst.ToString("0.##", CultureInfo.InvariantCulture).Replace('.', ',')}\n";
                }
            });

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
