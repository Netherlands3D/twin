using System;
using System.Collections;
using System.Collections.Generic;
using System.Runtime.Serialization;
using Netherlands3D.Coordinates;
using Newtonsoft.Json;
using UnityEngine;
using UnityEngine.Events;

namespace Netherlands3D.Twin.Layers.Properties
{
    [DataContract(Namespace = "https://netherlands3d.eu/schemas/projects/layers/properties", Name = "Transform")]
    public class TransformLayerPropertyData : LayerPropertyData
    {
        [DataMember] private Coordinate position;
        [DataMember] private Vector3 eulerRotation;
        [DataMember] private Vector3 localScale;

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
        public Vector3 LocalScale
        {
            get => localScale;
            set
            {
                localScale = value;
                OnScaleChanged.Invoke(value);
            }
        }

        [JsonIgnore] public readonly UnityEvent<Coordinate> OnPositionChanged = new();
        [JsonIgnore] public readonly UnityEvent<Vector3> OnRotationChanged = new();
        [JsonIgnore] public readonly UnityEvent<Vector3> OnScaleChanged = new();
        
        [JsonConstructor]
        public TransformLayerPropertyData(Coordinate position, Vector3 eulerRotation, Vector3 localScale)
        {
            this.position = position.Convert(CoordinateSystems.connectedCoordinateSystem);
            this.eulerRotation = eulerRotation;
            this.localScale = localScale;
        }

    }
}