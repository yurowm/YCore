using System;
using System.Collections.Generic;

namespace Yurowm {
    public class SingleCallEvent {
        Queue<Action> queue = new Queue<Action>();

        public static SingleCallEvent operator +(SingleCallEvent singleCallEvent, Action action) {
            singleCallEvent.queue.Enqueue(action);
            return singleCallEvent;
        }

        public void Invoke() {
            while (queue.Count > 0) queue.Dequeue().Invoke();
        }
    }

    public class SingleCallEvent<A>  {
        Queue<Action<A>> queue = new Queue<Action<A>>();

        public static SingleCallEvent<A> operator +(SingleCallEvent<A> singleCallEvent, Action<A> action) {
            singleCallEvent.Add(action);
            return singleCallEvent;
        }

        public void Add(Action<A> action) {
            queue.Enqueue(action);
        }

        public void Invoke(A a) {
            while (queue.Count > 0) queue.Dequeue().Invoke(a);
        }
    }

    public class SingleCallEvent<A, B> {
        Queue<Action<A, B>> queue = new Queue<Action<A, B>>();

        public static SingleCallEvent<A, B> operator +(SingleCallEvent<A, B> singleCallEvent, Action<A, B> action) {
            singleCallEvent.queue.Enqueue(action);
            return singleCallEvent;
        }

        public void Invoke(A a, B b) {
            while (queue.Count > 0) queue.Dequeue().Invoke(a, b);
        }
    }

    public class SingleCallEvent<A, B, C> {
        Queue<Action<A, B, C>> queue = new Queue<Action<A, B, C>>();

        public static SingleCallEvent<A, B, C> operator +(SingleCallEvent<A, B, C> singleCallEvent, Action<A, B, C> action) {
            singleCallEvent.queue.Enqueue(action);
            return singleCallEvent;
        }

        public void Invoke(A a, B b, C c) {
            while (queue.Count > 0) queue.Dequeue().Invoke(a, b, c);
        }
    }

    public class SingleCallEvent<A, B, C, D> {
        Queue<Action<A, B, C, D>> queue = new Queue<Action<A, B, C, D>>();

        public static SingleCallEvent<A, B, C, D> operator +(SingleCallEvent<A, B, C, D> singleCallEvent, Action<A, B, C, D> action) {
            singleCallEvent.queue.Enqueue(action);
            return singleCallEvent;
        }

        public void Invoke(A a, B b, C c, D d) {
            while (queue.Count > 0) queue.Dequeue().Invoke(a, b, c, d);
        }
    }
}
