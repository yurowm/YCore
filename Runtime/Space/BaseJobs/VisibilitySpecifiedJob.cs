using System.Collections.Generic;
using UnityEngine;
using Yurowm.Spaces;
using Yurowm.Utilities;
using Space = Yurowm.Spaces.Space;

namespace Yurowm.Jobs {
    public interface IVisibilitySpecified {
        bool isVisible {get;}
        void OnVisible();
        void OnInvisible();
        float GetVisibleSize();
    }
    
    public interface IVisibilitySpecifiedCamera {
        Vector2 position {get;}
        float viewSize {get;}
        
    }

    public class VisibilitySpecifiedJob : Job<IVisibilitySpecified>, ISpaceJob, ICatchJob, IUpdateJob {
        
        public Space space { get; set; }
        
        List<IVisibilitySpecified> visible = new List<IVisibilitySpecified>();

        public override bool IsSuitable(object subscriber) {
            if (subscriber is IVisibilitySpecified visibility)
                if (subscriber is SpacePhysicalItem physicalItem) {
                    if (camera != null)
                        SetVisibility(visibility, IsVisibleInReal(physicalItem, false));
                    return true;
                }
            
            return false;
        }

        public override void OnUnsubscribe(IVisibilitySpecified subscriber) {
            base.OnUnsubscribe(subscriber);
            visible.Remove(subscriber);
        }

        IVisibilitySpecifiedCamera camera;
        public void CatchInSpace(Space space) {
            context.Catch<GameEntity>(entity => {
                if (entity is IVisibilitySpecifiedCamera camera) {
                    this.camera = camera;
                    foreach (var subscriber in subscribers)
                        SetVisibility(subscriber, IsVisibleInReal(subscriber as SpacePhysicalItem, false));
                    return true;
                }
                return false;
            });
        }

        float camSize;
        bool isVisibleInMemory;
        DelayedAccess access = new DelayedAccess(1f / 15);
        public override void ToWork() {
            if (camera == null || subscribers.Count == 0 || !access.GetAccess())
                return;
            camSize = camera.viewSize;

            foreach (var subscriber in subscribers) {
                isVisibleInMemory = visible.Contains(subscriber);
                if (isVisibleInMemory != IsVisibleInReal(subscriber as SpacePhysicalItem, isVisibleInMemory))
                    SetVisibility(subscriber, !isVisibleInMemory);
            }
        }

        void SetVisibility(IVisibilitySpecified subscriber, bool value) {
            if (value) {
                visible.Add(subscriber);
                subscriber.OnVisible();
            } else {
                visible.Remove(subscriber);
                subscriber.OnInvisible();
            }
        }

        Vector2 offset;
        float distance;
        bool IsVisibleInReal(SpacePhysicalItem item, bool inMemory) {
            distance = (camSize + item.GetVisibleSize()) * (inMemory ? 1.2f : 1.1f);
            offset = item.position - camera.position;
            return Mathf.Abs(offset.x) < distance && Mathf.Abs(offset.y) < distance;
        }
    }
}