using System.Collections.Generic;
using UnityEngine;

namespace Yurowm.Shapes {
    public class YSquircle {

        public void FillMesh(MeshBuilderBase builder, Order order) {
            
            var offset = (Vector2.one * .5f - order.pivot) * order.size;
            builder.AddVert(offset, order.color);
            
            foreach (var vertex in GetPoints(order)) 
                builder.AddVert(vertex + offset, order.color);
            
            builder.AddTriangle(0, 1, order.details * 4);

            for (int i = 1; i < order.details * 4; i++) 
                builder.AddTriangle(0, i + 1, i);
        }

        public IEnumerable<Vector2> GetPointsForCorner(Order order, float cornerAngle) {
            if (order.size.x <= 0 || order.size.y <= 0)
                yield break;
            
            var step = 90f / (order.details - 1);
            
            var corner = order.corner
                .ClampMax(order.size.x / 2)
                .ClampMax(order.size.y / 2);
            
            var edges = order.size / 2 - Vector2.one * corner;
            var vector = new Vector2(
                YMath.CosDeg(cornerAngle + 45).Sign(),
                YMath.SinDeg(cornerAngle + 45).Sign());
            
            Vector2 IToVertex(int i) {
                var angle = cornerAngle + step * i;
                var vertex = new Vector2(YMath.CosDeg(angle), YMath.SinDeg(angle));
                vertex.x = Mathf.Pow(vertex.x.Abs(), order.power) * Mathf.Sign(vertex.x) / 2;
                vertex.y = Mathf.Pow(vertex.y.Abs(), order.power) * Mathf.Sign(vertex.y) / 2;
                
                vertex *= corner * 2;
                
                vertex += edges * vector;
                
                return vertex;
            }
            
            for (int i = 0; i < order.details; i++)
                yield return IToVertex(i);
            
            // yield return IToVertex(0);
        }
        
        public IEnumerable<Vector2> GetPoints(Order order) {
            for (var a = 0f; a < 4; a ++)
                foreach (var point in GetPointsForCorner(order, a * 90))
                    yield return point;
        }
        
        public struct Order {
            public Color color;
            public float corner;
            public int details;
            public float power;
            public Vector2 size;
            public Vector2 pivot;
        }
    }
}