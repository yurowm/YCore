using System;
using UnityEngine;
using System.Collections.Generic;
using System.Linq;
using UnityEngine.Rendering;
using UnityEngine.U2D;
using Yurowm.Extensions;
using Yurowm.Utilities;

namespace Yurowm.Shapes {
    using Border = MeshData.Border;
    
    public static class MeshUtils {
        
        public enum Scaling {
            AsIs,
            Stretch,
            FitInside,
            FitOutside,
            Slice
        }
        
        [Flags]
        public enum TransformMode {
            FlipHorizontal = 1 << 1,
            FlipVertical = 1 << 2,
            RotateCW = 1 << 3,
            RotateCCW = 1 << 4,
            Rotate180 =  1 << 5
        }

        public static void TransformVertices(float scale, Vector2[] vertices) {
            if (scale != 1)
                for (int i = 0; i < vertices.Length; i++)
                    vertices[i] *= scale;
        }
        
        public static void BuildMesh(this MeshData meshData, MeshAsset.Order order) { 
            int currentCount = order.builder.currentVertCount;

            if (order.vertices == null || order.vertices.Length != meshData.vertices.Length)
                order.vertices = meshData.vertices;
            
            if (meshData.colors == null || meshData.colors.Length != meshData.vertices.Length)
                meshData.colors = meshData.vertices.Select(_ => Color.white).ToArray();
            
            for (int i = 0; i < order.vertices.Length; i++)
                order.builder.AddVert(order.vertices[i], order.color * meshData.colors[i],
                    meshData.uv0[i], meshData.uv1[i], 
                    Vector3.back, new Vector4(1, 0, 0, -1));
            
            for (int i = 0; i < meshData.triangles.Length; i += 3)
                order.builder.AddTriangle(
                    currentCount + meshData.triangles[i],
                    currentCount + meshData.triangles[i + 1],
                    currentCount + meshData.triangles[i + 2],
                    order.flip);

            // if (order.options.HasFlag(Order.Options.Antialising))
            //     BuildMeshAntialiasing(order);
        }
        
        public static bool TransformVertices(RectTransform rectTransform, 
            Scaling scalingMode, RectOffsetFloat borders,
            float scale, Vector2[] vertices) {
            
            TransformVertices(scale, vertices);
            
            if (scalingMode == Scaling.AsIs) return true;

            float minX = float.MaxValue;
            float maxX = float.MinValue;
            float minY = float.MaxValue;
            float maxY = float.MinValue;

            foreach (var vertex in vertices) {
                minX = Mathf.Min(minX, vertex.x);
                maxX = Mathf.Max(maxX, vertex.x);
                
                minY = Mathf.Min(minY, vertex.y);
                maxY = Mathf.Max(maxY, vertex.y);
            }
            
            if (minX == maxX || minY == maxY) return false;
            
            Rect rect = rectTransform.rect;
            
            switch (scalingMode) {
                case Scaling.Stretch: {
                    if (EmptyRect(rectTransform)) return false;
                    
                    for (int i = 0; i < vertices.Length; i++) {
                        var vertex = vertices[i];
                        vertices[i] = new Vector2(
                            rect.x + rect.width * (vertex.x - minX) / (maxX - minX),
                            rect.y + rect.height * (vertex.y - minY) / (maxY - minY));
                    } break;
                }
                case Scaling.FitInside: 
                case Scaling.FitOutside: {
                    if (scalingMode == Scaling.FitInside && EmptyRect(rectTransform)) 
                        return false;
                    
                    var pivot = rectTransform.pivot;
                    
                    var meshAspectRatio = (maxX - minX) / (maxY - minY);
                    var rectAspectRatio = rect.width / rect.height;
                    
                    Vector2 size = rect.size;
                    
                    if ((scalingMode == Scaling.FitInside) == (rectAspectRatio > meshAspectRatio))
                        size.x = size.y * meshAspectRatio;
                    else
                        size.y = size.x / meshAspectRatio;
                    
                    rect = new Rect(- size * pivot, size);
                    
                    goto case Scaling.Stretch;
                }
                case Scaling.Slice: {
                    if (EmptyRect(rectTransform)) return false;
                    
                    float bordersScale = Mathf.Min(1,
                        rect.width / borders.Horizontal,
                        rect.height / borders.Vertical);
                    
                    float borderL = borders.Left;
                    float borderR = borders.Right;
                    float borderB = borders.Bottom;
                    float borderT = borders.Top;
                    float borderV = borderB + borderT;
                    float borderH = borderL + borderR;
                    
                    for (int i = 0; i < vertices.Length; i++) {
                        var vertex = vertices[i];
                        
                        if (vertex.x <= minX + borderL)
                            vertex.x = rect.x + (vertex.x - minX) * bordersScale;
                        else if (vertex.x >= maxX - borderR)
                            vertex.x = rect.xMax - (maxX - vertex.x) * bordersScale;
                        else
                            vertex.x = rect.x + Mathf.Lerp(
                                           borderL * bordersScale,
                                           rect.width - borderR * bordersScale + 1E-3f,
                                           (vertex.x - minX - borderL) 
                                           / (maxX - minX - borderH));
                        
                        if (vertex.y <= minY + borderB)
                            vertex.y = rect.y + (vertex.y - minY) * bordersScale;
                        else if (vertex.y >= maxY - borderT)
                            vertex.y = rect.yMax - (maxY - vertex.y) * bordersScale;
                        else
                            vertex.y = rect.y + Mathf.Lerp(
                                           borderB * bordersScale,
                                           rect.height - borderT * bordersScale + 1E-3f,
                                           (vertex.y - minY - borderB) 
                                           / (maxY - minY - borderV));

                        vertices[i] = vertex;
                    }
                    break;
                }
            }
            
            return true;
        }

