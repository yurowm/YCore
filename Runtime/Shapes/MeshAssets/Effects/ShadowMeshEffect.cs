using System.Collections.Generic;
using UnityEngine;
using Yurowm.Colors;
using Yurowm.Extensions;

namespace Yurowm.Shapes {
    public class ShadowMeshEffect : MeshEffect, IRepaintTarget {
        
        public Vector2 offset = new Vector2(0, -2);
        public float smooth = 2;
        public float size = 0;
        
        [SerializeField]
        Color color = Color.black;
        
        public Color Color {
            get => color;
            set {
                if (color == value) return;
                color = value;
                SetDirty();
            }
        }
        
        static readonly List<Vector2> positions = new List<Vector2>();
        static readonly List<Vector2> directions = new List<Vector2>();
        
        public override void BuildMesh(MeshData meshData, MeshAsset.Order order) {
            bool dynimicBorderDirection = order.options
                .HasFlag(MeshAsset.Order.Options.DynimicBorderDirections);
            
            float smooth = Mathf.Max(0, this.smooth);
            float size = Mathf.Max(0, this.size);
            
            // if (order.options.HasFlag(MeshAsset.Order.Options.Antialising))
            //     smooth = Mathf.Max(smooth, order.builder.GetPointSize());

            int initialCount = order.builder.currentVertCount;
            int shapeVertexCount = order.vertices.Length;
            
            positions.Clear();
            positions.AddRange(order.vertices);
            
            foreach (var border in meshData.borders) {
                int currentCount = initialCount + positions.Count;

                directions.Clear();
                
                for (int i = 0; i < border.Length; i++) {
                    var direction = dynimicBorderDirection ?
                        MeshUtils.CulculateBorderDirection(positions, border, i) : 
                        MeshUtils.GetTransformVertex(order.transformMode, border.directions[i]);
                    
                    if (dynimicBorderDirection && order.flip)
                        direction *= -1;
                    
                    directions.Add(direction);
                }
                
                for (int i = 0; i < border.Length; i++) {
                    var index = border.points[i];
                    
                    var direction = directions[i];
                    
                    var vertex = positions[index];
                    
                    vertex += direction * (size - smooth / 2);
                    
                    positions[index] = vertex;
                    
                    positions.Add(vertex + direction * smooth);
                }

                for (int i = 1; i <= border.points.Length; i++) {
                    int c = i == border.points.Length ? 0 : i;
                    int p = i - 1;
                
                    AddQaud(order.builder, 
                        initialCount + border.points[c],
                        initialCount + border.points[p],
                        currentCount + c,
                        currentCount + p,
                        order.flip);
                }
            }
            
            for (int i = 0; i < positions.Count; i++) 
                AddVertex(order.builder, positions[i] + offset, i < shapeVertexCount ? 1 : 0);
            
            for (int i = 0; i < meshData.triangles.Length; i += 3)
                order.builder.AddTriangle(
                    initialCount + meshData.triangles[i],
                    initialCount + meshData.triangles[i + 1],
                    initialCount + meshData.triangles[i + 2],
                    order.flip);
        }

        void AddVertex(IShapeBuilder builder, Vector2 position, float alpha) {
            builder.AddVert(position,
                color.TransparentMultiply(alpha),
                Vector2.zero, Vector2.zero, 
                Vector3.back, new Vector4(1, 0, 0, -1));
        }
        
        void AddQaud(IShapeBuilder builder, int indexC, int indexP, int indexAC, int indexAP, bool flip) {
            builder.AddTriangle(indexP, indexAP, indexC, flip);
            builder.AddTriangle(indexC, indexAP, indexAC, flip);
        }
    }
}