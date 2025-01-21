using Newtonsoft.Json.Linq;
using System;
using UnityEngine;

namespace Yurowm.YJSONSerialization {
    public class JsonAnimationCurvePacker : JSONValuePacker<AnimationCurve> {
        protected override JToken PackValue(AnimationCurve value) {
            var keyframes = new JArray();

            foreach (var keyframe in value.keys) {
                keyframes.Add(new JObject {
                    ["time"] = keyframe.time,
                    ["value"] = keyframe.value,
                    ["inTangent"] = keyframe.inTangent,
                    ["outTangent"] = keyframe.outTangent
                });
            }

            return new JObject {
                ["keys"] = keyframes,
                ["preWrapMode"] = value.preWrapMode.ToString(),
                ["postWrapMode"] = value.postWrapMode.ToString()
            };
        }

        protected override AnimationCurve UnpackValue(JToken token, Type targetType) {
            var curve = new AnimationCurve();

            if (token is JObject obj) {
                if (obj.TryGetValue("keys", out var keyframesToken) && keyframesToken is JArray keyframesArray) {
                    var keyframes = new Keyframe[keyframesArray.Count];
                    for (int i = 0; i < keyframesArray.Count; i++) {
                        var keyToken = keyframesArray[i];
                        if (keyToken is JObject keyObj) {
                            var key = new Keyframe {
                                time = keyObj["time"]?.Value<float>() ?? 0f,
                                value = keyObj["value"]?.Value<float>() ?? 0f,
                                inTangent = keyObj["inTangent"]?.Value<float>() ?? 0f,
                                outTangent = keyObj["outTangent"]?.Value<float>() ?? 0f
                            };
                            keyframes[i] = key;
                        }
                    }

                    curve.keys = keyframes;
                }

                if (obj.TryGetValue("preWrapMode", out var preWrapMode))
                    curve.preWrapMode = Enum.TryParse(preWrapMode.Value<string>(), out WrapMode preMode)
                        ? preMode
                        : WrapMode.Default;

                if (obj.TryGetValue("postWrapMode", out var postWrapMode))
                    curve.postWrapMode = Enum.TryParse(postWrapMode.Value<string>(), out WrapMode postMode)
                        ? postMode
                        : WrapMode.Default;
            }

            return curve;
        }
    }
}