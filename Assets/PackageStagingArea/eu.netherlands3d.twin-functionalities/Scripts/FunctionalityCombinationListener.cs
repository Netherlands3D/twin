using System;
using System.Linq;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.Serialization;

namespace Netherlands3D.Twin.Functionalities
{
    /// <summary>
    /// This class is used to listen to specific combinations of enabled/disabled functionalities
    /// </summary>
    public class FunctionalityCombinationListener : MonoBehaviour
    {
        [FormerlySerializedAs("feature")]
        public Combination[] functionalityCombinations;

        [Serializable]
        public class Combination{
            [HideInInspector] public string name = "";
            public Functionality[] enabledFunctionalities;
            public Functionality[] disabledFunctionalities;

            public UnityEvent Then = new ();
        }

        private void Awake()
        {
            //Create listeners for all functionalities within combinations to listen to their enable/disable events
            AddFunctionalityListeners();
            CheckIfCombinationIsTrue();
        }

        private void OnValidate() {
            if(functionalityCombinations == null) return;

            bool nextIf = false;
            foreach (var combination in functionalityCombinations)
            {
                var anyFunctionalityNull = combination.enabledFunctionalities.Any(f => f == null) || combination.disabledFunctionalities.Any(f => f == null);
                if(anyFunctionalityNull) continue;

                combination.name = (nextIf ? " Else If " : "If ") +  string.Join(" && ", combination.enabledFunctionalities.Select(f => f.name)) + " == enabled)";
                if(combination.disabledFunctionalities.Length > 0){
                    combination.name += ", && (" + string.Join(" && ", combination.disabledFunctionalities.Select(f => f.name)) + " == disabled)";
                }

                nextIf = true;
            }
        }

        private void AddFunctionalityListeners()
        {
            foreach (var combination in functionalityCombinations)
            {
                foreach (var functionality in combination.enabledFunctionalities)
                {
                    functionality.OnEnable.AddListener(EnableFunctionality);
                }
                foreach (var functionality in combination.disabledFunctionalities)
                {
                    functionality.OnDisable.AddListener(DisableFunctionality);
                }
            }
        }

        private void RemoveFunctionalityListeners()
        {
            foreach (var combination in functionalityCombinations)
            {
                foreach (var functionality in combination.enabledFunctionalities)
                {
                    functionality.OnEnable.RemoveListener(EnableFunctionality);
                }
                foreach (var functionality in combination.disabledFunctionalities)
                {
                    functionality.OnDisable.RemoveListener(DisableFunctionality);
                }
            }
        }

        private void CheckIfCombinationIsTrue()
        {
            foreach (var combination in functionalityCombinations)
            {
                var enableFunctionalitiesAreEnabled = combination.enabledFunctionalities.Length == 0 || combination.enabledFunctionalities.All(functionality => functionality.IsEnabled);
                var disableFunctionalitiesAreDisabled = combination.disabledFunctionalities.Length == 0 || combination.disabledFunctionalities.All(functionality => !functionality.IsEnabled);

                if (enableFunctionalitiesAreEnabled && disableFunctionalitiesAreDisabled)
                {
                    combination.Then.Invoke();
                    return;
                }
            }
        }

        private void EnableFunctionality()
        {
            CheckIfCombinationIsTrue();
        }

        private void DisableFunctionality()
        {
           CheckIfCombinationIsTrue();
        }

        private void OnDestroy()
        {
            RemoveFunctionalityListeners();
        }
    }
}