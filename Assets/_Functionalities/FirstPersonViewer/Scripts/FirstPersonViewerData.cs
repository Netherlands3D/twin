using Netherlands3D.FirstPersonViewer.Events;
using System;
using UnityEngine;

namespace Netherlands3D.FirstPersonViewer
{
    //Not a big fan of this
    public class FirstPersonViewerData : MonoBehaviour
    {
        public static FirstPersonViewerData Instance { private set; get; }

        public float ViewHeight { private set; get; }
        public float FOV { private set; get; }
        public float Speed { private set; get; } 

        private void Awake()
        {
            if (Instance == null) Instance = this;
            else Destroy(this);
        }

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
