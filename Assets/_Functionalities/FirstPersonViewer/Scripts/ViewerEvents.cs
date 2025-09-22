using System;
using UnityEngine;
using Netherlands3D.FirstPersonViewer.ViewModus;

namespace Netherlands3D.FirstPersonViewer.Events
{
    public static class ViewerEvents
    {
        public static Action OnViewerEntered;
        public static Action OnViewerExited;

        public static Action<MovementPresets> OnMovementPresetChanged;
        public static Action<CameraConstrain> OnChangeCameraConstrain;
        public static Action<float> OnViewheightChanged;
        public static Action<float> OnFOVChanged;
        public static Action<float> OnSpeedChanged;

        public static Action<Vector3> OnCameraRotation;

        public static Action OnResetToStart;
        public static Action OnSetCameraNorth;
    }
}
