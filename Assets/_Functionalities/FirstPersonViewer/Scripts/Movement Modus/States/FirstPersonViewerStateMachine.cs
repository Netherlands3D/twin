using System;
using System.Collections.Generic;
using UnityEngine;

namespace Netherlands3D.FirstPersonViewer.ViewModus
{
    public class FirstPersonViewerStateMachine
    {
        public ViewerState CurrentState { private set; get; }

        public FirstPersonViewerStateMachine(FirstPersonViewer player, FirstPersonViewerInput input, params ViewerState[] states)
        {
            foreach (ViewerState state in states)
            {
                state.Initialize(player, input);
            }
        }

        public void OnUpdate()
        {
            CurrentState?.OnUpdate();
        }

        public void SwitchState(ViewerState state)
        {
            CurrentState?.OnExit();
            CurrentState = state;
            CurrentState?.OnEnter();
        }
    }
}
