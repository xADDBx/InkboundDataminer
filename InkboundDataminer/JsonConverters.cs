using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;

namespace InkboundDataminer {
    public class Vector3Converter : JsonConverter {
        public override bool CanConvert(Type objectType) {
            return objectType == typeof(Vector3);
        }

        public override void WriteJson(JsonWriter writer, object value, JsonSerializer serializer) {
            if (value is Vector3 vector3) {
                var jObject = new JObject();
                jObject.Add("x", new JValue(vector3.x));
                jObject.Add("y", new JValue(vector3.y));
                jObject.Add("z", new JValue(vector3.z));
                jObject.WriteTo(writer);
            }
        }

        public override object ReadJson(JsonReader reader, Type objectType, object existingValue, JsonSerializer serializer) {
            if (reader.TokenType == JsonToken.StartObject) {
                var jObject = JObject.Load(reader, null);
                var x = (float)jObject["x"];
                var y = (float)jObject["y"];
                var z = (float)jObject["z"];
                return new Vector3(x, y, z);
            }

            throw new JsonSerializationException("Invalid Vector3 JSON");
        }
    }
}
