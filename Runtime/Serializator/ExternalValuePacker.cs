using System.Linq;
using UnityEngine;

namespace Yurowm.Serialization {
    public class GradientPacker : ExternalValuePacker<Gradient> {
        protected override void Serialize(Gradient value, Writer writer) {
            writer.Write("mode", value.mode);
            
            writer.Write("alphaTime", value.alphaKeys.Select(k => k.time).ToArray());
            writer.Write("alphaValue", value.alphaKeys.Select(k => k.alpha).ToArray());
            
            writer.Write("colorTime", value.colorKeys.Select(k => k.time).ToArray());
            writer.Write("colorValue", value.colorKeys.Select(k => k.color).ToArray());
        }

        protected override void Deserialize(Gradient value, Reader reader) {
            value.mode = reader.Read<GradientMode>("mode");
            
            value.SetKeys(reader.ReadCollection<float>("colorTime")
                .Zip(reader.ReadCollection<Color>("colorValue"), 
                    (t, v) => new GradientColorKey(v, t))
                .ToArray(),
                reader.ReadCollection<float>("alphaTime")
                    .Zip(reader.ReadCollection<float>("alphaValue"), 
                        (t, v) => new GradientAlphaKey(v, t))
                    .ToArray());
        }
    }
    //
    // public class AnimationCurvePacker : ExternalValuePacker<AnimationCurve> {
    //     protected override void Serialize(AnimationCurve value, Writer writer) {
    //         writer.Write("keys", value.keys);
    //     }
    //
    //     protected override void Deserialize(AnimationCurve value, Reader reader) {
    //         value.keys = reader.ReadCollection<Keyframe>("keys").ToArray();
    //     }
    // }
}