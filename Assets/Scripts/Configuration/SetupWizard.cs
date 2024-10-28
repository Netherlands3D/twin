using System;
using System.Globalization;
using System.Runtime.InteropServices;
using Netherlands3D.Coordinates;
using Netherlands3D.Minimap;
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

            originXField.text = origin.Points[0].ToString(CultureInfo.InvariantCulture);
            originXField.onEndEdit.AddListener(OnOriginXChanged);

            originYField.text = origin.Points[1].ToString(CultureInfo.InvariantCulture);
            originYField.onEndEdit.AddListener(OnOriginYChanged);

            ProjectData.Current.OnCameraPositionChanged.Invoke(origin);

            functionalitiesPane.Init(configuration.Functionalities);
            functionalitiesPane.Toggled.AddListener(functionality => OnSettingsChanged.Invoke());

            this.gameObject.SetActive(false);
        }

        private void Update()
        {
            if(originXField.isFocused || originYField.isFocused)
                return;

            //If we are not setting the origin from the coordinate input fields; use the origin of the camera position
            var cameraCoordinate = new Coordinate(CoordinateSystem.Unity, Camera.main.transform.position.x, Camera.main.transform.position.y, Camera.main.transform.position.z);
            ProjectData.Current.CameraPosition = cameraCoordinate.Convert(CoordinateSystem.RDNAP).Points;
            
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
            int.TryParse(value, out int y);

            var cameraPosition = Camera.main.transform.position;
            var cameraRD = new Coordinate(cameraPosition).Convert(CoordinateSystem.RD);
            cameraRD.Points[1] = y;

            var newCameraCoordinate = CoordinateConverter
                .ConvertTo(cameraRD, CoordinateSystem.Unity)
                .ToVector3();

            newCameraCoordinate.y = cameraPosition.y;

            Camera.main.transform.position = newCameraCoordinate;

            OnSettingsChanged.Invoke();
        }

        private void OnOriginXChanged(string value)
        {
            int.TryParse(value, out int x);

            var cameraPosition = Camera.main.transform.position;
            var cameraRD = new Coordinate(cameraPosition).Convert(CoordinateSystem.RD);
            cameraRD.Points[0] = x;

            var newCameraCoordinate = CoordinateConverter
                .ConvertTo(cameraRD, CoordinateSystem.Unity)
                .ToVector3();

            newCameraCoordinate.y = cameraPosition.y;

            Camera.main.transform.position = newCameraCoordinate;

            OnSettingsChanged.Invoke();
        }
    }
}