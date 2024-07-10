using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;

namespace Netherlands3D.Twin
{
    public class OpenTygronAdapter : MonoBehaviour
    {
        [SerializeField] private TygronImportAdapter tygronImportAdapter;
        public string Url = "";

        [SerializeField] private UnityEvent onProjectParsed;
        [SerializeField] private UnityEvent onProjectFailed;

        private void OnEnable()
        {
            tygronImportAdapter.onProjectParsed.AddListener(OnProjectParsed);
            tygronImportAdapter.onProjectFailed.AddListener(OnProjectFailed);
        }

        private void OnDisable()
        {
            tygronImportAdapter.onProjectParsed.RemoveListener(OnProjectParsed);
            tygronImportAdapter.onProjectFailed.RemoveListener(OnProjectFailed);
        }

        private void OnProjectParsed()
        {
            Debug.Log("Tygron project parsed");
        }

        private void OnProjectFailed()
        {
            Debug.LogError("Tygron project failed");
        }

        public void SetURL(string url)
        {
            Url = url;
        }

        public void OpenProject()
        {
            tygronImportAdapter.OpenProject(Url, this);
        }
    }
}
