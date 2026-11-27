using System;
using System.Collections.Generic;
using System.Runtime.Serialization;
using Netherlands3D.Twin.Projects;
using Newtonsoft.Json;
using UnityEngine;
using UnityEngine.Events;

namespace Netherlands3D.Twin.Layers.Properties
{
    [Serializable]
    public class LayerPropertyData
    {
        /// <summary>
        /// Property data has a unique identifier for tracking which data belongs to this
        /// property; such as assets. 
        /// </summary>
        [DataMember] public Guid UUID = Guid.NewGuid();

        [JsonIgnore] protected bool isEditable = true;
        [JsonIgnore] public UnityEvent<bool> IsEditableChanged = new UnityEvent<bool>();

        [JsonIgnore] public bool IsEditable
        {
            get => isEditable;
            set
            {
                isEditable = value;
                IsEditableChanged.Invoke(value);
            }
        }
        
        public LayerPropertyData Clone()
        {
            var settings = ProjectDataHandler.SerializerSettings;
            var json = JsonConvert.SerializeObject(this, settings);
            var copy = (LayerPropertyData)JsonConvert.DeserializeObject(
                json, GetType(), settings);

            copy.UUID = Guid.NewGuid();
            return copy;
        }
    }
}
