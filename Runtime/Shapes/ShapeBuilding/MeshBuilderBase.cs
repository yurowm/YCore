using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using Yurowm.Extensions;
using Yurowm.Utilities;

namespace Yurowm.Shapes {
    public class MeshBuilderBase : IShapeBuilder {
        public int currentVertCount => vertices.Count;
        
        protected List<Vertex> vertices = new List<Vertex>();
        protected List<Triangle> triangles = new List<Triangle>();

        public void AddVert(Vector2 position, Color32 color,
            Vector2 uv0, Vector2 uv1,
            Vector3 normal, Vector4 tangent) {
            
            if (position.IsNan()) {
                Debug.LogError("Vertex position can't be NaN");
                position = default;
            }
            
            vertices.Add(new Vertex {
                position = position,
                color = color,
                uv0 = uv0,
                uv1 = uv1,
                normal = normal,
                tangent = tangent
            });
        }

        public void AddVert(Vector2 position, Color32 color, Vector2 uv = default) {
            AddVert(position, color,
                uv, uv,
                Vector3.back,
                new Vector4(1.0f, 0.0f, 0.0f, -1.0f));
        }
        
        public void AddTriangle(int idx0, int idx1, int idx2, bool flip = false) {
            if (flip)
                triangles.Add(new Triangle {
                    idx0 = idx0,
                    idx1 = idx2,
                    idx2 = idx1
                });
            else
                triangles.Add(new Triangle {
                    idx0 = idx0,
                    idx1 = idx1,
                    idx2 = idx2
                });
        }

        public virtual void Clear() {
            vertices.Clear();
            triangles.Clear();
        }
        
        protected struct Vertex {
            public int index;
            
            public Vector2 position;
            public Color color;
            
            public Vector2 uv0;
            public Vector2 uv1;
            public Vector3 normal;
            public Vector4 tangent;
        }

        protected struct Triangle {
            public int idx0;
            public int idx1;
            public int idx2;
            
            public bool IsEmpty() {
                return idx0 == idx1 || idx1 == idx2 || idx0 == idx2;
            }

            public IEnumerable<EdgeID> GetEdgeIDs() {
                yield return new EdgeID(idx0, idx1, idx2);
                yield return new EdgeID(idx1, idx2, idx0);
                yield return new EdgeID(idx2, idx0, idx1);
            }
            
            public IEnumerable<int> GetIDs() {
                yield return idx0;
                yield return idx1;
                yield return idx2;
            }
        }

        #region Mesh Filters
        
        [Flags]
        public enum MeshOptimization {
            OptimizeVertices = 1 << 1,
            Antialising = 1 << 2,
        }
        
        public enum UVGenerator {
            None = 0,
            Zero = 1,
            Stretch = 2
            // FitInside = 3,
            // FitOutlise = 4
        }

        public void Optimize(MeshOptimization optimization) {
            if (optimization.HasFlag(MeshOptimization.OptimizeVertices))
                OptimizeVertices();
            if (optimization.HasFlag(MeshOptimization.Antialising))
                AddAntialising();
        }

        static BoundDetector2D boundDetector2D = new BoundDetector2D();
        
        public void GenerateUV(Func<Rect, Vector2, Vector2> getUV) {
            boundDetector2D.Clear();
            
            vertices.ForEach(v => boundDetector2D.Set(v.position));
            
            var bound = boundDetector2D.GetBound();
            
            for (int i = 0; i < vertices.Count; i++) {
                var vertex = vertices[i];
                vertex.uv0 = getUV(bound, vertex.position);
                vertices[i] = vertex;
            }
        }
        
        public void GenerateUV(UVGenerator uvGenerator) {
            if (uvGenerator == UVGenerator.None || vertices.Count == 0) return;
            
            if (uvGenerator == UVGenerator.Zero) {
                for (int i = 0; i < vertices.Count; i++) {
                    var vertex = vertices[i];
                    vertex.uv0.x = 0;
                    vertex.uv0.y = 0;
                    vertices[i] = vertex;
                }
                return;
            }
            
            boundDetector2D.Clear();
            
            vertices.ForEach(v => boundDetector2D.Set(v.position));
            
            var bound = boundDetector2D.GetBound();
            
            if (uvGenerator == UVGenerator.Stretch) {
                for (int i = 0; i < vertices.Count; i++) {
                    var vertex = vertices[i];
                    vertex.uv0 = (vertex.position - bound.min) / (bound.size);
                    vertices[i] = vertex;
                }
            }
        }
        
