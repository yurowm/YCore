using UnityEngine;

namespace Yurowm {
    public class BaseBehaviour : MonoBehaviour {

        #region Automate

        [ContextMenu("Automate")]
        void Automate() {
            #if UNITY_EDITOR
            OnAutomateAction();
            UnityEditor.EditorUtility.SetDirty(this);
            #endif
        }
        
        protected virtual void OnAutomateAction() { }

        #endregion
        
        public bool active {
            get => gameObject.activeSelf;
            set => gameObject.SetActive(value);
        }

        public RectTransform rectTransform => transform as RectTransform;
        
        Animation _animation;
        public new Animation animation {
            get {
                if (!_animation)
                    _animation = GetComponent<Animation>();
                return _animation;
            }
        }

        Renderer _renderer;
        public new Renderer renderer {
            get {
                if (!_renderer)
                    _renderer = GetComponent<Renderer>();
                return _renderer;
            }
        }

        SpriteRenderer _spriteRenderer;
        public SpriteRenderer spriteRenderer {
            get {
                if (!_spriteRenderer)
                    _spriteRenderer = GetComponent<SpriteRenderer>();
                return _spriteRenderer;
            }
        }

        MeshFilter _meshFilter;
        public MeshFilter meshFilter {
            get {
                if (!_meshFilter)
                    _meshFilter = GetComponent<MeshFilter>();
                return _meshFilter;
            }
        }

        public MeshRenderer meshRenderer => renderer as MeshRenderer;
        
        #if PHYSICS_3D
        
        Rigidbody _rigidbody;
        public new Rigidbody rigidbody {
            get {
                if (!_rigidbody)
                    _rigidbody = GetComponent<Rigidbody>();
                return _rigidbody;
            }
        }
        
        #endif
        
        #if PHYSICS_2D

        Rigidbody2D _rigidbody2D;
        public new Rigidbody2D rigidbody2D {
            get {
                if (!_rigidbody2D)
                    _rigidbody2D = GetComponent<Rigidbody2D>();
                return _rigidbody2D;
            }
        }
        
        Collider2D _collider;
        public new Collider2D collider {
            get {
                if (!_collider)
                    _collider = GetComponent<Collider2D>();
                return _collider;
            }
        }
        
        #endif
        
        ParticleSystem _particleSystem;
        public new ParticleSystem particleSystem {
            get {
                if (!_particleSystem)
                    _particleSystem = GetComponent<ParticleSystem>();
                return _particleSystem;
            }
        }
        
    }
}