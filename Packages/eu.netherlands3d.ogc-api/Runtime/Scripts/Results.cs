using System;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace Netherlands3D.OgcApi
{
    public class Results<T>
    {
        [JsonProperty("timeStamp", NullValueHandling = NullValueHandling.Ignore)]
        public DateTime? TimeStamp { get; set; } = null;

        [JsonProperty("numberMatched")] public long NumberMatched { get; set; } = 0;

        [JsonProperty("numberReturned")] public long NumberReturned { get; set; } = 0;

        [JsonIgnore] public T Value { get; set; }

        public Results()
        {
        }

        public Results(T value)
        {
            Value = value;
        }
    }

    public class ResultsConverter<T> : JsonConverter<Results<T>>
    {
        private readonly string numberMatchedFieldName;
        private readonly string numberReturnedFieldName;
        private readonly string timeStampFieldName;

        public ResultsConverter(
            string numberMatchedFieldName = "numberReturned",
            string numberReturnedFieldName = "numberMatched",
            string timeStampFieldName = "timeStamp"
        )
        {
            this.numberMatchedFieldName = numberMatchedFieldName;
            this.numberReturnedFieldName = numberReturnedFieldName;
            this.timeStampFieldName = timeStampFieldName;
        }

        public override Results<T> ReadJson(JsonReader reader,
            Type objectType,
            Results<T> existingValue,
            bool hasExistingValue,
            JsonSerializer serializer)
        {
            var jo = JObject.Load(reader);

            var timeStamp = jo.Value<DateTime?>(timeStampFieldName);
            var matched = jo.Value<long?>(numberMatchedFieldName) ?? 0;
            var returned = jo.Value<long?>(numberReturnedFieldName) ?? 0;
            jo.Remove(timeStampFieldName);
            jo.Remove(numberMatchedFieldName);
            jo.Remove(numberReturnedFieldName);

            var val = jo.ToObject<T>(serializer);

            return new Results<T>
            {
                TimeStamp = timeStamp,
                NumberMatched = matched,
                NumberReturned = returned,
                Value = val
            };
        }

        public override void WriteJson(JsonWriter writer,
            Results<T> wrapper,
            JsonSerializer serializer)
        {
            var jo = JObject.FromObject(wrapper.Value, serializer);

            jo[timeStampFieldName] = wrapper.TimeStamp;
            jo[numberMatchedFieldName] = wrapper.NumberMatched;
            jo[numberReturnedFieldName] = wrapper.NumberReturned;

            jo.WriteTo(writer);
        }
    }
}