        static bool EmptyRect(RectTransform rectTransform) {
            var rect = rectTransform.rect;
            return rect.width <= 0 || rect.height <= 0;
        }

        public static Vector2 GetTransformVertex(TransformMode mode, Vector2 vertex) {
            if (mode.HasFlag(TransformMode.FlipHorizontal)) vertex.x *= -1;
                
            if (mode.HasFlag(TransformMode.FlipVertical)) vertex.y *= -1;
                
            if (mode.HasFlag(TransformMode.RotateCW)) {
                var x = vertex.x;
                vertex.x = vertex.y;
                vertex.y = -x;
            }
            
            if (mode.HasFlag(TransformMode.RotateCCW)) {
                var x = vertex.x;
                vertex.x = -vertex.y;
                vertex.y = x;
            }
            
            if (mode.HasFlag(TransformMode.Rotate180)) vertex *= -1;

            return vertex;
        }
        
        public static Vector2 CulculateBorderDirection(IList<Vector2> positions, MeshData.Border border, int vertexIndex) {
            var prev = positions[border.points[vertexIndex == 0 ? border.Length - 1 : vertexIndex - 1]];
            var current = positions[border.points[vertexIndex]];
            var next = positions[border.points[vertexIndex == border.Length - 1 ? 0 : vertexIndex + 1]];
            
            var nP = (prev - current).FastNormalized().Perpendicular();
            var nN = (current - next).FastNormalized().Perpendicular();
            
            return BlendDirection(nP, nN);
        }
        
        public static Vector2 BlendDirection(Vector2 a, Vector2 b) {
            Vector2 target = (a + b).FastNormalized();
            return target / Vector2.Dot(a, target);
        }
        
        public static MeshData GenerateMeshData(Mesh mesh) {
            var result = new MeshData();
            
            ExtructMeshData(result, mesh);
            FixNormals(result);
            BuildBorders(result);
            
            return result;
        }
        
        public static void FixNormals(this MeshData meshAsset) {
            var triangles = meshAsset.triangles;
            var vertices = meshAsset.vertices;
            
            for (int i = 0; i < triangles.Length; i += 3) {
                var vertA = vertices[triangles[i]];
                var vertB = vertices[triangles[i + 1]];
                var vertC = vertices[triangles[i + 2]];
                
                if ((vertB - vertA).Perpendicular().Dot(vertC - vertB) < 0)
                    (triangles[i], triangles[i + 1]) = (triangles[i + 1], triangles[i]);
            }
        }

        static void BuildBorders(this MeshData meshData, ICollection<Vertex> vertices, ICollection<Triangle> triangles) {
               List<Edge> edges = triangles
                .SelectMany(t => t.GetEdgeIDs())
                .GroupBy(e => e)
                .Where(g => g.Count() == 1)
                .Select(g => {
                    var e = new Edge() {
                        pointA = vertices.ElementAt(g.Key.A),
                        pointB = vertices.ElementAt(g.Key.B)
                    };
                    e.CalculateNormal(vertices.ElementAt(g.Key.N).position);
                    return e;
                }).ToList();
            
            var borders = new List<Border>();
            var borderEdges = new List<GradientPoint>();
            
            while (edges.Count > 0) {
                Edge lastEdge = edges.Grab();
                
                borderEdges.Clear();
                
                GradientPoint startPoint = new GradientPoint {
                    index = lastEdge.pointB.index,
                    direction = lastEdge.normal
                };
                
                GradientPoint endPoint = new GradientPoint {
                    index = lastEdge.pointA.index,
                    direction = lastEdge.normal
                };
                
                borderEdges.Add(endPoint);
                
                while (true) {
                    lastEdge = edges.Grab(e => e.ContainsIndex(endPoint.index));
                    if (!lastEdge.ContainsIndex(endPoint.index))
                        break;
                    endPoint = new GradientPoint {
                        index = lastEdge.AnotherIndex(endPoint.index),
                        direction = lastEdge.normal
                    };
                    
                    borderEdges.Add(endPoint);
                    if (lastEdge.ContainsIndex(startPoint.index))
                        break;
                }
                
                for (int i = 0; i < borderEdges.Count; i++) {
                    var c = borderEdges[i];
                    var n = i == borderEdges.Count - 1 ? startPoint.direction : borderEdges[i + 1].direction;
                    c.BlendDirection(n);
                    borderEdges[i] = c;
                }
                
                Border border = new Border();
                
                border.points = borderEdges
                    .Select(p => p.index)
                    .ToArray();
                
                border.directions = borderEdges
                    .Select(p => p.direction)
                    .ToArray();
                
                borders.Add(border);
            }
            
            meshData.borders = borders.ToArray();
        }
        
