using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using Yurowm.Extensions;

namespace Yurowm.Shapes {
    public class YLine {
        
        public enum ConnectionType {Thread, Chain, Brick}
        
        List<Vector2> vertices = new();
        List<Vector3> normals = new();
        List<Vector2> uv = new();
        List<Triangle> triangles = new();
        
        #region Points
        
        List<Vector2> points = new();
        HashSet<int> breakers = new();
        
        public void AddBreaker() {
            breakers.Add(points.Count - 1);
        }
        
        public void AddPoint(Vector2 point) {
            points.Add(point);
        }
        
        public void SetPoints(IEnumerable<Vector2> points) {
            this.points.Reuse(points);
        }

        public void ChangePoint(int id, Vector2 point) {
            if (id >= 0 && id < points.Count) 
                points[id] = point;
        }

        public int PointsCount() {
            return points.Count;
        }

        public void Clear() {
            points.Clear();
            breakers.Clear();
        }
        
        public float GetLength() {
            if (points.Count < 2) return 0;
            
            var length = 0f;
            
            var previousPoint = points[0];

            for (int i = 1; i < points.Count; i++) {
                var currentPoint = points[i];   
                length += (currentPoint - previousPoint).FastMagnitude();
                previousPoint = currentPoint;
            }
            
            return length;
        }
        
        public IEnumerable<Vector2> GetPoints() {
            foreach (var point in points)
                yield return point;
        }
        
        public Vector2 GetPoint(int index) {
            if (index >= 0 && index < points.Count)
                return points[index];
            return default;
        }
        
        public IEnumerable<int> GetIndicesOf(Vector2 point) {
            var index = 0;
            foreach (var p in points) {
                if (p == point)
                    yield return index;
                index ++;
            }
        }
        
        #endregion
        
        public void FillMesh(MeshBuilderBase builder, Order order) {
            vertices.Clear();
            normals.Clear();
            uv.Clear();
            triangles.Clear();
            
            if (order.thickness <= 0) return;
            
            if (order.removeDuplicates)
                for (var i = points.Count - 1; i >= 1; i--)
                    if (points[i] == points[i - 1]) {
                        points.RemoveAt(i);
                        if (order.start >= i) order.start--;
                        if (order.end >= i) order.end--;
                    }
            
            if (points.Count < 2) return;
            
            if (order.end == 0) 
                order.end = points.Count - 1;
            
            switch (order.type) {
                case ConnectionType.Thread: BuildThread(order); break;
                case ConnectionType.Chain:
                case ConnectionType.Brick: BuildBrick(order); break;
            }
            
            for (int i = 0; i < vertices.Count; i++) {
                builder.AddVert(vertices[i], order.color, 
                    new Vector2(uv[i].x, uv[i].y), 
                    Vector2.zero, 
                    order.directionNormals ? normals[i] : Vector3.back,
                    new Vector4(1, 0, 0, -1));
            }

            foreach (Triangle triangle in triangles) 
                builder.AddTriangle(triangle.a, triangle.b, triangle.c);
            
        }
        
