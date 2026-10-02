using System.Collections.Generic;
using UnityEngine;

namespace Netherlands3D.Timeline
{ 
    public class TimestampValueRangeInterpreter : ITimestampValueInterpreter
    {
        private TimestampCollection collection;
        
        [SerializeField] private Color minColor = Color.red; //todo: this is not a monobehaviour and therefore not serialized
        [SerializeField] private Color maxColor = Color.green;//todo: this is not a monobehaviour and therefore not serialized

        public TimestampValueRangeInterpreter(TimestampCollection collection)
        {
            this.collection = collection;
        }

        public void ProcessNewCollection(TimestampCollection newTimestampCollection)
        {
            throw new System.NotImplementedException();
        }

        public Color? GetColorForTimestamp(Timestamp timestamp)
        {
            if(!timestamp.ValueAsFloat.HasValue)
                return null;
            
            var t = Mathf.InverseLerp(collection.MinFloatValue, collection.MaxFloatValue, timestamp.ValueAsFloat.Value);
            return Color.Lerp(minColor, maxColor, t);
        }
    }
}
