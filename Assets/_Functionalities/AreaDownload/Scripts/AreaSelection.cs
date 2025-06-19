using System.Collections.Generic;
// ReSharper disable once RedundantUsingDirective
using System.Text;
using Netherlands3D.Collada;
using UnityEngine;
using UnityEngine.Events;

#if UNITY_EDITOR
using UnityEditor;
#endif

namespace Netherlands3D.Functionalities.AreaDownload
{
    [CreateAssetMenu(fileName = "AreaSelection", menuName = "Netherlands3D/Data/AreaSelection", order = 1)]
    public class AreaSelection : ScriptableObject
    {
        private Bounds selectedAreaBounds;
        private List<Vector3> selectedArea;

        [HideInInspector]
        public List<Vector3> SelectedArea
        {
            get => selectedArea;
            private set => selectedArea = value;
        }

        [HideInInspector]
        public Bounds SelectedAreaBounds
        {
            get => selectedAreaBounds;
            private set => selectedAreaBounds = value;
        }

        [Header("Settings")] [SerializeField] private float minClipBoundsHeight = 1000.0f;

        [Header("Invoke events")] [Tooltip("Once a selection is confirmed, this is called")]
        public UnityEvent<List<Vector3>> OnSelectionAreaChanged = new();

        [Tooltip("Once a selection is confirmed, this is called")]
        public UnityEvent<Bounds> OnSelectionAreaBoundsChanged = new();

        [Tooltip("While a selection is being made, this is called")]
        public UnityEvent<Bounds> WhenSelectionAreaBoundsChanged = new();

        public UnityEvent<ExportFormat> OnExportFormatChanged = new();
        public UnityEvent<float> modelExportProgressChanged = new();
        public UnityEvent<string> modelExportStatusChanged = new();
        public UnityEvent OnSelectionCleared = new();

        private ExportFormat selectedExportFormat = ExportFormat.Collada;

        [SerializeField] private LayerMask includedLayers;

        public void SetDuringSelectionAreaBounds(Bounds selectedAreaBounds)
        {
            WhenSelectionAreaBoundsChanged.Invoke(selectedAreaBounds);
        }

        public void SetSelectionAreaBounds(Bounds selectedAreaBounds)
        {
            this.SelectedAreaBounds = selectedAreaBounds;
            OnSelectionAreaBoundsChanged.Invoke(this.SelectedAreaBounds);
        }

        public void SetSelectionArea(List<Vector3> selectedArea)
        {
            var bounds = new Bounds();
            foreach (var point in selectedArea)
            {
                bounds.Encapsulate(point);
                bounds.Encapsulate(point + Vector3.up);
            }

            this.SelectedArea = selectedArea;
            OnSelectionAreaChanged.Invoke(this.SelectedArea);

            SetSelectionAreaBounds(bounds);
        }

        public void Download()
        {
            var exportGameObject = new GameObject("Exporter");
            ModelFormatCreation exportScript;
            
            switch (selectedExportFormat)
            {
                case ExportFormat.Collada:
                    //Slice and export using collada
                    Debug.Log("Exporting Collada of area bounds: " + selectedAreaBounds);
                    exportScript = exportGameObject.AddComponent<ColladaCreation>();
                    exportScript.StartDownload(includedLayers, selectedAreaBounds, minClipBoundsHeight);
                    break;
                case ExportFormat.AutodeskDXF:
                    Debug.Log("Exporting Autodesk DXF of area bounds: " + selectedAreaBounds);
                    exportScript = exportGameObject.AddComponent<DXFCreation>();
                    exportScript.StartDownload(includedLayers, selectedAreaBounds, minClipBoundsHeight);
                    break;
            }
        }

        public void SetExportFormat(ExportFormat format)
        {
            selectedExportFormat = format;
            OnExportFormatChanged.Invoke(selectedExportFormat);
        }

        //used by the dropdown in the inspector
        public void SetExportFormat(int format)
        {
            //int to enum
            selectedExportFormat = (ExportFormat)format;
        }

        public void ClearSelection()
        {
            selectedAreaBounds = new Bounds()
            {
                center = Vector3.zero,
                size = Vector3.zero
            };
            selectedArea.Clear();

            OnSelectionCleared.Invoke();
        }
    }
}