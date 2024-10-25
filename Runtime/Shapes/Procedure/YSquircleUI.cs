using UnityEngine;

namespace Yurowm.Shapes {
    [RequireComponent(typeof(RectTransform))]
    public class YSquircleUI : ShapeUIBehaviour {
        YSquircle squircle = new();

        public MeshBuilderBase.MeshOptimization optimizeMesh = 0;
        
        [Range(0.05f, 1f)]
        [SerializeField]
        float _Power = .5f;
        public float Power {
            set {
                if (value == _Power) return;
                _Power = value;
                Rebuild();
            }
            get => _Power;
        }
        
        [SerializeField]
        float _Corner = 50f;
        public float Corner {
            set {
                if (value == _Corner) return;
                _Corner = value;
                Rebuild();
            }
            get => _Corner;
        }
        
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
        
        public override void FillMesh(MeshUIBuilder builder) {
            var rt = rectTransform;
            
            var order = new YSquircle.Order {
                power = _Power,
                color = color,
                details = _Details,
                size = rt.rect.size,
                corner = _Corner.ClampMin(0),
                pivot = rt.pivot
            };

            squircle.FillMesh(builder, order);
            
            builder.Optimize(optimizeMesh);
        }
        
        protected override void OnDidApplyAnimationProperties() {
            base.OnDidApplyAnimationProperties();
            Rebuild();
        }
    }
}