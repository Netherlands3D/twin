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
        private Coordinate position = new Coordinate(CoordinateSystem.RDNAP);
        private Vector3 eulerRotation;
        private Vector3 localScale;

        [JsonProperty]
        public Coordinate Position
        {
            get => position;
            set
            {
                position = value.Convert(CoordinateSystem.RDNAP);
                OnPositionChanged.Invoke(position);
            }
        }
        
        [JsonProperty]
        public Vector3 EulerRotation
        {
            get => eulerRotation;
            set
            {
                eulerRotation = value;
                OnRotationChanged.Invoke(value);
            }
        }

        [JsonProperty]
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

        public TransformLayerProperty()
        {
        }

        [JsonConstructor]
        public TransformLayerProperty(Coordinate position, Vector3 eulerRotation, Vector3 localScale)
        {
            Debug.Log("constructing layer property fron json");
            Debug.Log(position.ToUnity());

            this.position = position;
            this.eulerRotation = eulerRotation;
            this.localScale = localScale;
        }
    }
}
