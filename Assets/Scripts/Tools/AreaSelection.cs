using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;

namespace Netherlands3D.Twin
{
    [CreateAssetMenu(fileName = "AreaSelection", menuName = "Netherlands3D/Data/AreaSelection", order = 1)]
    public class AreaSelection : ScriptableObject
    {
        private Bounds selectedAreaBounds;
        private List<Vector3> selectedArea;
        [HideInInspector] public List<Vector3> SelectedArea { get => selectedArea; private set => selectedArea = value; }
        [HideInInspector] public Bounds SelectedAreaBounds { get => selectedAreaBounds; private set => selectedAreaBounds = value; }

        [Header("Invoke events")]
        public UnityEvent<List<Vector3>> OnSelectionAreaChanged = new();
        public UnityEvent<Bounds> OnSelectionAreaBoundsChanged = new();
        public UnityEvent<ExportFormat> OnExportFormatChanged = new();
        public UnityEvent OnSelectionCleared = new();

        private ExportFormat selectedExportFormat = ExportFormat.Collada;

        public void SetSelectionAreaBounds(Bounds selectedAreaBounds)
        {
            this.SelectedAreaBounds = selectedAreaBounds;
            OnSelectionAreaBoundsChanged.Invoke(this.SelectedAreaBounds);
        }

        public void SetSelectionArea(List<Vector3> selectedArea)
        {
            var bounds = new Bounds();
            foreach(var point in selectedArea)
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
            switch(selectedExportFormat)
            {
                case ExportFormat.Collada:
                    //Slice and export using collada
                    break;
                case ExportFormat.AutodeskDXF:
                    //Not implemented yet
                    Debug.LogError("DXF export is not implemented yet");
                    break;
            }
        }

        public void SetExportFormat(ExportFormat format)
        {
            selectedExportFormat = format;
            OnExportFormatChanged.Invoke(selectedExportFormat);
        }
        public void SetExportFormat(int format)
        {
            //int to enum
            selectedExportFormat = (ExportFormat)format;
        }

        public void ClearSelection()
        {
            selectedAreaBounds = new Bounds(){
                center = Vector3.zero,
                size = Vector3.zero
            };
            selectedArea.Clear();

            OnSelectionCleared.Invoke();
        }
    }
}
