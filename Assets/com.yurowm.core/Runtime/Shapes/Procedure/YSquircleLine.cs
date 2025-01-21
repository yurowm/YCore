using System;
using UnityEngine;
using Yurowm.Extensions;

namespace Yurowm.Shapes {
    [RequireComponent(typeof(IYLineBehaviuor))]
    [ExecuteAlways]
    public class YSquircleLine: MonoBehaviour {
        
        IYLineBehaviuor line;
        YSquircle squircle = new YSquircle();
        
        [Range(0.0001f, 1f)]
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
        float _Corner = 2;
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
        
        [SerializeField]
        Vector2 _Size = new (5, 5);
        public Vector2 Size {
            set {
                if (value == _Size) return;
                _Size = value;
                Rebuild();
            }
            get => _Size;
        }
        
        [SerializeField]
        bool _TransformSize = false;
        public bool TransformSize {
            set {
                if (value == _TransformSize) return;
                _TransformSize = value;
                Rebuild();
            }
            get => _TransformSize;
        }
        
        RectTransform rectTransform;
        
        void Start() {
            Rebuild();
        }
        
        public void Rebuild() {
            if (line != null || this.SetupComponent(out line)) {
                var order = new YSquircle.Order {
                    size = _Size,
                    power = _Power,
                    corner = _Corner,
                    details = _Details,
                };
                if (TransformSize)
                    if (rectTransform || this.SetupComponent(out rectTransform))
                        order.size = rectTransform.rect.size;
                line.GetLine().SetPoints(squircle.GetPoints(order));
                line.SetDirty();
            }
        }

        void OnRectTransformDimensionsChange() {
            if (TransformSize)
                Rebuild();
        }

        void OnValidate() {
            Rebuild();
        }

        void OnDidApplyAnimationProperties() {
            Rebuild();
        }
    }
}