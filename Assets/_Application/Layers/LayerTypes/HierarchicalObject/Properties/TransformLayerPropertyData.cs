using Netherlands3D.Coordinates;
using Netherlands3D.Twin.Layers.Properties;
using Newtonsoft.Json;
using RuntimeHandle;
using System.Runtime.Serialization;
using UnityEngine;
using UnityEngine.Events;

namespace Netherlands3D.Twin.Layers.LayerTypes.HierarchicalObject.Properties
{
    [DataContract(Namespace = "https://netherlands3d.eu/schemas/projects/layers/properties", Name = "Transform")]
    public class TransformLayerPropertyData : LayerPropertyData
    {
        [DataMember] private Coordinate position;
        [DataMember] private Vector3 eulerRotation;
        [DataMember] private Vector3 localScale;
        [DataMember] private string scaleUnitCharacter;

        [DataMember] private int positionAxes = 0;
        [DataMember] private int rotationAxes = 0;
        [DataMember] private int scaleAxes = 0;

        [JsonIgnore]
        public Coordinate Position
        {
            get => position;
            set
            {
                position = value.Convert(CoordinateSystems.connectedCoordinateSystem);
                OnPositionChanged.Invoke(position);
            }
        }

        [JsonIgnore]
        public Vector3 UnityPosition => position.ToUnity();

        [JsonIgnore]
        public Vector3 EulerRotation
        {
            get => eulerRotation;
            set
            {
                eulerRotation = value;
                OnRotationChanged.Invoke(value);
            }
        }
        
        [JsonIgnore]
        public Quaternion Rotation => Quaternion.Euler(eulerRotation);

        [JsonIgnore]
        public Vector3 LocalScale
        {
            get => localScale;
            set
            {
                localScale = value;
                OnScaleChanged.Invoke(value);
            }
        }


        [JsonIgnore]
        public int PositionAxes
        {
            get => positionAxes;
            set
            {
                positionAxes = value;
                OnPositionAxesChanged.Invoke(value);
            }
        }

        [JsonIgnore]
        public int RotationAxes
        {
            get => rotationAxes;
            set
            {
                rotationAxes = value;
                OnRotationAxesChanged.Invoke(value);
            }
        }

        [JsonIgnore]
        public int ScaleAxes
        {
            get => scaleAxes;
            set
            {
                scaleAxes = value;
                OnScaleAxesChanged.Invoke(value);
            }
        }

        [JsonIgnore]
        public string ScaleUnitCharacter => scaleUnitCharacter;

        [JsonIgnore] public readonly UnityEvent<Coordinate> OnPositionChanged = new();
        [JsonIgnore] public readonly UnityEvent<Vector3> OnRotationChanged = new();
        [JsonIgnore] public readonly UnityEvent<Vector3> OnScaleChanged = new();
        [JsonIgnore] public readonly UnityEvent<int> OnPositionAxesChanged = new();
        [JsonIgnore] public readonly UnityEvent<int> OnRotationAxesChanged = new();
        [JsonIgnore] public readonly UnityEvent<int> OnScaleAxesChanged = new();

        [JsonConstructor]
        public TransformLayerPropertyData(Coordinate position, Vector3 eulerRotation, Vector3 localScale, string scaleUnitCharacter = "%")
        {
            this.position = position.Convert(CoordinateSystems.connectedCoordinateSystem);
            this.eulerRotation = eulerRotation;
            this.localScale = localScale;
            this.scaleUnitCharacter = scaleUnitCharacter;
        }
    }
}