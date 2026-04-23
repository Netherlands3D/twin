using UnityEngine;

namespace Netherlands3D.UI_Toolkit.Scripts
{
    public static class HexColorUtility
    {
        public static bool ParseHexColor(string hexString, out Color color)
        {
            if (!hexString.StartsWith("#"))
            {
                hexString = "#" + hexString;
            }

            if (hexString.Length != 7 && hexString.Length != 9)
            {
                Debug.LogWarning("Invalid HEX format. Ensure it is 6 or 8 characters long after '#'.");
                color = new Color();
                return false;
            }

            if (!UnityEngine.ColorUtility.TryParseHtmlString(hexString, out color))
            {
                Debug.LogWarning("Failed to parse color from hex code: " + hexString);
                return false;
            }

            return true;
        }
    }
}
