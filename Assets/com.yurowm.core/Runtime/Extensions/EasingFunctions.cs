using System;
using UnityEngine;

namespace Yurowm.Utilities {
    public static class EasingFunctions {
        
        public enum Easing {
            Linear = 0,
            InQuad = 1,
            OutQuad = 2,
            InOutQuad = 3,
            InCubic = 4,
            OutCubic = 5,
            InOutCubic = 6,
            InQuart = 7,
            OutQuart = 8,
            InOutQuart = 9,
            InQuint = 10,
            OutQuint = 11,
            InOutQuint = 12,
            InElastic = 13,
            OutElastic = 14,
            InOutElastic = 15,
            InBack = 16,
            OutBack = 17,
            InOutBack = 18
        }

        public static float Evaluate(this Easing type, float t) {
            switch (type) {
                case Easing.Linear: return Linear(t);
                case Easing.InQuad: return InQuad(t);
                case Easing.OutQuad: return OutQuad(t);
                case Easing.InOutQuad: return InOutQuad(t);
                case Easing.InCubic: return InCubic(t);
                case Easing.OutCubic: return OutCubic(t);
                case Easing.InOutCubic: return InOutCubic(t);
                case Easing.InQuart: return InQuart(t);
                case Easing.OutQuart: return OutQuart(t);
                case Easing.InOutQuart: return InOutQuart(t);
                case Easing.InQuint: return InQuint(t);
                case Easing.OutQuint: return OutQuint(t);
                case Easing.InOutQuint: return InOutQuint(t);
                case Easing.InElastic: return InElastic(t);
                case Easing.OutElastic: return OutElastic(t);
                case Easing.InOutElastic: return InOutElastic(t);
                case Easing.InBack: return InBack(t);
                case Easing.OutBack: return OutBack(t);
                case Easing.InOutBack: return InOutBack(t);
                default: return t;
            }
        }

        public static float Ease(this float t, Easing type) {
            return Evaluate(type, t);
        }

        public static float Linear(float t) {
            return t;
        }

        public static float InQuad(float t) {
            return t * t;
        }

        public static float OutQuad(float t) {
            return t * (2 - t);
        }

        public static float InOutQuad(float t) {
            return t < .5f ? 2 * t * t : -1 + (4 - 2 * t) * t;
        }

        public static float InCubic(float t) {
            return t * t * t;
        }

        public static float OutCubic(float t) {
            return (--t) * t * t + 1;
        }

        public static float InOutCubic(float t) {
            return t < .5f ? 4 * t * t * t : (t - 1) * (2 * t - 2) * (2 * t - 2) + 1;
        }

        public static float InQuart(float t) {
            return t * t * t * t;
        }

        public static float OutQuart(float t) {
            return 1 - (--t) * t * t * t;
        }

        public static float InOutQuart(float t) {
            return t < .5f ? 8 * t * t * t * t : 1 - 8 * (--t) * t * t * t;
        }

        public static float InQuint(float t) {
            return t * t * t * t * t;
        }

        public static float OutQuint(float t) {
            return 1 + (--t) * t * t * t * t;
        }

        public static float InOutQuint(float t) {
            return t < .5f ? 16 * t * t * t * t * t : 1 + 16 * (--t) * t * t * t * t;
        }

        public static float InElastic(float t) {
            if (t <= 0 || t >= 1)
                return t.Clamp01();
            float p = 0.5f;
            return -(Mathf.Pow(2, -10 * t) * Mathf.Sin(-(t + p / 4) * (2 * Mathf.PI) / p));
        }

        public static float OutElastic(float t) {
            if (t <= 0 || t >= 1)
                return t.Clamp01();
            float p = 0.5f;
            return Mathf.Pow(2, -10 * t) * Mathf.Sin((t - p / 4) * (2 * Mathf.PI) / p) + 1;
        }

        public static float InOutElastic(float t) {
            if (t <= 0 || t >= 1)
                return t.Clamp01();
            t = Mathf.Lerp(-1, 1, t);

            float p = 0.9f;

            if (t < 0)
                return 0.5f * (Mathf.Pow(2, 10 * t) * Mathf.Sin((t + p / 4) * (2 * Mathf.PI) / p));
            else
                return Mathf.Pow(2, -10 * t) * Mathf.Sin((t - p / 4) * (2 * Mathf.PI) / p) * 0.5f + 1;
        }

        public static float InBack(float t) {
            const float c1 = 1.70158f;
            const float c3 = c1 + 1;

            return c3 * t * t * t - c1 * t * t;
        }

        public static float OutBack(float t) {
            const float c1 = 1.70158f;
            const float c3 = c1 + 1;
            
            return 1f + c3 * Mathf.Pow(t - 1, 3) + c1 * Mathf.Pow(t - 1, 2);
        }

        public static float InOutBack(float t) {
            const float c1 = 1.70158f;
            const float c2 = c1 * 1.525f;

            if (t < .5f)
                return Mathf.Pow(t * 2, 2) * ((c2 + 1) * 2 * t - c2) / 2;
            else
                return (Mathf.Pow(t * 2 - 2, 2) * ((c2 + 1) * (t * 2 - 2) + c2) + 2) / 2;
        }
    }
}