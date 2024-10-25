using System;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEngine;
using Yurowm.Extensions;

namespace Yurowm.Shapes {
    
    using TransformMode = MeshUtils.TransformMode;
    
    public class MeshAsset : ScriptableObject {
        
        [HideInInspector]
        public MeshData meshData;
        
        #region Asset Creating

        public static MeshAsset Create(Mesh mesh) {
            var asset = CreateInstance<MeshAsset>();
            
            asset.meshData = MeshUtils.GenerateMeshData(mesh);
            
            return asset;
        }

        #endregion
        
        #region Mesh Building
        
        public struct Order {
            public IShapeBuilder builder;
            public Color32 color;
            public Vector2[] vertices;
            public TransformMode transformMode;
            public Options options;
            
            internal bool flip;
            
            [Flags]
            public enum Options {
                // Antialising = 1 << 1,
                DynimicBorderDirections = 1 << 2,
            }
            
            public Order(IShapeBuilder builder) {
                this.builder = builder;
                color = new Color32(255, 255, 255, 255);
                vertices = null;
                transformMode = 0;
                options = 0;
                flip = false;
            }
        }

        void BuildMeshAntialiasing(Order order) {
            float antialiasingSize = order.builder.GetPointSize();
            
            if (antialiasingSize <= 0) return;
            
            int initialCount = order.builder.currentVertCount - order.vertices.Length;
            
            bool dynimicBorderDirection = order.options.HasFlag(Order.Options.DynimicBorderDirections);

            foreach (var border in meshData.borders) {
                int currentCount = order.builder.currentVertCount;
                
                for (int i = 0; i < border.Length; i++) {
                    var index = border.points[i];
                    var direction = dynimicBorderDirection ?
                        MeshUtils.CulculateBorderDirection(order.vertices, border, i) : 
                        MeshUtils.GetTransformVertex(order.transformMode, border.directions[i]);
                    
                    if (dynimicBorderDirection && order.flip)
                        direction *= -1;
                    
                    order.builder.AddVert(order.vertices[index] + direction * antialiasingSize,
                        order.color.Transparent(0),
                        meshData.uv0[index], meshData.uv1[index], 
                        Vector3.back, new Vector4(1, 0, 0, -1));
                }
                
                for (int i = 1; i <= border.points.Length; i++) {
                    int c = i == border.points.Length ? 0 : i;
                    int p = i - 1;
                    
                    var indexC = initialCount + border.points[c];
                    var indexP = initialCount + border.points[p];
                    var indexAC = currentCount + c;
                    var indexAP = currentCount + p;
                    
                    order.builder.AddTriangle(indexP, indexAP, indexC, order.flip);
                    order.builder.AddTriangle(indexC, indexAP, indexAC, order.flip);
                }
            }
        }
        
        #endregion
    }
    
    [Serializable]
    public class MeshData {
        public float Scale = 1f;
        
        public Vector2[] vertices;
        
        public Color[] colors;
        
        public int[] triangles;
        
        public Vector2[] uv0;
        
        public Vector2[] uv1;

        public Border[] borders;
        
        [Serializable]
        public class Border {
            public int[] points;
            public Vector2[] directions;
            public int Length => points.Length;
        }
        
        
        public IEnumerable<Vector2> GetTransformVertices(TransformMode mode) {
            foreach (var vertex in vertices)
                yield return MeshUtils.GetTransformVertex(mode, vertex) * Scale;
        }
    }
    
    public interface IMeshDataComponent {
        MeshData meshData {get; set;}
        
        
    }
    
}