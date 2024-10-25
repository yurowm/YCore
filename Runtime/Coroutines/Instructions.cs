using System;
using System.Collections;
using System.Threading.Tasks;
using UnityEngine;

namespace Yurowm.Coroutines {
    public class Wait : IEnumerator {
        float end = 0;

        public Wait(float duration) {
            end = Time.time + duration;
        }

        public object Current => null;

        public bool MoveNext() {
            return Time.time < end;
        }

        public void Reset() { }
    }
    
    public class WaitTask : IEnumerator {
        readonly Task task;

        public WaitTask(Task task) {
            this.task = task;
        }

        public object Current => null;

        public bool MoveNext() {
            return !task.IsCompleted;
        }

        public void Reset() { }
    }
    
    public class SkipFrames : IEnumerator {
        int count;
        
        public SkipFrames(int count = 1) {
            this.count = count;
        }

        public object Current => null;

        public bool MoveNext() {
            return --count > 0;
        }

        public void Reset() { }
    }
    
    public class WaitRel : IEnumerator {
        float time = 0;

        public WaitRel(float duration) {
            time = Mathf.Max(duration, 0);
        }

        public object Current => null;

        public bool MoveNext() {
            time -= Time.deltaTime;
            return time >= 0;
        }

        public void Reset() { }
    }
    
    public class WaitTimeSpan : IEnumerator {
        DateTime endTime;

        public WaitTimeSpan(DateTime end) {
            endTime = end;
        }
        
        public WaitTimeSpan(TimeSpan span) : this(DateTime.Now + span) {}
        
        public WaitTimeSpan(float seconds) : this(TimeSpan.FromSeconds(seconds)) { }

        public object Current => null;

        public bool MoveNext() {
            return DateTime.Now < endTime;
        }

        public void Reset() { }
    }
}