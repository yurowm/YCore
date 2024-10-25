using System.Collections.Generic;
using UnityEngine;
using Yurowm.Extensions;

namespace Yurowm.Shapes {
    [RequireComponent(typeof(IYLineBehaviuor))]
    [ExecuteAlways]
    public class DashedCircleLine: MonoBehaviour {
        
        IYLineBehaviuor line;
        
        [SerializeField]
        float _Radius = .5f;
        public float Radius {
            set {
                if (value == _Radius) return;
                _Radius = value;
                Rebuild();
            }
            get => _Radius;
        }
        
        [SerializeField]
        float _Space = 2f;
        public float Space {
            set {
                if (value == _Space) return;
                _Space = value;
                Rebuild();
            }
            get => _Space;
        }
        
        [SerializeField]
        float _Segments = 2;
        public float Segments {
            set {
                if (value == _Segments) return;
                _Segments = value;
                Rebuild();
            }
            get => _Segments;
        }
        
        int SegmentsCount => _Segments.CeilToInt();
        
        float Fade => 1f - SegmentsCount + _Segments;
        
        [SerializeField]
        int _Details = 32;
        public int Details {
            set {
                if (value == _Details) return;
                _Details = value;
                Rebuild();
            }
            get => _Details;
        }
        
        public void Rebuild() {
            if (line != null || this.SetupComponent(out line)) {
                var l = line.GetLine();
                l.Clear();
                BuildLine(l);
                line.SetDirty();
            }
        }
        
        void BuildLine(YLine line) {
            _Details = _Details.ClampMin(3);
            
            if (Radius <= 0) return;
            
            if (Space <= 0 || Segments <= 1) {
                // simple circle
                for (var i = 0; i < _Details; i++)
                    line.AddPoint(AngleToVector(360f * i / _Details));
                
                line.AddPoint(AngleToVector(0));
                
                return;
            }
            
            var lineBudget = 360f - _Segments * Space;
            
            if (lineBudget <= 0)
                return;
            
            var angle = - _Space / 2;
            var segmentDetails = (1f * _Details / SegmentsCount).CeilToInt().ClampMin(2);
            var segmentAngle = lineBudget / _Segments;
            
            for (var segment = 0; segment < SegmentsCount; segment++) {
                var lastSegmet = segment == SegmentsCount - 1;
                
                angle += _Space * (lastSegmet ? (1f + Fade) / 2 : 1f);
                
                line.AddPoint(AngleToVector(angle));
                
                for (var i = 0; i < segmentDetails; i++) {
                    angle += segmentAngle * (lastSegmet ? Fade : 1) / segmentDetails;
                    line.AddPoint(AngleToVector(angle));
                }
                
                line.AddBreaker();
            }
        }
        
        Vector2 AngleToVector(float degress) => new Vector2(_Radius, 0).Rotate(degress);

        void OnValidate() {
            Rebuild();
        }

        void OnDidApplyAnimationProperties() {
            Rebuild();
        }
    }
}