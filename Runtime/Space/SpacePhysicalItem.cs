using System;
using UnityEngine;
using Yurowm.ContentManager;
using Yurowm.Extensions;
using Yurowm.Jobs;
using Yurowm.Serialization;

namespace Yurowm.Spaces {

    public abstract class SpacePhysicalItemBase : GameEntity, IBody {
        
        float _size = 1f;
        public virtual float size {
            get => _size;
            set {
                _size = value;
                if (body)
                    ApplyTransform(body.transform);
            }
        }
        
        public string bodyName {get; set;}
        
        public virtual Type BodyType => typeof(SpaceObject);
        
        public void SetBody(SpaceObject body) {
            this.body = body;
            if (body) 
                onSetBody.Invoke(body);
            OnSetBody();
            SendOnSetBody();
        }
        
        public static SPI New<SPI>(string bodyName) where SPI : SpacePhysicalItemBase {
            var result = Activator.CreateInstance<SPI>();
            result.bodyName = bodyName;
            return result;
        }
        
        public override void OnEnable() {
            base.OnEnable();
            body?.gameObject.SetActive(true);
            if (this is IVisibilitySpecified ivs) {
                if (ivs.isVisible)
                    ivs.OnVisible();
                else
                    ivs.OnInvisible();
            }
        }

        public override void OnDisable() {
            base.OnDisable();
            body?.gameObject.SetActive(false);
        }

        #region ISpaceEntity
        public override void OnAddToSpace(Space space) {
            base.OnAddToSpace(space);
            if (!bodyName.IsNullOrEmpty()) {
                SetBody(EmitBody());
            }
            space.Subscribe(this);
        }

        public override void OnRemoveFromSpace(Space space) {
            onRemoveFromSpace();
            base.OnRemoveFromSpace(space);
            KillBody();
        }
        
        public void KillBody() {
            if (body) body.Kill();
            body = null;
        }
        
        #endregion

        public SpaceObject body;
        
        protected virtual void OnSetBody() { }
        
        #if PHYSICS_2D
        
        Collider2D _collider2D;
        public Collider2D collider2D {
            get {
                if (_collider2D || (body && body.SetupComponent(out _collider2D)))
                    return _collider2D;
                return null;
            }
        }
        
        Rigidbody2D _rigidbody2D;
        public Rigidbody2D rigidbody2D {
            get {
                if (_rigidbody2D || (body && body.SetupComponent(out _rigidbody2D)))
                    return _rigidbody2D;
                return null;
            }
        }
        
        #endif

        protected virtual Transform GetRoot() {
            return space?.root;
        }
        
        public SingleCallEvent<SpaceObject> onSetBody = new SingleCallEvent<SpaceObject>();
        public virtual SpaceObject EmitBody() {
            var result = AssetManager.Emit<SpaceObject>(bodyName, context);
            if (result) {
                result.transform.SetParent(GetRoot());            
                ApplyTransform(result.transform);
                result.item = this;
            }
            return result;
        }
        
        protected virtual void ApplyTransform(Transform transform) {
            transform.localScale = new (_size, _size, .03f);
        }

        public void ApplyTransform() {
            if (body)
                ApplyTransform(body.transform);
        } 

        public void SendOnSetBody() {
            body?.GetComponentsInChildren<IOnSetBodyHandler>()
                .ForEach(h => h.OnSetBody(this));
        }
        
        public override void OnKill() {
            if (body) body.Destroying();
            body = null;
            base.OnKill();
        }

        #region ISerializable
        public override void Serialize(IWriter writer) {
            base.Serialize(writer);
            if (!bodyName.IsNullOrEmpty()) writer.Write("body", bodyName);
            
            if (size != 1) writer.Write("size", size);
        }

        public override void Deserialize(IReader reader) {
            base.Deserialize(reader);
            bodyName = reader.Read<string>("body");
            if (!reader.Read("size", ref _size))
                _size = 1;
        }
        #endregion

        #region IVisibilitySpecified
        public bool isVisible {get; private set;} = true;

        public virtual float GetVisibleSize() {
            return size;
        }

        public virtual void OnVisible() {
            isVisible = true;
        }

        public virtual void OnInvisible() {
            isVisible = false;
        }
        #endregion

    }
    
    public class SpacePhysicalItem : SpacePhysicalItemBase {

        #region Dimensions
        Vector2 _position = new Vector2();
        public Action<SpacePhysicalItem> onChangePosition = null;
        public virtual Vector2 position {
            get => _position;
            set {
                if (_position == value) return;
                _position = value;
                onChangePosition?.Invoke(this);
            }
        }
        
        public float depth = 0;

        Vector2 _velocity = new Vector2();
        public virtual Vector2 velocity {
            get => _velocity;
            set => _velocity = value;
        }

        float _direction = 0;
        public virtual float direction {
            get => _direction;
            set => _direction = value;
        }
        #endregion

        protected override void ApplyTransform(Transform transform) {
            base.ApplyTransform(transform);
            transform.localPosition = position.To3D(depth);
            transform.localRotation = Quaternion.Euler(0, 0, direction);
        } 
        
        public override void Serialize(IWriter writer) {
            base.Serialize(writer);
            if (!position.IsEmpty()) writer.Write("position", position);
            
            if (direction != 0) writer.Write("direction", direction);
        }

        public override void Deserialize(IReader reader) {
            base.Deserialize(reader);
            reader.Read("position", ref _position);
            reader.Read("direction", ref _direction);
        }
    }
    
    public interface IOnSetBodyHandler {
        void OnSetBody(GameEntity entity);
    }
}
