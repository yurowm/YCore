using System;
using Newtonsoft.Json.Linq;
using UnityEngine;
using Yurowm.YJSONSerialization;

namespace Yurowm.YJSONSerialization {
    public class Vector2Packer: JSONValuePacker<Vector2> {
        protected override JToken PackValue(Vector2 value) =>
            new JObject {
                ["X"] = value.x, 
                ["Y"] = value.y
            };
        
        protected override Vector2 UnpackValue(JToken token, Type targetType) {
            var result = new Vector2();
            if (token is JObject obj) {
                if (obj.TryGetValue("X", out var x))
                    if (float.TryParse(x.Value<string>(), out var X))
                        result.x = X;
                if (obj.TryGetValue("Y", out var y))
                    if (float.TryParse(y.Value<string>(), out var Y))
                        result.y = Y;
            }
            return result;
        }
    }
    
    public class Vector3Packer: JSONValuePacker<Vector3> {
        protected override JToken PackValue(Vector3 value) =>
            new JObject {
                ["X"] = value.x, 
                ["Y"] = value.y,
                ["Z"] = value.z
            };
        
        protected override Vector3 UnpackValue(JToken token, Type targetType) {
            var result = new Vector3();
            if (token is JObject obj) {
                if (obj.TryGetValue("X", out var x))
                    if (float.TryParse(x.Value<string>(), out var X))
                        result.x = X;
                if (obj.TryGetValue("Y", out var y))
                    if (float.TryParse(y.Value<string>(), out var Y))
                        result.y = Y;
                if (obj.TryGetValue("Z", out var z))
                    if (float.TryParse(z.Value<string>(), out var Z))
                        result.z = Z;
            }
            return result;
        }
    }
}