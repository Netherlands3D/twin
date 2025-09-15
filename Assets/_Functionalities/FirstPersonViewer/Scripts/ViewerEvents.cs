using System;
using UnityEngine;
using Netherlands3D.FirstPersonViewer.ViewModus;

namespace Netherlands3D.FirstPersonViewer.Events
{
    public static class ViewerEvents
    {
        public static Action OnViewerEntered;
        public static Action OnViewerExitd;

        public static Action ExitViewer;

        public static Action<MovementPresets> OnMovementPresetChanged;

        public static Action<float> ChangeViewHeight;
        public static Action<float> ChangeFOV;
        public static Action<float> ChangeSpeed;
    }
}
