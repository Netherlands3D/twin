using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Netherlands3D.Functionalities.Sun
{
    public class DateTimeExtract : MonoBehaviour
    {
        public enum ExtractType
        {
            SECONDS,
            MINUTES,
            HOURS,
            DAYS,
            MONTHS,
            YEARS,
            TIME
        }

        [SerializeField] private ExtractType extractType;

        private InputField field;
        private TMP_InputField tmp_field;

        private void Start()
        {
            field = GetComponent<InputField>();
            tmp_field = GetComponent<TMP_InputField>();
        }

        public void ExtractFromDateTime(DateTime dateTime)
        {
            string extractValue = string.Empty;
            switch (extractType)
            {
                case ExtractType.SECONDS:
                    extractValue = dateTime.Second.ToString();
                    break;
                case ExtractType.MINUTES:
                    extractValue = dateTime.Minute.ToString();
                    break;
                case ExtractType.HOURS:
                    extractValue = dateTime.Hour.ToString();
                    break;
                case ExtractType.DAYS:
                    extractValue = dateTime.Day.ToString();
                    break;
                case ExtractType.MONTHS:
                    extractValue = dateTime.Month.ToString();
                    break;
                case ExtractType.YEARS:
                    extractValue = dateTime.Year.ToString();
                    break;
                case ExtractType.TIME:
                    extractValue = dateTime.ToString("HH:mm");
                    break;
                default:
                    throw new Exception("Impossible case found, this shouldn't happen!");
            }

            if (field && !field.isFocused)
            {
                field.text = extractValue;
            }

            if (tmp_field && !tmp_field.isFocused)
            {
                tmp_field.text = extractValue;
            }
        }
    }
}
