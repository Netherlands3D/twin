using Netherlands3D.Coordinates;
using Netherlands3D.Twin.Layers.Properties;
using Newtonsoft.Json;
using RuntimeHandle;
using System.Runtime.Serialization;
using UnityEngine;
using UnityEngine.Events;

namespace Netherlands3D.Twin.Layers.LayerTypes.HierarchicalObject.Properties
{
    [DataContract(Namespace = "https://netherlands3d.eu/schemas/projects/layers/properties", Name = "TransformLock")]
    public class TransformLockLayerPropertyData : LayerPropertyData
    {       
        [DataMember] private int positionAxes = 0;
        [DataMember] private int rotationAxes = 0;
        [DataMember] private int scaleAxes = 0;

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

        [JsonIgnore] public readonly UnityEvent<int> OnPositionAxesChanged = new();
        [JsonIgnore] public readonly UnityEvent<int> OnRotationAxesChanged = new();
        [JsonIgnore] public readonly UnityEvent<int> OnScaleAxesChanged = new();

        [JsonConstructor]
        public TransformLockLayerPropertyData(int positionAxes, int rotationAxes, int scaleAxes)
        {
            this.positionAxes = positionAxes;
            this.rotationAxes = rotationAxes;
            this.scaleAxes = scaleAxes;
        }
    }
}