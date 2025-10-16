using System;
using System.Collections.Generic;
using UnityEngine;

namespace Netherlands3D.FirstPersonViewer.ViewModus
{
    public class FirstPersonViewerStateMachine
    {
        private Dictionary<string, ViewerState> stateDictionary = new Dictionary<string, ViewerState>();
        public ViewerState CurrentState { private set; get; }

        public FirstPersonViewerStateMachine(FirstPersonViewer player, FirstPersonViewerInput input, Type startState, params ViewerState[] states)
        {
            foreach (ViewerState state in states)
            {
                state.Initialize(this, player, input);
                string stateName = state.GetType().Name;
                if (!stateDictionary.ContainsKey(stateName)) stateDictionary.Add(stateName, state);
            }

            if(startState != null) SwitchState(startState.Name);
        }

        public void OnUpdate()
        {
            CurrentState?.OnUpdate();
        }

        public void SwitchState(string newStateType)
        {
            if (!stateDictionary.ContainsKey(newStateType))
            {
                Debug.Log($"State {newStateType} not found.");
            }

            CurrentState?.OnExit();
            CurrentState = stateDictionary[newStateType];
            CurrentState?.OnEnter();
        }

        public void SwitchState(ViewerState state) => SwitchState(state.name);
    }
}
