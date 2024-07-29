using System;
using System.Collections;
using System.Collections.Generic;
using Netherlands3D.Coordinates;
using Newtonsoft.Json;
using UnityEngine;
using UnityEngine.Events;

namespace Netherlands3D.Twin
{
   [Serializable]
    public class TransformLayerProperty: LayerProperty
    {
        [SerializeField, JsonProperty] private Coordinate position = new Coordinate(CoordinateSystem.RDNAP);
        // [SerializeField, JsonProperty] private double[] rdPosition = new double[3];
        [SerializeField, JsonProperty] private Vector3 eulerRotation;
        [SerializeField, JsonProperty] private Vector3 localScale;

        [JsonIgnore]
        public Coordinate Position
        {
            get => position;
            set
            {
                position = value.Convert(CoordinateSystem.RDNAP);
                // rdPosition[0] = rd.x;
                // rdPosition[1] = rd.y;
                // rdPosition[2] = rd.z;
                OnPositionChanged.Invoke(value);
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

        public override void Load()
        {
            Debug.Log("loading Position: " + position.ToUnity());

            Position = position;
            EulerRotation = eulerRotation;
            LocalScale = localScale;
        }
    }
}
