using Netherlands3D.Coordinates;
using UnityEditor;
using UnityEngine;

namespace Netherlands3D.Twin.FloatingOrigin.Editor
{
    [CustomEditor(typeof(Origin))]
    public class OriginInspector : UnityEditor.Editor
    {
        private bool showCoordinate = true;

        public override void OnInspectorGUI()
        {
            DrawDefaultInspector();

            showCoordinate = CoordinateGUI.CoordinateField(showCoordinate, (IHasCoordinate)target);
        }
    }
}