        void BuildThread(Order order) {
            if (order.start == 0 && order.end == points.Count - 1) {
                if (breakers.Any()) {
                    var breakers = this.breakers
                        .Where(i => i > 0 && i < points.Count)
                        .OrderBy(i => i)
                        .ToArray();
                    
                    if (breakers.Any()) {
                        for (var i = 0; i < breakers.Length; i++) {
                            var subOrder = order;
                            subOrder.start = i > 0 ? breakers[i - 1] + 1 : 0;
                            subOrder.end = breakers[i];
                            BuildThread(subOrder);
                        }
                        
                        return;
                    }
                }
            }
            
            var firstVertexIndex = vertices.Count;
            
            var pointIndex = 0;
            var length = 0f;
            var totalLength = 0f;
            for (var i = order.start + 1; i <= order.end; i++) 
                totalLength += (points[i] - points[i - 1]).FastMagnitude();
            if (order.loop)
                totalLength += (points[0] - points[^1]).FastMagnitude();

            if (points[order.start] == points[order.end]) {
                order.end --;
                order.loop = true;
            }

            if (order.smooth >= 2 && points.Count >= 3) {
                order.points = new List<Vector2>();
                var guides = new List<Vector2>();
                for (int i = 0; i < points.Count; i++) {
                    if (i == 0 || i == points.Count - 1)
                        guides.Add(Vector2.zero);
                    else {
                        Vector2 guide = (points[i - 1] - points[i]).normalized + (points[i + 1] - points[i]).normalized;
                        guide = guide.normalized;
                        guide.x += guide.y;
                        guide.y -= guide.x;
                        guide.x += guide.y;

                        if (Vector2.Angle(guide, points[i + 1] - points[i]) > 90)
                            guide *= -1;
                        guides.Add(guide);
                    }
                }
                for (int i = 0; i < points.Count - 1; i++) {
                    float guide_magnitude = Vector2.Distance(points[i], points[i + 1]) * order.smoothPower;
                    var a = points[i];
                    var b = a + guides[i] * guide_magnitude;
                    var d = points[i + 1];
                    var c = d - guides[i + 1] * guide_magnitude;
                    //Debug.DrawLine(points[i], points[i] + guideStart, Color.green, 1);
                    //Debug.DrawLine(points[i + 1], points[i + 1] + guideEnd, Color.red, 1);
                    //Debug.DrawLine(points[i], points[i + 1], Color.yellow, 1);
                    order.points.Add(points[i]);
                    
                    for (int s = 1; s < order.smooth; s++) {
                        var t = 1f * s / order.smooth;

                        order.points.Add(Vector2.Lerp(
                            Vector2.Lerp(
                                Vector2.Lerp(a, b, t),
                                Vector2.Lerp(b, c, t),
                                t),
                            Vector2.Lerp(
                                Vector2.Lerp(b, c, t),
                                Vector2.Lerp(c, d, t),
                                t),
                            t
                        ));
                    }
                }
                order.points.Add(points[^1]);

            } else
                order.points = points;

            for (int i = order.start; i <= order.end; i++) {
                var a = GetPoint(i - 1, ref order) - GetPoint(i, ref order);
                var b = GetPoint(i + 1, ref order) - GetPoint(i, ref order);

                a = a.normalized;
                b = b.normalized;

                var offset = Vector2.Lerp(a, b, 0.5f);
                if (offset == Vector2.zero)
                    offset = Quaternion.Euler(0, 0, 90) * a;
            
                offset = offset.normalized;

                if (i > order.start)
                    length += (order.points[i - 1] - order.points[i]).FastMagnitude();
                uv.Add(new Vector2(1, length / totalLength));
                uv.Add(new Vector2(0, length / totalLength));
                
                var _thickness = order.thickness / Mathf.Cos(Mathf.Deg2Rad * (90 - Vector2.Angle(a, b) / 2));
                _thickness *= order.thicknessCurve?.Evaluate(length / totalLength) ?? 1;
                
                var left = Vector3.Project(Quaternion.Euler(0, 0, 90) * a, offset)
                    .FastNormalized() 
                    * _thickness / 2;
                
                vertices.Add(new Vector3(order.points[i].x, order.points[i].y, 0) - left);
                vertices.Add(new Vector3(order.points[i].x, order.points[i].y, 0) + left);
                var vi = vertices.Count;
                
                if (order.directionNormals) {
                    var normal = new Vector3(0, 0, ((b - a).Angle(false) + 90) * Mathf.Deg2Rad);
                    normals.Add(normal);
                    normals.Add(normal);
                }

                if (vi >= 4 && pointIndex >= 1) {
                    triangles.Add(new Triangle(vi - 2, vi - 3, vi - 4));
                    triangles.Add(new Triangle(vi - 3, vi - 2, vi - 1));
                }

                pointIndex++;
            }

            if (order.loop) {
                vertices.Add(vertices[firstVertexIndex]);
                vertices.Add(vertices[firstVertexIndex + 1]);
                
                var vi = vertices.Count;

                triangles.Add(new Triangle(vi - 2, vi - 3, vi - 4));
                triangles.Add(new Triangle(vi - 3, vi - 2, vi - 1));

                length += (order.points[order.end] - order.points[order.start]).FastMagnitude();
                uv.Add(new Vector2(1, length / totalLength));
                uv.Add(new Vector2(0, length / totalLength));

                if (order.directionNormals) {
                    var normal = new Vector3(0, 0, ((order.points[order.start] - order.points[order.end]).Angle(false) + 90) * Mathf.Deg2Rad);
                    normals.Add(normal);
                    normals.Add(normal);
                }
            }

            if (order.tileY != 0)
                for (int i = 0; i < uv.Count; i++)
                    uv[i] = new Vector2(uv[i].x, uv[i].y / order.tileY);
        }

