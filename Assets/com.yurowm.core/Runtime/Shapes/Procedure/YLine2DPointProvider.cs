using UnityEngine;
using Yurowm.Extensions;

namespace Yurowm.Shapes {
    [ExecuteAlways]
    [RequireComponent(typeof(YLine2D))]
    public class YLine2DPointProvider: MonoBehaviour {
        
        YLine2D line;
        
        public bool loop = false;

        void OnDisable() {
            Update();
        }

        void Update() {
            if (!line && !this.SetupComponent(out line)) {
                enabled = false;
                return;
            }
            
            line.Clear();
            
            Vector2 lastPoint = default;
            
            var empty = true;
            
            for (var i = 0; i < transform.childCount; i++) {
                var child = transform.GetChild(i);
                
                if (!child.gameObject.activeInHierarchy)
                    continue;
                    
                var point = child.localPosition.To2D();
                
                if (empty || point != lastPoint) {
                    line.AddPoint(point);
                    empty = false;
                }
                
                lastPoint = point;
            }
            
            line.Loop = loop;
        }
    }
}