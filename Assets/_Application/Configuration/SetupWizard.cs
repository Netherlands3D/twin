using System;
using System.Globalization;
using System.Runtime.InteropServices;
using Netherlands3D.Coordinates;
using Netherlands3D.Minimap;
using Netherlands3D.Twin.Cameras;
using Netherlands3D.Twin.FloatingOrigin;
using Netherlands3D.Twin.Functionalities;
using Netherlands3D.Twin.Projects;
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

        private bool urlNeedsUpdate = false;
        private int urlUpdateFrameCooldown = 60;
        private int lastUrlUpdate = 0;

        private void Start()
        {
            var origin = new Coordinate(CoordinateSystem.RDNAP, ProjectData.Current.CameraPosition);

            originXField.text = origin.easting.ToString(CultureInfo.InvariantCulture);
            originXField.onEndEdit.AddListener(OnOriginXChanged);

            originYField.text = origin.northing.ToString(CultureInfo.InvariantCulture);
            originYField.onEndEdit.AddListener(OnOriginYChanged);

            ProjectData.Current.OnCameraPositionChanged.Invoke(origin);

            functionalitiesPane.Init(configuration.Functionalities);
            functionalitiesPane.Toggled.AddListener(functionality => OnSettingsChanged.Invoke());
        }

        private void Update()
        {
            if(originXField.isFocused || originYField.isFocused)
                return;

            //If we are not setting the origin from the coordinate input fields; use the origin of the camera position
            var cameraCoordinate = new Coordinate(Camera.main.transform.position).Convert(CoordinateSystem.RDNAP);
            ProjectData.Current.CameraPosition = new double[]
            {
                cameraCoordinate.value1, cameraCoordinate.value2, cameraCoordinate.value3
            };
            
            //Update url with some cooldown (browsers do not like setting url too often)
            if(urlNeedsUpdate && Time.frameCount - lastUrlUpdate > urlUpdateFrameCooldown)
            {
                UpdateShareUrl();
                lastUrlUpdate = Time.frameCount;
            }
        }

        /// <summary>
        /// Assign listeners to the ScriptableObject in onEnable because they need to be explicitly removed
        /// in OnDisable to prevent lingering listeners on the scriptable object as soon as this object is destroyed.
        /// </summary>
        private void OnEnable()
        {
            ProjectData.Current.OnCameraPositionChanged.AddListener(ValidateRdCoordinate);
            ProjectData.Current.OnCameraPositionChanged.AddListener(UpdateInterfaceToNewOrigin);
            foreach (var availableFunctionality in configuration.Functionalities)
            {
                availableFunctionality.OnEnable.AddListener(UpdateShareUrlWhenFunctionalityChanges);
                availableFunctionality.OnDisable.AddListener(UpdateShareUrlWhenFunctionalityChanges);
            }
        }

        private void ValidateRdCoordinate(Coordinate coordinate)
        {
            var rd = coordinate.Convert(CoordinateSystem.RDNAP);
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
            ProjectData.Current.OnCameraPositionChanged.RemoveListener(ValidateRdCoordinate);
            ProjectData.Current.OnCameraPositionChanged.RemoveListener(UpdateInterfaceToNewOrigin);
            foreach (var availableFunctionality in configuration.Functionalities)
            {
                availableFunctionality.OnEnable.RemoveListener(UpdateShareUrlWhenFunctionalityChanges);
                availableFunctionality.OnDisable.RemoveListener(UpdateShareUrlWhenFunctionalityChanges);
            }
        }

        public void UpdateInterfaceToNewOrigin(Coordinate coordinate)
        {
            var convertedCoordinate = CoordinateConverter.ConvertTo(coordinate, CoordinateSystem.RD);

            var roundedEasting = (int)convertedCoordinate.easting;
            var roundedNorthing = (int)convertedCoordinate.northing;
            
            var xText = roundedEasting.ToString(CultureInfo.InvariantCulture);
            var yText = roundedNorthing.ToString(CultureInfo.InvariantCulture);

            //Update fields and browser URL if rounded coordinate is different from current text
            if(yText != originYField.text || xText != originXField.text){
                urlNeedsUpdate = true;
                originXField.SetTextWithoutNotify(xText);
                originYField.SetTextWithoutNotify(yText);
            }
        }

        private void UpdateShareUrlWhenFunctionalityChanges()
        {
            UpdateShareUrl();
        }

        public void UpdateShareUrl()
        {
            urlNeedsUpdate = false;
            
            var queryString = Uri.UnescapeDataString(configuration.ToQueryString());
            #if UNITY_WEBGL && !UNITY_EDITOR
            ReplaceUrl($"./{queryString}");
            #else
            Debug.Log($"Update url query to ./{queryString}");
            #endif
        }

        private void OnOriginYChanged(string value)
        {
            if (!int.TryParse(value, out int y))
                return;
            
            var mainCam = Camera.main;
            
            var cameraPosition = mainCam.transform.position;
            var cameraRD = new Coordinate(cameraPosition).Convert(CoordinateSystem.RDNAP);
            var targetCoordinate = new Coordinate(CoordinateSystem.RDNAP, cameraRD.easting, y, cameraRD.height);
            mainCam.GetComponent<WorldTransform>().MoveToCoordinate(targetCoordinate);

            OnSettingsChanged.Invoke();
        }

        private void OnOriginXChanged(string value)
        {
            if (!int.TryParse(value, out int x))
                return;

            var mainCam = Camera.main;
            
            var cameraPosition = mainCam.transform.position;
            var cameraRD = new Coordinate(cameraPosition).Convert(CoordinateSystem.RDNAP);
            var targetCoordinate = new Coordinate(CoordinateSystem.RDNAP, x, cameraRD.northing, cameraRD.height);
            mainCam.GetComponent<WorldTransform>().MoveToCoordinate(targetCoordinate);

            OnSettingsChanged.Invoke();
        }
    }
}