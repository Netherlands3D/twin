using Netherlands3D.Coordinates;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using TMPro;
using UnityEngine;

namespace Netherlands3D.Twin.UI
{
    public class BottomBar : MonoBehaviour
    {
        [Header("Camera coordinates")]
        [SerializeField] private TextMeshProUGUI coordinatesText;
        [SerializeField] private string coordinateFormat = "x{0} y{1} z{2}";

        private string[] cachedValues = new string[10] { "0", "1", "2", "3", "4", "5", "6", "7", "8", "9" };
        private string[] currentSet = new string[10];
        private string[] xyz = new string[3] { "x", "y", "z" };
        private string negative = "-";
        private string space = " ";

        private StringBuilder builder;
        private Vector3Int lastPosition;

        private void Start()
        {
            builder = new StringBuilder();
        }
        public void Update()
        {
            ApplyCameraPositionToText();
        }

        private void ApplyCameraPositionToText()
        {
            //Use coordinate convert to convert camera to rd coordinates
            var cameraCoordinate = new Coordinate(
                CoordinateSystem.Unity,
                Camera.main.transform.position.x,
                Camera.main.transform.position.y,
                Camera.main.transform.position.z
             );
            var rd = CoordinateConverter.ConvertTo(cameraCoordinate, CoordinateSystem.RDNAP);

            Vector3Int position = new Vector3Int(Mathf.RoundToInt((float)rd.value1), Mathf.RoundToInt((float)rd.value2), Mathf.RoundToInt((float)rd.value3));
            if (lastPosition != position)
            {
                builder.Clear();
                AppendValueString(position.x, xyz[0], builder);
                AppendValueString(position.z, xyz[1], builder);
                AppendValueString(position.y, xyz[2], builder);
                coordinatesText.text = builder.ToString();
                lastPosition = position;
            }            
        }

        private void AppendValueString(int value, string dimension, StringBuilder builder)
        {
            bool neg;
            int count;
            for (int i = 0; i < currentSet.Length; i++)
                currentSet[i] = null;
            builder.Append(dimension);
            string[] result = GetNumberString(value, currentSet, out neg, out count);
            if (neg)
                builder.Append(negative);
            for (int i = 0; i < count; i++)
                builder.Append(currentSet[count - i - 1]);
            builder.Append(space);
        }


        private string[] GetNumberString(int number, string[] set, out bool negative, out int count)
        {
            negative = number < 0;
            number = Mathf.Abs(number);
            int index = 0;
            while (number > 0)
            {                
                int digit = number % 10;
                number /= 10;
                set[index] = cachedValues[digit];
                index++;
            }
            //set.Reverse();
            count = index;
            return set;
        }
    }
}
