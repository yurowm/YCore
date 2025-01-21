using System;
using System.Globalization;
using Newtonsoft.Json.Linq;
using UnityEngine;
using Yurowm.Extensions;
using Yurowm.Utilities;
using Yurowm.YJSONSerialization;

namespace Yurowm.Serialization {
    public class IntRangePacker : ValuePacker<IntRange> {
        public override void PackValue(IntRange value, Writer writer) {
            writer.AddScreened(value);
        }

        public override IntRange UnpackValue(Reader.Entry entry, Type targetType) {
            try {
                return IntRange.Parse(entry.value);
            } catch (Exception e) {
                Debug.LogException(e);
                return 0;
            }
        }
    }
    
    public class JsonIntRangePacker : JSONValuePacker<IntRange> {
        protected override JToken PackValue(IntRange value) =>
            new JObject {
                ["min"] = value.Min, 
                ["max"] = value.Max
            };
        
        protected override IntRange UnpackValue(JToken token, Type targetType) {
            var result = new IntRange();
            if (token is JObject obj) {
                if (obj.TryGetValue("min", out var min)) 
                    result.min = min.Value<int>();
                if (obj.TryGetValue("max", out var max))
                    result.max = max.Value<int>();
            }
            return result;
        }
    }
    
    public class FloatRangePacker : ValuePacker<FloatRange> {
        public override void PackValue(FloatRange value, Writer writer) {
            writer.AddScreened(value);
        }

        public override FloatRange UnpackValue(Reader.Entry entry, Type targetType) {
            try {
                return FloatRange.Parse(entry.value);
            } catch (Exception e) {
                Debug.LogException(e);
                return 0;
            }
        }
    }
    
    public class JsonFloatRangePacker : JSONValuePacker<FloatRange> {
        protected override JToken PackValue(FloatRange value) =>
            new JObject {
                ["min"] = value.Min, 
                ["max"] = value.Max
            };
        
        protected override FloatRange UnpackValue(JToken token, Type targetType) {
            var result = new FloatRange();
            if (token is JObject obj) {
                if (obj.TryGetValue("min", out var min)) 
                    result.min = min.Value<float>();
                if (obj.TryGetValue("max", out var max))
                    result.max = max.Value<float>();
            }
            return result;
        }
    }
    
    public class ColorRangePacker : ValuePacker<ColorRange> {
        
        int ToInt(float value) {
            return Mathf.RoundToInt(value * 255);
        }
        
        float ToFloat(int value) {
            return 1f * value / 255;
        }
        
        const char separator = '.';
        public override void PackValue(ColorRange value, Writer writer) {
            writer.AddScreenedFew(
                ToInt(value.start.r), 
                separator, 
                ToInt(value.start.g), 
                separator,
                ToInt(value.start.b), 
                separator, 
                ToInt(value.start.a),
                
                separator,
                
                ToInt(value.end.r), 
                separator, 
                ToInt(value.end.g), 
                separator,
                ToInt(value.end.b), 
                separator, 
                ToInt(value.end.a));
        }

        public override ColorRange UnpackValue(Reader.Entry entry, Type targetType) {
            ColorRange range = new ColorRange();
            var values = entry.value.Split(separator);
            if (values.Length == 8) {
                range.start.r = ToFloat(int.Parse(values[0]));
                range.start.g = ToFloat(int.Parse(values[1]));
                range.start.b = ToFloat(int.Parse(values[2]));
                range.start.a = ToFloat(int.Parse(values[3]));
                range.end.r = ToFloat(int.Parse(values[4]));
                range.end.g = ToFloat(int.Parse(values[5]));
                range.end.b = ToFloat(int.Parse(values[6]));
                range.end.a = ToFloat(int.Parse(values[7]));
            }
            return range;
        }
    }

    public class Vector2Packer : ValuePacker<Vector2> {
        const char separator = 'x';

