using System.Collections.Generic;
using UnityEngine;
using Yurowm.Extensions;
using Yurowm.Utilities;

namespace Yurowm.Shapes {
    [ExecuteInEditMode]
    [RequireComponent(typeof(YLine2D))]
    public class Trail : BaseBehaviour {
        List<Vector2> points = new List<Vector2>();
        
        public Transform positionProvider;
        
        public float vertexDistance = .1f;
        public float maxDistance = 3f;
        
        YLine2D line;
        
        Transform masterTransform {
            get {
                if (positionProvider)
                    return positionProvider;
                return transform;
            }
        }

        void OnEnable() {
            points.Clear();
        }

        void LateUpdate() {
            if (vertexDistance < 0.1f) vertexDistance = 0.1f;
            
            //TODO: Refactor it
            
            float distance = 0;
            
            var t = masterTransform;
            
            if (points.Count == 0) {
                points.Add(t.position);
            } else {
                Vector2 lastPoint = points[^1];
                distance = (lastPoint - (Vector2) t.position).FastMagnitude() - vertexDistance / 10;
                
                while (distance > vertexDistance) {
                    lastPoint = Vector2.MoveTowards(lastPoint, t.position, vertexDistance);
                    points.Add(lastPoint);
                    distance -= vertexDistance;
                }
            }
            
            if (points.Count >= 3)
                distance += (points.Count - 2) * vertexDistance;

            float endVertexDistance = (GetLastPoint() - GetLastFixedPoint()).FastMagnitude();
            
            distance += endVertexDistance;
            
            while (distance > maxDistance && points.Count > 0) {
                float cropDistance = endVertexDistance > 0 ? endVertexDistance : vertexDistance;
                
                float delta = distance - maxDistance;
                if (delta >= cropDistance) {
                    points.RemoveAt(0);
                    distance -= cropDistance;
                    endVertexDistance = 0;
                } else {
                    var a = GetLastFixedPoint();
                    var b = GetLastPoint();
                    
                    float d = (a - b).FastMagnitude() - delta;
                    
                    points[0] = Vector2.MoveTowards(a, b, d);
                    distance = -delta;
                }
            }
            
            if (!line && !this.SetupComponent(out line)) 
                return;
            
            line.Clear();

            if (points.IsEmpty()) return;
            
            var nearPoint = points[^1];
            
            if ((nearPoint - (Vector2) t.position).MagnitudeIsGreaterThan(vertexDistance / 10))
                line.AddPoint(Vector2.zero);
            else 
                line.AddPoint(transform.InverseTransformPoint(nearPoint));
            
            for (int i = points.Count - 2; i >= 0; i--)
                line.AddPoint(transform.InverseTransformPoint(points[i]));
            
            line.RebuildImmediate();
        }
        
        Vector2 GetLastFixedPoint() {
            return points.Count > 1 ? points[1] : masterTransform.position.To2D();
        }
        
        Vector2 GetLastPoint() {
            return points.Count > 0 ? points[0] : masterTransform.position.To2D();
        }
    }
}