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
        public List<Vector3> SelectedArea { get => selectedArea; private set => selectedArea = value; }

        public UnityEvent<List<Vector3>> OnSelectionChanged = new();
        public UnityEvent OnSelectionCleared = new();

        public void SetSelection(List<Vector3> selectedArea)
        {
            this.SelectedArea = selectedArea;

            OnSelectionChanged.Invoke(selectedArea);
        }

        public void DownloadAreaAsCollada()
        {
            //Find all gameobjects and export their meshes
        }

        public void ClearSelection()
        {
            SelectedArea.Clear();
            OnSelectionCleared.Invoke();
        }
    }
}
