using Netherlands3D.Coordinates;
using UnityEditor;

namespace Netherlands3D.Twin.FloatingOrigin.Editor
{
    public static class CoordinateGUI
    {
        public static bool CoordinateField(bool foldout, IHasCoordinate coordinateContainer, string title = "Coordinate")
        {
            if (coordinateContainer == null) foldout = false;
    
            foldout = EditorGUILayout.Foldout(foldout, title);
            if (!foldout) return false;

            EditorGUI.indentLevel++;
            Coordinate coordinate = coordinateContainer.Coordinate;
            var coordinateSystemAsString = coordinate.CoordinateSystem != 0
                ? ((CoordinateSystem)coordinate.CoordinateSystem).ToString()
                : "Undefined";
            EditorGUILayout.LabelField("Coordinate System", coordinateSystemAsString);
            EditorGUILayout.LabelField(
                "Points",
                coordinate.PointsLength == 2 ? string.Join(", ", coordinate.value1, coordinate.value2) :
                coordinate.PointsLength == 3 ? string.Join(", ", coordinate.value1, coordinate.value2, coordinate.value3) :
                "There are no points defined"
            );
            EditorGUI.indentLevel--;

            return true;
        }

    }
}