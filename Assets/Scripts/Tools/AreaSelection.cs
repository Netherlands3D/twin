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
        private List<Vector3> selectedArea = new();
        [HideInInspector] public List<Vector3> SelectedArea { get => selectedArea; private set => selectedArea = value; }

        [Header("Invoke events")]
        public UnityEvent<List<Vector3>> OnSelectionChanged = new();
        public UnityEvent<ExportFormat> OnExportFormatChanged = new();
        public UnityEvent OnSelectionCleared = new();

        private ExportFormat selectedExportFormat = ExportFormat.Collada;


        public void SetSelection(List<Vector3> selectedArea)
        {
            this.SelectedArea = selectedArea;

            OnSelectionChanged.Invoke(selectedArea);
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
            SelectedArea.Clear();
            OnSelectionCleared.Invoke();
        }
    }
}