        void OptimizeVertices() {
            for (int i = 0; i < vertices.Count; i++) {
                var vertex = vertices[i];
                vertex.index = i;
                vertices[i] = vertex;
            }
            
            int indexer = 0;
            
            Vector2 PositionThreshold(Vector2 v, float threshold) {
                v.x = Mathf.Round(v.x * threshold) / threshold;
                v.y = Mathf.Round(v.y * threshold) / threshold;
                return v;
            }
                
            
            var verticesOptimized = vertices
                .GroupBy(v => PositionThreshold(v.position, 1000))
                .Select(g => {
                    Vertex vertex = default;
                    foreach (var v in g) {
                        vertex = vertices[v.index];
                        vertex.index = indexer;
                        vertices[v.index] = vertex;
                    }
                    indexer++;
                    return vertex;
                }).ToArray();
            
            var trianglesOptimized = triangles
                .Select(t => {
                    t.idx0 = vertices[t.idx0].index;
                    t.idx1 = vertices[t.idx1].index;
                    t.idx2 = vertices[t.idx2].index;
                    return t;
                })
                .Where(t => !t.IsEmpty())
                .ToArray();
            
            vertices.Clear();
            triangles.Clear();
            
            vertices.AddRange(verticesOptimized);
            triangles.AddRange(trianglesOptimized);
        }
        
        void AddAntialising() {
            float pointSize = GetPointSize();
            
            List<Vertex> aaVertices = new List<Vertex>();
            List<Triangle> aaTriangles = new List<Triangle>();
            
            triangles
                .SelectMany(t => t.GetEdgeIDs())
                .GroupBy(e => e)
                .Where(g => g.Count() == 1)
                .ForEach(g => {
                    var e = new Edge() {
                        pointA = vertices[g.Key.A],
                        pointB = vertices[g.Key.B]
                    };
                    e.CalculateNormal(vertices[g.Key.N].position);
                    var a = e.pointA;
                    var b =  e.pointB;
                    
                    int index = aaVertices.Count + vertices.Count;

                    aaVertices.Add(a);
                    aaVertices.Add(b);
                    
                    var offset = e.normal * pointSize;
                    
                    a.position += offset;
                    var color = a.color;
                    color.a = 0;
                    a.color = color;
                    
                    aaVertices.Add(a);
                    
                    b.position += offset;
                    color = b.color;
                    color.a = 0;
                    b.color = color;
                    
                    aaVertices.Add(b);
                    
                    aaTriangles.Add(new Triangle() {
                        idx0 = index,
                        idx2 = index + 2,
                        idx1 = index + 1
                    });
                    
                    aaTriangles.Add(new Triangle() {
                        idx0 = index + 1,
                        idx1 = index + 3,
                        idx2 = index + 2
                    });
                });

            vertices.AddRange(aaVertices);
            triangles.AddRange(aaTriangles);
        }

        public virtual float GetPointSize() {
            return 0f;
        }

        protected struct EdgeID {
            public readonly int A;
            public readonly int B;
            public readonly int N;

            public EdgeID(int a, int b, int n) {
                if (a > b) {
                    A = a;
                    B = b;
                } else {
                    A = b;
                    B = a;
                }
                N = n;
            }
            
            public override bool Equals(object obj) {
                if (obj is EdgeID other)
                    return A == other.A && B == other.B;
                return base.Equals(obj);
            }

            public override int GetHashCode() {
                unchecked {
                    return (A * 397) ^ B;
                }
            }
        }
        
        struct Edge {
            public Vertex pointA;
            public Vertex pointB;
            
            public Vector2 normal;
            
            public void CalculateNormal(Vector2 pointN) {
                normal = pointB.position - pointA.position;
                normal.Normalize();
                
                normal.x += normal.y;
                normal.y = normal.x - normal.y;
                normal.x = normal.x - normal.y;
                
                normal.y *= -1;
                
                if (Vector2.Dot(normal, pointN - pointA.position) > 0) {
                    normal *= -1;
                    var v = pointA;
                    pointA = pointB;
                    pointB = v;
                }
            }
        }

        #endregion
    }
}