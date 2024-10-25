using System;
using UnityEngine;
using Yurowm.DebugTools;
using Yurowm.Extensions;
using Yurowm.Jobs;

namespace Yurowm.Spaces {
    public class SpaceLinkerJob : Job<SpacePhysicalItem>, ISpaceJob, IUpdateJob {
        public Space space { get; set; }
        
        public override int GetPriority() {
            return -1000;
        }

        public override void ToWork() {
            bool vacuum = false;
            var delta = space.time.Delta;
            foreach (var s in subscribers) {
                if (!s.body) {
                    vacuum = true;
                    continue;
                }
                if (!s.velocity.IsEmpty())
                    s.position += s.velocity * delta;
                if (s.position.x == Single.NaN || s.position.y == Single.NaN)
                    s.position = default;
                s.body.transform.localPosition = s.position.To3D(s.depth);
                if (s.body.transform.localEulerAngles.z != s.direction) {
                    s.body.transform.localEulerAngles = new Vector3(0, 0, s.direction);
                    s.direction = s.body.transform.localEulerAngles.z;
                }
            }
            DebugPanel.Log("SLJ Subscrivers", "Session", subscribers.Count);
            if (vacuum) 
                subscribers.RemoveAll(s => !s.body);
        }
    }
}
