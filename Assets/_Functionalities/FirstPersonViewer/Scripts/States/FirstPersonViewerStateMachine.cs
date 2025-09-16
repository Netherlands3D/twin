using System;
using System.Collections.Generic;
using UnityEngine;

namespace Netherlands3D.FirstPersonViewer.ViewModus
{
    public class FirstPersonViewerStateMachine
    {
        private Dictionary<Type, ViewerState> stateDictionary = new Dictionary<Type, ViewerState>();
        public ViewerState CurrentState { private set; get; }

        public FirstPersonViewerStateMachine(FirstPersonViewer player, FirstPersonViewerInput input, Type startState, params ViewerState[] states)
        {
            foreach (ViewerState state in states)
            {
                state.Initialize(this, player, input);
                stateDictionary.Add(state.GetType(), state);
            }

            if(startState != null) SwitchState(startState);
        }

        public void OnUpdate()
        {
            CurrentState?.OnUpdate();
        }

        public void SwitchState(Type newStateType)
        {
            if (!stateDictionary.ContainsKey(newStateType))
            {
                Debug.Log($"State {newStateType} not found.");
            }

            if(CurrentState != null) CurrentState.isAcive = false;
            CurrentState?.OnExit();
            CurrentState = stateDictionary[newStateType];
            CurrentState?.OnEnter();
            CurrentState.isAcive = true;
        }
    }
}
