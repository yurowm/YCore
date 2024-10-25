using System;
using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using System.Text.RegularExpressions;
using UnityEngine;

namespace Yurowm.Utilities {
    [Serializable]
    public class IntRange : IEnumerable<int> {
        public int min;
        public int max;

        public int Max => YMath.Max(min, max);

        public int Min => YMath.Min(min, max);

        public int Count => (max - min).Abs() + 1;
        public int Interval => (max - min).Abs();

        public IntRange() : this(0) { }
        
        public IntRange(int value) : this(value, value) { }

        public IntRange(int min, int max) {
            Set(min, max);
        }
        
        public void Set(int min, int max) {
            if (min > max)
                Set(max, min);
            else {
                this.min = min;
                this.max = max;
            }
        }
        public bool IsInRange(int value) {
            return value >= Min && value <= Max;
        }

        public int Lerp(float t) {
            return Mathf.RoundToInt(Mathf.Lerp(min, max, t));
        }

        internal IntRange GetClone() {
            return (IntRange) MemberwiseClone();
        }
    
        IEnumerator<int> IEnumerable<int>.GetEnumerator() {
            for (int value = Min; value <= Max; value++)
                yield return value;
        }

        IEnumerator IEnumerable.GetEnumerator() {
            return (this as IEnumerable<int>).GetEnumerator();
        }

        static Regex parser = new Regex(@"\((?<min>\d+)[\,\;x](?<max>\d+)\)");
        public static IntRange Parse(string raw) {
            var match = parser.Match(raw);
            if (match.Success)
                return new IntRange(int.Parse(match.Groups["min"].Value), int.Parse(match.Groups["max"].Value));
            throw new FormatException("Can't to parse \"" + raw + "\" to IntRange format. It must have next format: (int;int)");
        }

        static string format = "({0};{1})";
        public override string ToString() {
            return string.Format(format, min, max);
        }

        public static implicit operator IntRange(int number) {
            return new IntRange(number, number);
        }

        public bool Overlaps(IntRange range) {
            return IsInRange(range.min) || IsInRange(range.max) || 
                   range.IsInRange(min) || range.IsInRange(max);
        }
    }

    [Serializable]
    public class FloatRange {
        public float min;
        public float max;

        public float Max => YMath.Max(min, max);

        public float Min => YMath.Min(min, max);

        public float Interval => (max - min).Abs();
        
        public FloatRange() : this(0) { }
        
        public FloatRange(float value) : this(value, value) { }

        public FloatRange(float min, float max) {
            Set(min, max);
        }
        
        public void Set(float min, float max) {
            if (min > max)
                Set(max, min);
            else {
                this.min = min;
                this.max = max;
            }
        }

        public bool IsInRange(float value) {
            return value >= Min && value <= Max;
        }

        public float Lerp(float t) {
            return Mathf.Lerp(min, max, t);
        }

        internal FloatRange GetClone() {
            return (FloatRange) MemberwiseClone();
        }

        static Regex parser = new Regex(@"\((?<min>\d*\.?\d+)[\,\;x](?<max>\d*\.?\d+)\)");
        public static FloatRange Parse(string raw) {
            var match = parser.Match(raw);
            if (match.Success)
                return new FloatRange(float.Parse(match.Groups["min"].Value, CultureInfo.InvariantCulture), float.Parse(match.Groups["max"].Value, CultureInfo.InvariantCulture));
            throw new FormatException("Can't to parse \"" + raw + "\" to FloatRange format. It must have next format: (float;float)");
        }

        static string format = "({0};{1})";
        public override string ToString() {
            return string.Format(CultureInfo.InvariantCulture, format, min, max);
        }

        public float Clamp(float value) {
            return Mathf.Clamp(value, min, max);
        }

        public static implicit operator FloatRange(float value) {
            return new FloatRange(value, value);
        }

        public float GetTime(float value) {
            if (min == max) return 0;
            return (value - min) / (max - min);
        }

        public bool Overlaps(FloatRange range) {
            return IsInRange(range.min) || IsInRange(range.max) || 
                   range.IsInRange(min) || range.IsInRange(max);
        }
    }
}