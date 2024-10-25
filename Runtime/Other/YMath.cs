using UnityEngine;
using Yurowm.Extensions;
using Yurowm.Utilities;

namespace Yurowm {
    public static class UnityMath {
        
        public static Vector2 Abs(this Vector2 vector) {
            vector.x = vector.x.Abs();
            vector.y = vector.y.Abs();
            return vector;
        }

        public static Vector2 MoveByPath(this Vector2 current, Vector2 waypoint, ref float lengthReserve) {
            var distance = (waypoint - current).FastMagnitude();
            if (distance > lengthReserve) {
                current = current + (waypoint - current).FastNormalized() * lengthReserve;
                lengthReserve = 0;
                return current;
            } else {
                lengthReserve -= distance;
                return waypoint;
            }
        }

        public static Vector2 MoveTowards(this Vector2 current, Vector2 target, float maxStep) {
            var offset = target - current;
            if (offset.MagnitudeIsGreaterThan(maxStep))
                return current + offset.WithMagnitude(maxStep);
            
            return target;
        }
    }
}