        public override void PackValue(Vector2 value, Writer writer) {
            writer.AddScreenedFew(
                value.x.ToString(CultureInfo.InvariantCulture),
                separator,
                value.y.ToString(CultureInfo.InvariantCulture));
        }

        public override Vector2 UnpackValue(Reader.Entry entry, Type targetType) {
            int x = entry.value.LastIndexOf(separator);
            return new Vector2(
                float.Parse(entry.value.Substring(0, x), CultureInfo.InvariantCulture),
                float.Parse(entry.value.Substring(x + 1), CultureInfo.InvariantCulture));
        }
    }

    public class Vector3Packer : ValuePacker<Vector3> {
        const char separator = 'x';

        public override void PackValue(Vector3 value, Writer writer) {
            writer.AddScreenedFew(
                value.x.ToString(CultureInfo.InvariantCulture),
                separator,
                value.y.ToString(CultureInfo.InvariantCulture),
                separator,
                value.z.ToString(CultureInfo.InvariantCulture));
        }

        public override Vector3 UnpackValue(Reader.Entry entry, Type targetType) {
            var values = entry.value.Split(separator);
            if (values.Length == 3)
                return new Vector3(
                    float.Parse(values[0], CultureInfo.InvariantCulture),
                    float.Parse(values[1], CultureInfo.InvariantCulture),
                    float.Parse(values[2], CultureInfo.InvariantCulture));
            return default;
        }
    }

    public class QuaternionPacker : ValuePacker<Quaternion> {
        const char separator = 'x';

        public override void PackValue(Quaternion value, Writer writer) {
            writer.AddScreenedFew(
                value.x.ToString(CultureInfo.InvariantCulture),
                separator,
                value.y.ToString(CultureInfo.InvariantCulture),
                separator,
                value.z.ToString(CultureInfo.InvariantCulture),
                separator,
                value.w.ToString(CultureInfo.InvariantCulture));
        }

        public override Quaternion UnpackValue(Reader.Entry entry, Type targetType) {
            var values = entry.value.Split(separator);
            if (values.Length == 4)
                return new Quaternion(
                    float.Parse(values[0], CultureInfo.InvariantCulture),
                    float.Parse(values[1], CultureInfo.InvariantCulture),
                    float.Parse(values[2], CultureInfo.InvariantCulture),
                    float.Parse(values[3], CultureInfo.InvariantCulture));
            return default;
        }
    }
    
