using UnityEngine;
using Yurowm.ContentManager;
using Yurowm.Extensions;

namespace Yurowm.Spaces {
    public abstract class Room: GameEntity {
        
        public readonly LiveContext roomContext;
        public Transform root;
        
        public Room() {
            roomContext = new (GetType().Name);
            roomContext.SetArgument(this);
        }

        public override void OnAddToSpace(Space space) {
            base.OnAddToSpace(space);
            roomContext.SetArgument(space);
            
            root = new GameObject(GetType().Name).transform;
            root.SetParent(space.root);
            root.Reset();
            root.position = Vector3.zero;
            root.localScale = Vector3.zero;
        }
        
        public override void OnRemoveFromSpace(Space space) {
            base.OnRemoveFromSpace(space);
            roomContext.Destroy();
            root?.Destroy();
        }

        #region Transform
        
        public RoomTransform transform { get; private set; }

        public void SetTransform(RoomTransform rt) {
            rt.Apply(root);
            transform = rt;
            root.localPosition = root.localPosition.ChangeZ(1);
        }
        
        protected abstract Rect GetRoomRect();

        public struct RoomTransform {
            public Vector2 position;
            public float scale;

            public void Apply(Transform transform) {
                transform.position = position;
                transform.localScale = new Vector3(scale, scale, 1);
            }
        }
        
        #endregion

        #region Content
        
        public void Add(GameEntity content) {
            if (!roomContext.Add(content)) return;

            if (content is IRoomEntity re)
                re.room = this;
            
            content.space = space;
            content.OnAddToSpace(space);
            space.onAddItem.Invoke(content);
            
            if (content is SpacePhysicalItemBase spib) {
                AddBody(spib.body);
                spib.ApplyTransform();
            }
        }
        
        public void Remove(GameEntity content) {
            if (content.space != space) return;
            
            content.enabled = false;
            content.OnRemoveFromSpace(space);
            roomContext.Remove(content);
            content.space = null;
            if (content is IRoomEntity re)
                re.room = null;
        }
        
        public void AddBody(SpaceObject body) {
            var transform = body?.transform;

            if (!transform) return;

            transform.SetParent(root);
            transform.Reset();
        }
        
        #endregion
    }
    
    public interface IRoomEntity {
        Room room {get; set;}
    }
}