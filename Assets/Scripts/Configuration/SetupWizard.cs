using System;
using System.Globalization;
using System.Runtime.InteropServices;
using Netherlands3D.Coordinates;
using Netherlands3D.Minimap;
using Netherlands3D.Twin.Functionalities;
using TMPro;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.EventSystems;
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
        [SerializeField] private AddressSearch.AddressSearch addressSearchField;
        [SerializeField] private WMTSMap minimap;
        [SerializeField] private FunctionalitiesPane functionalitiesPane;

        [Header("Events")]
        public UnityEvent OnSettingsChanged = new UnityEvent();


        private void Start()
        {
            originXField.text = configuration.Origin.Points[0].ToString(CultureInfo.InvariantCulture);
            originXField.onEndEdit.AddListener(OnOriginXChanged);

            originYField.text = configuration.Origin.Points[1].ToString(CultureInfo.InvariantCulture);
            originYField.onEndEdit.AddListener(OnOriginYChanged);

            configuration.OnOriginChanged.Invoke(configuration.Origin);

            functionalitiesPane.Init(configuration.Functionalities);
            functionalitiesPane.Toggled.AddListener(functionality => OnSettingsChanged.Invoke());
        }

        private void Update()
        {
            if(originXField.isFocused || originYField.isFocused)
                return;

            //If we are not setting the origin from the coordinate input fields; use the origin of the camera position
            var cameraCoordinate = new Coordinate(CoordinateSystem.Unity, Camera.main.transform.position.x, Camera.main.transform.position.y, Camera.main.transform.position.z);
            configuration.Origin = cameraCoordinate;
        }

        /// <summary>
        /// Assign listeners to the ScriptableObject in onEnable because they need to be explicitly removed
        /// in OnDisable to prevent lingering listeners on the scriptable object as soon as this object is destroyed.
        /// </summary>
        private void OnEnable()
        {
            configuration.OnOriginChanged.AddListener(ValidateRdCoordinate);
            configuration.OnOriginChanged.AddListener(UpdateInterfaceToNewOrigin);
            configuration.OnTitleChanged.AddListener(UpdateShareUrlWhenTitleChanges);
            foreach (var availableFunctionality in configuration.Functionalities)
            {
                availableFunctionality.OnEnable.AddListener(UpdateShareUrlWhenFunctionalityChanges);
                availableFunctionality.OnDisable.AddListener(UpdateShareUrlWhenFunctionalityChanges);
            }
        }

        private void ValidateRdCoordinate(Coordinate coordinate)
        {
            var rd = coordinate.Convert(CoordinateSystem.RD);
            var validRdCoordinates = rd.IsValid();
            
            originXField.textComponent.color = validRdCoordinates ? Color.black : Color.red;
            originYField.textComponent.color = validRdCoordinates ? Color.black : Color.red;
        }

        /// <summary>
        /// Explicitly remove listeners on the ScriptableObject to prevent lingering listeners on the scriptable object
        /// as soon as this object is destroyed.
        /// </summary>
        private void OnDisable()
        {
            configuration.OnOriginChanged.RemoveListener(ValidateRdCoordinate);
            configuration.OnOriginChanged.RemoveListener(UpdateInterfaceToNewOrigin);
            configuration.OnTitleChanged.RemoveListener(UpdateShareUrlWhenTitleChanges);
            foreach (var availableFunctionality in configuration.Functionalities)
            {
                availableFunctionality.OnEnable.RemoveListener(UpdateShareUrlWhenFunctionalityChanges);
                availableFunctionality.OnDisable.RemoveListener(UpdateShareUrlWhenFunctionalityChanges);
            }
        }

        public void UpdateInterfaceToNewOrigin(Coordinate coordinate)
        {
            var convertedCoordinate = CoordinateConverter.ConvertTo(coordinate, CoordinateSystem.RD);
            var roundedCoordinate = new Coordinate(
                convertedCoordinate.CoordinateSystem, 
                (int)convertedCoordinate.Points[0], 
                (int)convertedCoordinate.Points[1], 
                (int)convertedCoordinate.Points[2]
            );

            var xText = roundedCoordinate.Points[0].ToString(CultureInfo.InvariantCulture);
            var yText = roundedCoordinate.Points[1].ToString(CultureInfo.InvariantCulture);

            //Update fields and browser URL if rounded coordinate is different from current text
            if(yText != originYField.text || xText != originXField.text){
                UpdateShareUrlWhenOriginChanges(coordinate);
                originXField.SetTextWithoutNotify(xText);
                originYField.SetTextWithoutNotify(yText);
            }
        }

        private void UpdateShareUrlWhenFunctionalityChanges()
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
            var queryString = Uri.UnescapeDataString(configuration.ToQueryString());
            #if UNITY_WEBGL && !UNITY_EDITOR
            ReplaceUrl($"./{queryString}");
            #endif
        }

        private void OnOriginYChanged(string value)
        {
            int.TryParse(value, out int y);
            configuration.Origin = new Coordinate(CoordinateSystem.RD, (int)configuration.Origin.Points[0], y, configuration.Origin.Points[2]);

            OnSettingsChanged.Invoke();
        }

        private void OnOriginXChanged(string value)
        {
            int.TryParse(value, out int x);
            configuration.Origin = new Coordinate(CoordinateSystem.RD, x, (int)configuration.Origin.Points[1], configuration.Origin.Points[2]);

            OnSettingsChanged.Invoke();
        }
    }
}