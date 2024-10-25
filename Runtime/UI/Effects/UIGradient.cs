using UnityEngine;
using System.Collections.Generic;
using UnityEngine.UI;
using Yurowm.Extensions;
using Yurowm.Utilities;

namespace Yurowm.UI {
    [AddComponentMenu("UI/Effects/Gradient")]
    public class UIGradient : BaseMeshEffect {
        public enum Type {
            Linear = 0,
            Radial = 1
        }
    
        public Type GradientType = Type.Linear;

        public Vector2 offset;
        public float angle;
        public float scale = 1;
    
        public bool multiply = true;

        public Gradient gradient;
        
        static readonly List<UIVertex> list = new List<UIVertex>();
        
        BoundDetector boundDetector = new BoundDetector(); 
        BoundDetector2D boundDetector2D = new BoundDetector2D(); 
    
        public void Refresh() {
            graphic.SetVerticesDirty();
        }

        public override void ModifyMesh(VertexHelper vh) {
            if (!IsActive())
                return;

            list.Clear();
            vh.GetUIVertexStream(list);

            if (list.Count == 0) return;
            
            switch (GradientType) {
                case Type.Linear: ApplyLinear(); break;
                case Type.Radial: ApplyRadial(); break;
            }

            vh.AddUIVertexTriangleStream(list);
        }
        
        void ApplyLinear() {
            boundDetector2D.Clear();
            list.ForEach(v => boundDetector2D.Set(v.position));
            
            var center = boundDetector2D.GetBound().center;

            var direction = Vector2.right.Rotate(angle);
            
            boundDetector.Clear();
            list.ForEach(v => 
                boundDetector.Set(Vector2.Dot(direction, v.position.To2D() - center)));
            
            var gradientRange = boundDetector.GetBound();

            for (int i = list.Count - 1; i >= 0; --i) {
                var vertex = list[i];
                
                var dot = Vector2.Dot(direction, (vertex.position.To2D() - center - offset) / scale);
                var t = gradientRange.GetTime(dot);
                var color = gradient.Evaluate(t);
                            
                if (multiply) 
                    color = color.Multiply(vertex.color);
                            
                vertex.color = color;
                list[i] = vertex;
            }
        }
        
        void ApplyRadial() {
            boundDetector2D.Clear();
            list.ForEach(v => boundDetector2D.Set(v.position));
            
            var center = boundDetector2D.GetBound().center;

            float radius = 0;

            foreach (var v in list) {
                var offset = v.position.To2D() - center;
                if (offset.MagnitudeIsGreaterThan(radius))
                    radius = offset.FastMagnitude();
            }
            
            for (int i = list.Count - 1; i >= 0; --i) {
                var vertex = list[i];
                
                Color color;
                if (scale != 0 && radius > 0) {
                    var distance = ((vertex.position.To2D() - center - offset) / scale).FastMagnitude();
                    color = gradient.Evaluate((distance / radius).Clamp01());
                } else
                    color = gradient.Evaluate(1);
                
                if (multiply) 
                    color = color.Multiply(vertex.color);
                            
                vertex.color = color;
                list[i] = vertex;
            }
        }
    }
}