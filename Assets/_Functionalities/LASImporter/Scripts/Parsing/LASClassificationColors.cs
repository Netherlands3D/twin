using UnityEngine;

namespace Netherlands3D.Functionalities.LASImporter.Parsing
{
    public static class LASClassificationColors
    {
        private static readonly Color32[] Colors =
        {
            new(190, 190, 190, 255), // Created, never classified
            new(225, 225, 225, 255), // Unclassified
            new(128, 95, 64, 255),   // Ground
            new(86, 156, 74, 255),   // Low vegetation
            new(52, 126, 55, 255),   // Medium vegetation
            new(22, 96, 43, 255),    // High vegetation
            new(201, 92, 74, 255),   // Building
            new(194, 91, 186, 255),  // Low point / noise
            new(84, 142, 206, 255),  // Model key-point
            new(240, 179, 68, 255),  // Water
            new(118, 118, 118, 255), // Rail
            new(92, 92, 92, 255),    // Road surface
            new(110, 184, 210, 255), // Overlap / reserved-ish
            new(180, 130, 90, 255),
            new(170, 120, 80, 255),
            new(160, 110, 70, 255),
            new(210, 160, 100, 255),
            new(200, 150, 90, 255),
            new(190, 140, 80, 255),
            new(180, 130, 70, 255)
        };

        public static Color32 ForClassification(byte classification)
        {
            if (classification < Colors.Length)
                return Colors[classification];

            var hue = (classification * 0.073f) % 1f;
            Color color = Color.HSVToRGB(hue, 0.55f, 0.9f);
            return color;
        }

        public static string GetName(byte classification)
        {
            return classification switch
            {
                0 => "Created, never classified",
                1 => "Unclassified",
                2 => "Ground",
                3 => "Low vegetation",
                4 => "Medium vegetation",
                5 => "High vegetation",
                6 => "Building",
                7 => "Low point / noise",
                8 => "Model key-point",
                9 => "Water",
                10 => "Rail",
                11 => "Road surface",
                _ => "Class " + classification
            };
        }
    }
}
