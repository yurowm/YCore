using System.Collections.Generic;
using UnityEngine;
using Yurowm.Colors;
using Yurowm.Extensions;

namespace Yurowm.Shapes {
    public class ExtrudeMeshEffect : MeshEffect, IRepaintTarget {

        public Vector2 offset = new Vector2(0, -1);
        
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
        
        List<Extrude> extrudes = new List<Extrude>();
        List<Vector2> directions = new List<Vector2>();
        
        public override void BuildMesh(MeshData meshData, MeshAsset.Order order) {
            if (offset.IsEmpty()) return;
            
            bool dynimicBorderDirection = order.options
                .HasFlag(MeshAsset.Order.Options.DynimicBorderDirections);
            
            // bool antialiasing = order.options
            //     .HasFlag(MeshAsset.Order.Options.Antialising);
            
            var antialiasing = false;
            
            float antialiasingSize = antialiasing ? order.builder.GetPointSize() : 0;
            
            var offsetNormalized = offset.FastNormalized();
            
            Extrude? currentExtrude = null;

            foreach (var border in meshData.borders) {
                extrudes.Clear();
                directions.Clear();
                
                int length = border.Length;
                
                bool visibleFirst = false;
                
                for (int i = 0; i < length; i++) {
                
                    var direction = dynimicBorderDirection ?
                        MeshUtils.CulculateBorderDirection(order.vertices, border, i) : 
                        MeshUtils.GetTransformVertex(order.transformMode, border.directions[i]);
                    
                    if (dynimicBorderDirection && order.flip)
                        direction *= -1;
                    
                    if (antialiasing)
                        directions.Add(direction);
                    
                    var vertex = order.vertices[border.points[i]];
                    
                    
                    var index = border.points[(i + 1).Repeat(length)];
                        
                    bool visible = (vertex - order.vertices[index])
                               .Perpendicular(direction).FastNormalized()
                               .Dot(offsetNormalized) > 0.1f;

                    if (visible) {
                        if (currentExtrude.HasValue) {
                            var extr = currentExtrude.Value;
                            extr.last = i;
                            currentExtrude = extr;
                        } else {
                            currentExtrude = new Extrude {
                                first = i,
                                last = i
                            };
                        }
                    } else {
                        if (currentExtrude.HasValue) {
                            extrudes.Add(currentExtrude.Value);
                            currentExtrude = null;
                        }
                    }
                }
                
                if (currentExtrude.HasValue) {
                    if (extrudes.Count > 0 && extrudes[0].first == 0) {
                        var firstExtrude = extrudes[0];
                        firstExtrude.first = currentExtrude.Value.first;
                        extrudes[0] = firstExtrude;
                    } else
                        extrudes.Add(currentExtrude.Value);

                    currentExtrude = null;
                }
                
                if (extrudes.Count == 0)
                    continue;

                while (extrudes.Count > 0) {
                    int currentCount = order.builder.currentVertCount;
                    
                    var extrude = extrudes.Grab();
                    
                    var firstIndex = extrude.first;
                    var lastIndex = (extrude.last + 1).Repeat(length);
                    
                    var extrudeLength = lastIndex - firstIndex + 1;
                    if (extrudeLength <= 0) extrudeLength = length + extrudeLength;
                    
                    int pointCounter = 0;
                    
                    for (int i = 0; i < extrudeLength; i ++) {
                        
                        int ir = (firstIndex + i).Repeat(length);
                        
                        int index = border.points[ir];
                        var vertex = order.vertices[index];

                        Vector2 direction = antialiasing ? directions[ir] : default;
                        
                        if (antialiasing) {
                            if (ir == firstIndex || ir == lastIndex) {
                                bool first = ir == firstIndex;
                                int indexX = border.points[(ir + (first ? 1 : -1)).Repeat(length)];
                                var vertexX = order.vertices[indexX];
                                
                                var cw = first;
                                if (order.flip) cw = !cw;
                                
                                var directionX = MeshUtils.BlendDirection(
                                    offset.Perpendicular(!cw).FastNormalized(), 
                                    (vertex - vertexX).Perpendicular(!cw).FastNormalized());
                                
                                AddVertex(order.builder, vertex + directionX * antialiasingSize, 0);
                            } else
                                AddVertex(order.builder, vertex - direction * antialiasingSize, 0);
                        }

                        AddVertex(order.builder, vertex, 1);
                        AddVertex(order.builder, vertex + offset, 1);
                        
                        if (antialiasing) {
                            if (ir == firstIndex || ir == lastIndex) {
                                bool first = ir == firstIndex;
                                int indexX = border.points[(ir + (first ? 1 : -1)).Repeat(length)];
                                var vertexX = order.vertices[indexX];
                                
                                var cw = first;
                                if (order.flip) cw = !cw;
                                
                                var directionX = MeshUtils.BlendDirection(
                                    offset.Perpendicular(!cw).FastNormalized(), 
                                    (vertex - vertexX).Perpendicular(cw).FastNormalized());
                                
                                AddVertex(order.builder, vertex + offset + directionX * antialiasingSize, 0);
                            } else
                                AddVertex(order.builder, vertex + offset + direction * antialiasingSize, 0);
                        }
                    
                        pointCounter ++;
                    }
                    
                    if (antialiasing) {
                        
                        var index = currentCount;
                        
                        AddQaud(order.builder, 
                            index + 3,
                            index + 2,
                            index,
                            index + 1,
                            order.flip);
                        
                        index += (pointCounter - 1) * 4;
                        
                        AddQaud(order.builder, 
                            index,
                            index + 1,
                            index + 3,
                            index + 2,
                            order.flip);
                                
                    }
                    
                    for (int i = 1; i < pointCounter; i++) {
                        int c = i;
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
        
        struct Extrude {
            public int first;
            public int last;
        }
    }
}