        public static void BuildBorders(this MeshData meshData) {
            var index = 0;
            var vertices = meshData.vertices
                .Select(v => new Vertex {
                    position = v,
                    index = index++
                })
                .ToArray();
            
            
            var triangles = new List<Triangle>();
            for (int i = 0; i < meshData.triangles.Length; i += 3) {
                var triangle = new Triangle();
                
                triangle.idx0 = meshData.triangles[i];
                triangle.idx1 = meshData.triangles[i + 1];
                triangle.idx2 = meshData.triangles[i + 2];

                triangles.Add(triangle);
            }
            
            meshData.BuildBorders(vertices, triangles);
        }

        public static void ExtructMeshData(MeshData meshData, Mesh mesh) {
            var vertices = new List<Vertex>();
            var triangles = new List<Triangle>();
            var colors = mesh.colors;
            
            bool uvProvided = mesh.vertexCount == mesh.uv.Length;
            bool uv2Provided = mesh.vertexCount == mesh.uv2.Length;
            
            for (int i = 0; i < mesh.vertexCount; i++) {
                var vertex = new Vertex();
                
                vertex.index = i;
                
                vertex.position = mesh.vertices[i];
                vertex.uv0 = uvProvided ? mesh.uv[i] : default;
                vertex.uv1 = uv2Provided ? mesh.uv2[i] : default;
                vertex.color = colors.IsEmpty() ? Color.white : colors[i];

                vertices.Add(vertex);
            }
            
            for (int i = 0; i < mesh.triangles.Length; i += 3) {
                var triangle = new Triangle();
                
                triangle.idx0 = mesh.triangles[i];
                triangle.idx1 = mesh.triangles[i + 1];
                triangle.idx2 = mesh.triangles[i + 2];

                triangles.Add(triangle);
            }

            #region Optimize

            int indexer = 0;
            
            int VertexOptimizeHash(Vertex v) {
                const float round = .001f;
                var hash = HashCode.Combine(v.position.x.Round(round), v.position.y.Round(round));
                hash = HashCode.Combine(hash, v.color);
                return hash;
            }

            var verticesOptimized = vertices
                .GroupBy(VertexOptimizeHash)
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
            
            #endregion
            
            // meshData.name = mesh.name;

            meshData.vertices = vertices.Select(v => v.position).ToArray();
            meshData.colors = vertices.Select(v => v.color).ToArray();
            meshData.triangles = triangles.SelectMany(v => v.GetIDs()).ToArray();
            meshData.uv0 = vertices.Select(v => v.uv0).ToArray();
            meshData.uv1 = vertices.Select(v => v.uv1).ToArray();
        }
        
        public static Mesh GenerateMeshFromSprite(Sprite sprite) {
            if (!sprite) return null;
            
            var mesh = new Mesh();
            mesh.name = sprite.name;
            
            mesh.vertices = sprite.vertices.Select(v => v.To3D()).ToArray();
            mesh.triangles = sprite.triangles.Select(u => (int) u).ToArray();
            mesh.uv = sprite.uv;
            if (sprite.HasVertexAttribute(VertexAttribute.TexCoord2))
                mesh.uv2 = sprite.GetVertexAttribute<Vector2>(VertexAttribute.TexCoord2).ToArray();
            
            return mesh;
        }
        
        #region Structs
        
        struct Vertex {
            public int index;
            
            public Vector2 position;
            
            public Vector2 uv0;
            public Vector2 uv1;
            public Color color;
        }
        
        struct Edge {
            public Vertex pointA;
            public Vertex pointB;
            
            public Vector2 normal;
            
            public void CalculateNormal(Vector2 pointN) {
                normal = pointB.position - pointA.position;
                normal = normal.FastNormalized();
                
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

            public bool ContainsIndex(int index) {
                return pointA.index == index || pointB.index == index;
            }

            public int AnotherIndex(int index) {
                if (pointA.index == index) return pointB.index;
                if (pointB.index == index) return pointA.index;
                return 0;
            }
        }
        
        struct EdgeID {
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
        
        struct Triangle {
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
        
        struct GradientPoint {
            public int index;
            
            public Vector2 direction;
            
            public void BlendDirection(Vector2 another) {
                direction = MeshUtils.BlendDirection(direction, another);
            }
        }

        #endregion
    }
}
