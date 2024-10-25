using UnityEngine;
using Yurowm.Colors;
using Yurowm.Extensions;

namespace Yurowm.Shapes {
    public class OutlineMeshEffect : MeshEffect, IRepaintTarget {
        
        public float lineOffset = 0;
        public float lineWidth = 2;
        
        [SerializeField]
        Color color = Color.green;
        
        public Color Color {
            get => color;
            set {
                if (color == value) return;
                color = value;
                SetDirty();
            }
        }
        
        public override void BuildMesh(MeshData meshData, MeshAsset.Order order) {
            bool dynimicBorderDirection = order.options
                .HasFlag(MeshAsset.Order.Options.DynimicBorderDirections);
            
            // bool antialiasing = order.options
            //     .HasFlag(MeshAsset.Order.Options.Antialising);
            
            var antialiasing = false;
            
            float antialiasingSize = antialiasing ? order.builder.GetPointSize() : 0;

            foreach (var border in meshData.borders) {
                int currentCount = order.builder.currentVertCount;
                
                for (int i = 0; i < border.Length; i++) {
                    var index = border.points[i];
                    var direction = dynimicBorderDirection ?
                        MeshUtils.CulculateBorderDirection(order.vertices, border, i) : 
                        MeshUtils.GetTransformVertex(order.transformMode, border.directions[i]);
                    
                    if (dynimicBorderDirection && order.flip)
                        direction *= -1;
                    
                    var vertex = order.vertices[index];
                    
                    if (antialiasing)
                        AddVertex(order.builder, vertex + direction * (lineOffset - lineWidth / 2 - antialiasingSize), 0);
                    AddVertex(order.builder, vertex + direction * (lineOffset - lineWidth / 2), 1);
                    AddVertex(order.builder, vertex + direction * (lineOffset + lineWidth / 2), 1);
                    if (antialiasing)
                        AddVertex(order.builder, vertex + direction * (lineOffset + lineWidth / 2 + antialiasingSize), 0);
                }
                
                for (int i = 1; i <= border.points.Length; i++) {
                    int c = i == border.points.Length ? 0 : i;
                    int p = i - 1;

                    if (antialiasing) {
                        AddQaud(order.builder, 
                            currentCount + c * 4,
                            currentCount + p * 4,
                            currentCount + c * 4 + 1,
                            currentCount + p * 4 + 1,
                            order.flip);
                            
                        AddQaud(order.builder, 
                            currentCount + c * 4 + 1,
                            currentCount + p * 4 + 1,
                            currentCount + c * 4 + 2,
                            currentCount + p * 4 + 2,
                            order.flip);
                            
                        AddQaud(order.builder, 
                            currentCount + c * 4 + 2,
                            currentCount + p * 4 + 2,
                            currentCount + c * 4 + 3,
                            currentCount + p * 4 + 3,
                            order.flip);
                    } else {
                        AddQaud(order.builder, 
                            currentCount + c * 2,
                            currentCount + p * 2,
                            currentCount + c * 2 + 1,
                            currentCount + p * 2 + 1,
                            order.flip);
                    }
                }
            }
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