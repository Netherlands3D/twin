using System;
using System.Collections;
using System.Collections.Generic;
using Netherlands3D.Coordinates;
using Newtonsoft.Json;
using UnityEngine;
using UnityEngine.Events;

namespace Netherlands3D.Twin.Layers.Properties
{
    [Serializable]
    public class TransformLayerPropertyData : LayerPropertyData
    {
        [SerializeField, JsonProperty] private Coordinate position = new Coordinate(CoordinateSystem.RDNAP);
        [SerializeField, JsonProperty] private Vector3 eulerRotation;
        [SerializeField, JsonProperty] private Vector3 localScale;

        [JsonIgnore]
        public Coordinate Position
        {
            get => position;
            set
            {
                position = value.Convert(CoordinateSystem.RDNAP);
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
            Debug.Log("constructing property. input pos crs: " + position.CoordinateSystem);
            Debug.Log("constructing property. input rot: " + eulerRotation);
            Debug.Log("pos count: " + position.Points.Length);
            this.position = position.Convert(CoordinateSystem.RDNAP);
            this.eulerRotation = eulerRotation;
            this.localScale = localScale;
        }
    }
}