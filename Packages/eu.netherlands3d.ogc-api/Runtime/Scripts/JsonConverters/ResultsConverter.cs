using System;
using Netherlands3D.OgcApi.Pagination;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace Netherlands3D.OgcApi.JsonConverters
{
    public class ResultsConverter<T> : JsonConverter<Results<T>>
    {
        private const string numberMatchedFieldName = "numberMatched";
        private const string numberReturnedFieldName = "numberReturned";
        private const string timeStampFieldName = "timeStamp";
        private const string linksFieldName = "links";

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
            var linksToken = jo[linksFieldName];
            var links = linksToken != null
                ? linksToken.ToObject<Link[]>(serializer)
                : Array.Empty<Link>();
            
            jo.Remove(timeStampFieldName);
            jo.Remove(numberMatchedFieldName);
            jo.Remove(numberReturnedFieldName);
            jo.Remove(linksFieldName);

            var val = jo.ToObject<T>(serializer);

            return new Results<T>(val)
            {
                TimeStamp = timeStamp,
                NumberMatched = matched,
                NumberReturned = returned,
                Links = links
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
            jo[linksFieldName] = JToken.FromObject(wrapper.Links, serializer);

            jo.WriteTo(writer);
        }
    }
}