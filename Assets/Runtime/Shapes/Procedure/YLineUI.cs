using UnityEngine;
using System.Collections.Generic;
using Yurowm.Colors;

namespace Yurowm.Shapes {
    public class YLineUI : ShapeUIBehaviour, IRepaintTarget, IYLineBehaviuor {
        
        [ContextMenu("Test")]
        void Test() {
            if (Application.isPlaying) return;
            Clear();
            line.AddPoint(Vector2.zero);
            line.AddPoint(Vector2.up);
            line.AddPoint(Vector2.one);
            line.AddPoint(new Vector2(1, -1));
            line.AddPoint(-Vector2.up);
            SetDirty();
        }
        
        public YLine line = new YLine();

        public YLine GetLine() => line;
        
        public Color Color {
            get => color; 
            set => color = value;
        }
        
        #region Points
        
        public void AddPoint(Vector2 point) {
            line.AddPoint(point);
            SetDirty();
        }
        public void SetPoints(IEnumerable<Vector2> points) {
            line.SetPoints(points);
            SetDirty();
        }

        public void ChangePoint(int id, Vector2 point) {
            line.ChangePoint(id, point);
            SetDirty();
        }

        public override void Clear() {
            base.Clear();
            line.Clear();
            SetDirty();
        }
        
        #endregion

        public MeshBuilderBase.MeshOptimization optimizeMesh = 0;
        
        public YLine.ConnectionType type;
        public int smooth;
        public bool directionNormals;
        public bool removeDuplicates;
        
        [Range(0.1f, 1f)]
        public float smoothPower = 0.33f;
        
        public float tileY = 1f;

        [SerializeField]
        float _Thickness = 1f;
        public float Thickness {
            set {
                if (value == _Thickness) return;
                _Thickness = value;
                SetDirty();
            }
            get => _Thickness;
        }

        [SerializeField]
        bool _Loop;
        public bool Loop {
            set {
                if (_Loop == value) return;
                _Loop = value;
                SetDirty();
            }
            get => _Loop;
        }

        public override void SetDirty() {
            base.SetDirty();
            SetAllDirty();
        }

        public float GetLength() {
            return line.GetLength();
        }
        
        public override void FillMesh(MeshUIBuilder builder) {
            var order = new YLine.Order {
                type = type,
                color = color,
                directionNormals = directionNormals,
                removeDuplicates = removeDuplicates,
                thickness = Thickness,
                tileY = tileY,
                smooth = smooth,
                smoothPower = smoothPower,
                loop = Loop
            };
            
            line.FillMesh(builder, order);
            
            builder.Optimize(optimizeMesh);
        }
    }
}
