using Netherlands3D.FirstPersonViewer.Events;
using System;
using UnityEngine;

namespace Netherlands3D.FirstPersonViewer
{
    public class FirstPersonViewerData : MonoBehaviour
    {
        public float ViewHeight { private set; get; }
        public float FOV { private set; get; }
        public float Speed { private set; get; }

        [field:SerializeField] public Camera FPVCamera { private set; get; }

        private void OnEnable()
        {
            ViewerEvents.OnViewheightChanged += OnViewHeightChanged;
            ViewerEvents.OnFOVChanged += OnFOVChanged;
            ViewerEvents.OnSpeedChanged += OnSpeedChanged;
        }

        private void OnDisable()
        {
            ViewerEvents.OnViewheightChanged -= OnViewHeightChanged;
            ViewerEvents.OnFOVChanged -= OnFOVChanged;
            ViewerEvents.OnSpeedChanged -= OnSpeedChanged;
        }

        private void OnViewHeightChanged(float height) => ViewHeight = height;

        private void OnFOVChanged(float FOV) => this.FOV = FOV;

        private void OnSpeedChanged(float speed) => Speed = speed;
    }
}
