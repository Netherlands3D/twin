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
        public UnityEvent OnSelectionCleared = new();

        private string currentExportFormat = "dae";
        [SerializeField] private ExportFormat[] exportFormats = new ExportFormat[]{
            new("Collada", "dae"),
            new("AutoCAD DXF", "dxf")
        };
        public ExportFormat[] ExportFormats { get => exportFormats; set => exportFormats = value; }
        public class ExportFormat{
            public string name;
            public string extension;
            public ExportFormat(string name, string extension){
                this.name = name;
                this.extension = extension;
            }
        }

        public void SetSelection(List<Vector3> selectedArea)
        {
            this.SelectedArea = selectedArea;

            OnSelectionChanged.Invoke(selectedArea);
        }

        public void Download()
        {
            var exportFormat = Array.Find(exportFormats, format => format.extension == currentExportFormat);
            if(exportFormat == null){
                Debug.LogError($"Export format {currentExportFormat} not found in exportFormats");
                return;
            }
            
            switch(currentExportFormat){
                case "dae":
                    //Clip and export to Collada

                    break;
                case "dxf":
                    //Log not implemented warning
                    Debug.LogWarning($"Export format {currentExportFormat} not implemented yet");
                    break;
            }
        }

        public void SetExportFormat(string extension)
        {
            currentExportFormat = extension;
        }

        public void ClearSelection()
        {
            SelectedArea.Clear();
            OnSelectionCleared.Invoke();
        }
    }
}
