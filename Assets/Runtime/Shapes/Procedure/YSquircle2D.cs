using UnityEngine;
using Yurowm.Colors;
using Yurowm.Extensions;

namespace Yurowm.Shapes {
    
    [RequireComponent(typeof(RectTransform))]
    public class YSquircle2D : Shape2DBehaviour, IRepaintTarget {
        YSquircle squircle = new YSquircle();
        
        public Color Color {
            get => color;
            set {
                color = value;
                SetDirty();
            }
        }

        public Color color = Color.white;
        
        public MeshBuilderBase.MeshOptimization optimizeMesh = 0;
        
        [Range(0.05f, 1f)]
        [SerializeField]
        float _Power = .5f;
        public float Power {
            set {
                if (value == _Power) return;
                _Power = value;
                SetDirty();
            }
            get => _Power;
        }
        
        [SerializeField]
        float _Corner = 50f;
        public float Corner {
            set {
                if (value == _Corner) return;
                _Corner = value;
                SetDirty();
            }
            get => _Corner;
        }
        
        [SerializeField]
        int _Details = 32;
        public int Details {
            set {
                if (value == _Details) return;
                _Details = value;
                SetDirty();
            }
            get => _Details;
        }

        RectTransform rectTransform;
        
        public override void FillMesh(MeshBuilder builder) {
            if (!rectTransform && !this.SetupComponent(out rectTransform))
                return;
            
            var order = new YSquircle.Order {
                power = _Power,
                color = color,
                details = _Details,
                size = rectTransform.rect.size,
                corner = _Corner.ClampMin(0),
                pivot = rectTransform.pivot
            };

            squircle.FillMesh(builder, order);
            
            builder.Optimize(optimizeMesh);
        }

        void OnRectTransformDimensionsChange() {
            SetDirty();
        }
    }
}