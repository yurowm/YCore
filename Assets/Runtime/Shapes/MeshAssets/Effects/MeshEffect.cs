using UnityEngine;

namespace Yurowm.Shapes {
    [ExecuteAlways]
    public abstract class MeshEffect : MonoBehaviour, IMeshEffect {

        public MeshEffectOrder order = MeshEffectOrder.Below;
        public MeshEffectOrder Order => order;
        
        public abstract void BuildMesh(MeshData meshData, MeshAsset.Order order);
        
        public bool isVisible => isActiveAndEnabled;
        
        public GameObject shape;
        
        IMeshEffectTarget _target = null;
        IMeshEffectTarget target {
            get {
                if (_target == null) {
                    _target = (shape ? shape : gameObject)
                        .GetComponent<IMeshEffectTarget>();
                    _target?.AddEffect(this);
                }
                return _target;
            }
        }

        void OnDidApplyAnimationProperties() {
            SetDirty();
        }

        void OnDestroy() {
            target?.RemoveEffect(this);
        }

        void Update() {
            if (isDirty)
                Refresh();
        }

        protected virtual void OnEnable() {
            target?.AddEffect(this);
            Refresh();
        }

        protected virtual void OnDisable() {
            target?.RemoveEffect(this);
            Refresh();
        }
        
        void Refresh() {
            target?.SetDirty();
            isDirty = false;
            
        }

        bool isDirty = true;
        protected void SetDirty() {
            isDirty = true;
        }

        void OnValidate() {
            SetDirty();
        }
    }
    
    public enum MeshEffectOrder {
        Below = 0,
        Above = 1
    }
    
    public interface IMeshEffect {
        
        bool isVisible {get;}
        MeshEffectOrder Order {get;}
        
        void BuildMesh(MeshData meshData, MeshAsset.Order order);
    }
    
    public interface IMeshEffectTarget {
        
        void SetDirty();
        void AddEffect(IMeshEffect meshEffect);
        void RemoveEffect(IMeshEffect meshEffect);
    }
}