        void BuildBrick(Order order) {
            int p = 0;
            
            order.points = points;
            
            for (int i = 0; i < points.Count; i++) {
                if (order.type == ConnectionType.Chain && i == 0 && !order.loop)
                    continue;

                if (order.type == ConnectionType.Brick && i % 2 == 0)
                    continue;

                var a = GetPoint(i - 1, ref order);
                var b = GetPoint(i, ref order);
                
                if (a == b)
                    continue;
                
                var distance = order.tileY > 0 ? Vector2.Distance(a, b) * order.tileY : 1;

                Vector2 offset = Quaternion.Euler(0, 0, 90) * (b - a);
                offset = offset.normalized;
            
                Vector3 left = offset * order.thickness / 2;
                
                vertices.Add(new Vector3(a.x, a.y, 0) + left);
                vertices.Add(new Vector3(a.x, a.y, 0) - left);
                uv.Add(new Vector2(0, 0));
                uv.Add(new Vector2(1, 0));

                vertices.Add(new Vector3(b.x, b.y, 0) + left);
                vertices.Add(new Vector3(b.x, b.y, 0) - left);
                uv.Add(new Vector2(0, distance));
                uv.Add(new Vector2(1, distance));

                triangles.Add(new Triangle(p + 2, p + 1, p));
                triangles.Add(new Triangle(p + 1, p + 2, p + 3));
                p += 4;
            }
        }

        Vector2 GetPoint(int i, ref Order order) {
            var points = order.points;
            
            if (points.Count == 0)
                return Vector2.zero;
            
            if (points.Count == 1)
                return points[0];
            
            if (i < order.start) {
                if (order.loop)
                    return points[order.end];
                return Vector2.LerpUnclamped(points[order.start], points[order.start + 1], -1);
            }

            if (i > order.end) {
                if (order.loop)
                    return points[order.start];
                return Vector2.LerpUnclamped(points[order.end], points[order.end - 1], -1);
            }
            
            return points[i];
        }
        
        public struct Order {
            public ConnectionType type;
            public Color32 color;
            public bool directionNormals;
            public bool removeDuplicates;
            
            public bool loop;
            
            public List<Vector2> points;
            
            public float thickness;
            public AnimationCurve thicknessCurve;
            
            public float tileY;
            
            public float smooth;
            public float smoothPower;
            
            public int start;
            public int end;
        }
        
        struct Triangle {
            public int a;
            public int b;
            public int c;

            public Triangle(int a, int b, int c) {
                this.a = a;
                this.b = b;
                this.c = c;
            }
        }

        public int GetHash() {
            var result = 0;

            if (!points.IsEmpty())
                foreach (var point in points) 
                    result = HashCode.Combine(result, point);
            
            return result;
        }
    }
    
    public interface IYLineBehaviuor {
        void SetDirty();
        YLine GetLine();
    }
}