    // public class KeyframePacker : ValuePacker<Keyframe> {
    //     const char separator = 'x';
    //     
    //     public override void PackValue(Keyframe value, Writer writer) {
    //         var mode = (int) value.weightedMode;
    //         
    //         switch (value.weightedMode) {
    //             case WeightedMode.None:
    //                 writer.AddScreenedFew(
    //                     mode, separator,
    //                     value.time.ToString(CultureInfo.InvariantCulture), separator,
    //                     value.value.ToString(CultureInfo.InvariantCulture));
    //                 break;
    //             
    //             case WeightedMode.In:
    //                 writer.AddScreenedFew(
    //                     mode, separator,
    //                     value.time.ToString(CultureInfo.InvariantCulture), separator,
    //                     value.value.ToString(CultureInfo.InvariantCulture), separator,
    //                     value.inTangent.ToString(CultureInfo.InvariantCulture), separator,
    //                     value.inWeight.ToString(CultureInfo.InvariantCulture));
    //                 break;
    //             
    //             case WeightedMode.Out:
    //                 writer.AddScreenedFew(
    //                     mode, separator,
    //                     value.time.ToString(CultureInfo.InvariantCulture), separator,
    //                     value.value.ToString(CultureInfo.InvariantCulture), separator,
    //                     value.outTangent.ToString(CultureInfo.InvariantCulture), separator,
    //                     value.outWeight.ToString(CultureInfo.InvariantCulture));
    //                 break;
    //             
    //             case WeightedMode.Both:
    //                 writer.AddScreenedFew(
    //                     mode, separator,
    //                     value.time.ToString(CultureInfo.InvariantCulture), separator,
    //                     value.value.ToString(CultureInfo.InvariantCulture), separator,
    //                     value.inTangent.ToString(CultureInfo.InvariantCulture), separator,
    //                     value.inWeight.ToString(CultureInfo.InvariantCulture), separator,
    //                     value.outTangent.ToString(CultureInfo.InvariantCulture), separator,
    //                     value.outWeight.ToString(CultureInfo.InvariantCulture));
    //                 break;
    //         }
    //     }
    //
    //     public override Keyframe UnpackValue(Reader.Entry entry, Type targetType) {
    //         
    //         var values = entry.value.Split(separator);
    //         
    //         if (values.Length < 3)
    //             return default;
    //         
    //         if (int.TryParse(values[0], out int modeInt)) {
    //             var result = new Keyframe();
    //
    //             result.weightedMode = (WeightedMode) modeInt;
    //             result.time = float.Parse(values[1], CultureInfo.InvariantCulture);
    //             result.value = float.Parse(values[2], CultureInfo.InvariantCulture);
    //
    //             switch (result.weightedMode) {
    //                 case WeightedMode.None: break;
    //                 case WeightedMode.In: {
    //                     if (values.Length >= 5) {
    //                         result.inTangent = float.Parse(values[3], CultureInfo.InvariantCulture);
    //                         result.inWeight = float.Parse(values[4], CultureInfo.InvariantCulture);
    //                     }
    //                     break;
    //                 }
    //                 case WeightedMode.Out: {
    //                     if (values.Length >= 5) {
    //                         result.outTangent = float.Parse(values[3], CultureInfo.InvariantCulture);
    //                         result.outWeight = float.Parse(values[4], CultureInfo.InvariantCulture);
    //                     }
    //                     break;
    //                 }
    //                 case WeightedMode.Both: {
    //                     if (values.Length >= 7) {
    //                         result.inTangent = float.Parse(values[3], CultureInfo.InvariantCulture);
    //                         result.inWeight = float.Parse(values[4], CultureInfo.InvariantCulture);
    //                         result.outTangent = float.Parse(values[5], CultureInfo.InvariantCulture);
    //                         result.outWeight = float.Parse(values[6], CultureInfo.InvariantCulture);
    //                     }
    //                     break;
    //                 }
    //             }
    //                 
    //             return result;
    //         }
    //             
    //         return default;
    //     }
    // }

    public class Int2Packer : ValuePacker<int2> {
        const char separator = 'x';
        const string nullValue = "null";

        public override void PackValue(int2 value, Writer writer) {
            if (value == int2.Null)
                writer.AddScreened(nullValue);
            else
                writer.AddScreenedFew(value.X, separator, value.Y);
        }

        public override int2 UnpackValue(Reader.Entry entry, Type targetType) {
            if (entry.value == nullValue)
                return int2.Null;
            int x = entry.value.LastIndexOf(separator);
            return new int2(
                int.Parse(entry.value.Substring(0, x)),
                int.Parse(entry.value.Substring(x + 1)));
        }
    }

    public class UnityColorPacker : ValuePacker<Color> {
        int ToInt(float value) {
            return Mathf.RoundToInt(value * 255);
        }

        float ToFloat(int value) {
            return 1f * value / 255;
        }

        const char separator = '.';
        public override void PackValue(Color value, Writer writer) {
            writer.AddScreenedFew(
                ToInt(value.r), 
                separator, 
                ToInt(value.g), 
                separator,
                ToInt(value.b), 
                separator, 
                ToInt(value.a));
        }

        public override Color UnpackValue(Reader.Entry entry, Type targetType) {
            Color color = new Color();
            var values = entry.value.Split(separator);
            if (values.Length == 4) {
                color.r = ToFloat(int.Parse(values[0]));
                color.g = ToFloat(int.Parse(values[1]));
                color.b = ToFloat(int.Parse(values[2]));
                color.a = ToFloat(int.Parse(values[3]));
            }
            return color;
        }
    }
}