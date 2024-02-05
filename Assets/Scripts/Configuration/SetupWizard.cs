using System;
using System.Globalization;
using System.Runtime.InteropServices;
using Netherlands3D.Coordinates;
using Netherlands3D.Twin.Functionalities;
using TMPro;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.Serialization;
using UnityEngine.UI;

namespace Netherlands3D.Twin.Configuration
{
    public class SetupWizard : MonoBehaviour
    {
        #if UNITY_WEBGL && !UNITY_EDITOR
        [DllImport("__Internal")]
        private static extern void ReplaceUrl(string url);
        #endif

        [SerializeField] private Configuration configuration;

        [Header("References")] 
        [SerializeField] private TMP_InputField originXField;
        [SerializeField] private TMP_InputField originYField;
        [SerializeField] private TMP_InputField shareUrlField;

        [SerializeField] private GameObject featureList;

        [FormerlySerializedAs("featureSelectionPrefab")]
        [Header("Prefab")] [SerializeField] private FunctionalitySelection functionalitySelectionPrefab;

        [Header("Events")]
        public UnityEvent OnSettingsChanged = new UnityEvent();

        private void Start()
        {
            originXField.text = configuration.Origin.Points[0].ToString(CultureInfo.InvariantCulture);
            originXField.onValueChanged.AddListener(OnOriginXChanged);

            originYField.text = configuration.Origin.Points[1].ToString(CultureInfo.InvariantCulture);
            originYField.onValueChanged.AddListener(OnOriginYChanged);

            foreach (var availableFeature in configuration.Functionalities)
            {
                FunctionalitySelection functionalitySelection = Instantiate(functionalitySelectionPrefab, featureList.transform);
                functionalitySelection.Init(availableFeature);

                functionalitySelection.Toggle.onValueChanged.AddListener(value => OnFunctionalityChanged(availableFeature, value));
                functionalitySelection.Button.onClick.AddListener(() => OnFunctionalitySelected(availableFeature));
            }
        }

        /// <summary>
        /// Assign listeners to the ScriptableObject in onEnable because they need to be explicitly removed
        /// in OnDisable to prevent lingering listeners on the scriptable object as soon as this object is destroyed.
        /// </summary>
        private void OnEnable()
        {
            configuration.OnOriginChanged.AddListener(ValidateRdCoordinates);
            configuration.OnOriginChanged.AddListener(UpdateShareUrlWhenOriginChanges);
            configuration.OnTitleChanged.AddListener(UpdateShareUrlWhenTitleChanges);
            foreach (var availableFeature in configuration.Functionalities)
            {
                availableFeature.OnEnable.AddListener(UpdateShareUrlWhenFeatureChanges);
                availableFeature.OnDisable.AddListener(UpdateShareUrlWhenFeatureChanges);
            }
        }

        private void ValidateRdCoordinates(Coordinate coordinates)
        {
            // TODO: IsValid is broken.. whoops!
            // var validRdCoordinates = EPSG7415.IsValid(coordinates.ToVector3RD());
            var validRdCoordinates = true;
            originXField.textComponent.color = validRdCoordinates ? Color.black : Color.red;
            originYField.textComponent.color = validRdCoordinates ? Color.black : Color.red;
        }

        /// <summary>
        /// Explicitly remove listeners on the ScriptableObject to prevent lingering listeners on the scriptable object
        /// as soon as this object is destroyed.
        /// </summary>
        private void OnDisable()
        {
            configuration.OnOriginChanged.RemoveListener(ValidateRdCoordinates);
            configuration.OnOriginChanged.RemoveListener(UpdateShareUrlWhenOriginChanges);
            configuration.OnTitleChanged.RemoveListener(UpdateShareUrlWhenTitleChanges);
            foreach (var availableFeature in configuration.Functionalities)
            {
                availableFeature.OnEnable.RemoveListener(UpdateShareUrlWhenFeatureChanges);
                availableFeature.OnDisable.RemoveListener(UpdateShareUrlWhenFeatureChanges);
            }
        }

        public void UpdateStartingPositionWithoutNotify(Coordinate coordinate)
        {
            var currentAltitude = configuration.Origin.Points[2];
            coordinate = CoordinateConverter.ConvertTo(coordinate, CoordinateSystem.RD);
            
            // Setting a starting position's altitude to 0 shouldn't happen, if we detect this as an artefact of
            // the conversion process or an actual intention, we reinstate the original altitude.
            if (Mathf.Approximately((float)coordinate.Points[2], 0f))
            {
                coordinate.Points[2] = currentAltitude;
            }

            var roundedCoordinate = new Coordinate(CoordinateSystem.RD, (int)coordinate.Points[0], (int)coordinate.Points[1], (int)coordinate.Points[2]);

            originXField.SetTextWithoutNotify(roundedCoordinate.Points[0].ToString(CultureInfo.InvariantCulture));
            originYField.SetTextWithoutNotify(roundedCoordinate.Points[1].ToString(CultureInfo.InvariantCulture));
            configuration.Origin = coordinate;
        }

        private void UpdateShareUrlWhenFeatureChanges()
        {
            UpdateShareUrl();
        }

        private void UpdateShareUrlWhenOriginChanges(Coordinate origin)
        {
            UpdateShareUrl();
        }

        private void UpdateShareUrlWhenTitleChanges(string title)
        {
            UpdateShareUrl();
        }

        public void UpdateShareUrl()
        {
            shareUrlField.text = Uri.UnescapeDataString(configuration.ToQueryString());
            #if UNITY_WEBGL && !UNITY_EDITOR
            ReplaceUrl($"./{Uri.UnescapeDataString(configuration.ToQueryString())}");
            #endif
        }

        private void OnFunctionalityChanged(Functionality availableFunctionality, bool value)
        {
            availableFunctionality.IsEnabled = value;

            OnSettingsChanged.Invoke();
        }

        private void OnFunctionalitySelected(Functionality functionality)
        {
            //TODO: Display information about the functionality
            Debug.Log("Show information about " + functionality.Title);
        }

        private void OnOriginYChanged(string value)
        {
            int.TryParse(value, out int y);
            configuration.Origin = new Coordinate(CoordinateSystem.RD, (int)configuration.Origin.Points[0], y, 300);

            OnSettingsChanged.Invoke();
        }

        private void OnOriginXChanged(string value)
        {
            int.TryParse(value, out int x);
            configuration.Origin = new Coordinate(CoordinateSystem.RD, x, (int)configuration.Origin.Points[1], 300);

            OnSettingsChanged.Invoke();
        }
    }
}