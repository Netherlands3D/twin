using System;
using UnityEngine;
using Netherlands3D.FirstPersonViewer.ViewModus;
using System.Collections.Generic;

namespace Netherlands3D.FirstPersonViewer.Events
{
    public static class ViewerEvents
    {
        public static Action OnViewerEntered;
        public static Action OnViewerExited;

        public static Action OnViewerSetupComplete;

        public static Action<MovementPresets> OnMovementPresetChanged;
        public static Action<CameraConstrain> OnChangeCameraConstrain;

        public static Action<string, object> onSettingChanged;

        public static Action<Vector3> OnCameraRotation;

        public static Action OnResetToStart;
        public static Action OnResetToGround;
        public static Action OnSetCameraNorth;
        public static Action OnHideUI;

        public static Action<float> ExitDuration;
    }
}
