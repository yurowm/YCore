using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using Yurowm.Extensions;
using Yurowm.Utilities;

namespace Utilities.Helpers {
    public static class PointSpaceGenerator {
        public static IEnumerable<Vector2> GeneratePoints(ICollection<Vector2> existed, int count, int relax, int inheriting, FloatRange distanceRange, YRandom random = null, string key = null) {
            if (random == null) random = YRandom.main;
            var starPositionRandom = random.NewRandom(key);

            List<Point> points = new List<Point>();
            
            if (existed == null || existed.Count == 0)
                points.Add(new Point() {
                    position = Vector2.zero,
                });
            else
                points.AddRange(existed.Take(count).Select(v => new Point() {
                    position = v
                }));
            
            if (points.Count < count) {
                Point current;
                Point parent;
                int relaxing = -1;
                int minChild = 0;
                Func<Point, int> minChilds = p => p.childs;
                Func<Point, bool> findInheritors = p => p.childs - minChild <= inheriting;
                float radiusAddiction;
                while (points.Count < count) {
                    current = new Point();
                    if (relaxing <= 0) {
                        if (relaxing == 0)
                            points.ForEach(p => p.childs = 1);
                        relaxing = count / (relax + 1);
                    }
                    minChild = points.Min(minChilds);
                    while (true) {
                        parent = points.Where(findInheritors).GetRandom(starPositionRandom);
                        current.position = parent.position
                            + new Vector2(0, starPositionRandom.Range(distanceRange))
                            .Rotate(starPositionRandom.Range(0f, 360f));
                        if (!points.Any(p => p != current && (current.position - p.position).MagnitudeIsLessThan(distanceRange.min))) {
                            points.Add(current);
                            parent.childs++;
                            if (relax > 0)
                                relaxing--;
                            break;
                        }
                    }
                }
            }

            return points.Select(p => p.position);
        }

        public static IEnumerable<Vector2> PutIntoRect(ICollection<Vector2> points, Rect rect) {
            if (points == null || points.Count == 0)
                yield break;

            var center = rect.center;

            FloatRange xRange = new FloatRange(points.Min(p => p.x), points.Max(p => p.x));
            FloatRange yRange = new FloatRange(points.Min(p => p.y), points.Max(p => p.y));
            
            Vector2 size = new Vector2(Mathf.Min(xRange.Interval, rect.width), Mathf.Min(yRange.Interval, rect.height));

            foreach (Vector2 p in points)
                yield return new Vector2(
                                 size.x * (xRange.GetTime(p.x) - 0.5f),
                                 size.y * (yRange.GetTime(p.y) - 0.5f))
                             + center;
        }
             
        public static IEnumerable<Vector2> AlignToVector(ICollection<Vector2> points, Vector2 vector) {
            if (points == null || points.Count == 0)
                yield break;
            
            if (vector.IsEmpty() || points.Count == 1)
                foreach (Vector2 point in points)
                    yield return point;
            
            Vector2 center = new Vector2(
                points.Sum(p => p.x),
                points.Sum(p => p.y)) / points.Count;
                    
            float currentAngle = points.GetMax(p => (p - center).sqrMagnitude).Angle();
            
            float deltaAngle = vector.Angle() - currentAngle;
            
            foreach (Vector2 point in points)
                yield return center + (point - center).Rotate(deltaAngle);
        }
        
        class Point {
            public Vector2 position;
            public int childs;
        }
    }
}