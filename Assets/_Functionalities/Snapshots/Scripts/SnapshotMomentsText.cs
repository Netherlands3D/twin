using Netherlands3D.Snapshots;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using TMPro;
using UnityEngine;
using static Netherlands3D.Snapshots.PeriodicSnapshots;


namespace Netherlands3D.Functionalities.Snapshots
{
    [RequireComponent(typeof(TextMeshProUGUI))]
    public class SnapshotsMomentsText : MonoBehaviour
    {
        private PeriodicSnapshots snapshots;
        private const string dayMonthSeperator = "-";
        private const string aboutString = " om ";
        private const string timeSuffix = ":00";
        private TextMeshProUGUI textComponent;

        private void Start()
        {
            snapshots = FindObjectOfType<PeriodicSnapshots>();
            textComponent = GetComponent<TextMeshProUGUI>();
            ApplyMomentsText();
        }

        public void ApplyMomentsText()
        {
            if(snapshots != null) 
            {
                StringBuilder builder = new StringBuilder();
                List<Moment> moments = snapshots.Moments;
                moments.Sort((a, b) => a.ToDateTime().CompareTo(b.ToDateTime()));
                foreach (Moment moment in moments)
                {
                    //example     21-03 om 12:00
                    builder.Append(moment.day.ToString("D2"));
                    builder.Append(dayMonthSeperator);
                    builder.Append(moment.month.ToString("D2"));
                    builder.Append(aboutString);
                    builder.Append(moment.hour.ToString());
                    builder.AppendLine(timeSuffix);
                }

                textComponent.text = builder.ToString();
            }
        }

        private void OnValidate()
        {
            ApplyMomentsText();
        }
    }
}
