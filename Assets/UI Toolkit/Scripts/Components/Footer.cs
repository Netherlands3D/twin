using Netherlands3D.Coordinates;
using Netherlands3D.Services;
using Netherlands3D.Twin.Cameras;
using Netherlands3D.Twin.Services;
using Netherlands3D.UI.ExtensionMethods;
using UnityEngine;
using UnityEngine.UIElements;

namespace Netherlands3D.UI.Components
{
    [UxmlElement]
    public partial class Footer : VisualElement
    {
        private Label attributionLabel;
        private Label AttributionLabel => attributionLabel ??= this.Q<Label>("Attribution");

        private CoordinateLabel coordinateLabel;
        private CoordinateLabel CoordinateLabel => coordinateLabel ??= this.Q<CoordinateLabel>();

        [UxmlAttribute("attribution")]
        public string Attribution
        {
            get => AttributionLabel.text;
            set => AttributionLabel.text = value;
        }

        [UxmlAttribute("x")]
        public float X
        {
            get => CoordinateLabel.X;
            set => CoordinateLabel.X = value;
        }

        [UxmlAttribute("y")]
        public float Y
        {
            get => CoordinateLabel.Y;
            set => CoordinateLabel.Y = value;
        }

        [UxmlAttribute("z")]
        public float Z
        {
            get => CoordinateLabel.Z;
            set => CoordinateLabel.Z = value;
        }

        public Footer()
        {
            this.CloneComponentTree("Components");
            this.AddComponentStylesheet("Components");
            
            // Ensure attribution is not visible upon start
            OnShowAttribution("");
            
            // Cannot disable because events do not support replaying, meaning you could lose attribution information
            // if attribution changed while object is disabled
            schedule.Execute(() =>
            {
                ServiceLocator.GetService<CameraService>()?.OnPositionChanged.AddListener(OnActiveCameraPositionChanged);
                ServiceLocator.GetService<LayerMessageService>()?.OnAttributionReceived.AddListener(OnShowAttribution);
            });
        }
        
        public void OnShowAttribution(string attribution) => Attribution = attribution;

        public void OnActiveCameraPositionChanged(Vector3 position)
        {
            Coordinate coordinate = new Coordinate(position);
            var rd = coordinate.Convert(CoordinateSystem.RDNAP);

            X = (float)rd.easting;
            Y = (float)rd.northing;
            Z = (float)rd.height;
        }
    }
}