using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using Yurowm.Extensions;
using Yurowm.Serialization;

namespace Yurowm.Nodes {
    public abstract class Node : ISerializable {
        public int ID;
        
        public NodeSystem system;
        public virtual string IconName => "Dot";

        public virtual void OnCreate() {}
        
        #if UNITY_EDITOR
        public Vector2 position;
        public float height = 20;
        public virtual float width => 200;
        
        public int orientation = 0;
        #endif
        
        public void SetOrientation(int o) {
            #if UNITY_EDITOR
            orientation = o;
            CollectPorts().ForEach(p => p.orientation = orientation);
            #endif
        }
        
        public Node() {
            CollectPorts().ForEach(p => p.node = this);
        }
        
        public virtual string GetTitle() {
            return GetType().Name.NameFormat("", "Node", true);
        }

        #region Ports
        
        public abstract IEnumerable GetPorts();
        
        public IEnumerable<Port> CollectPorts() {
            return GetPorts().Collect<Port>();
        }
        
        public virtual Port GetPortByID(int portID) {
            return CollectPorts().FirstOrDefault(p => p.ID == portID);
        }
        
        public void Push(Port port, params object[] args) {
            PushWithArgs(port, args);
        }
        
        public void PushWithArgs(Port port, object[] args) {
            system.connections
                .Where(c => c.Contains(port))
                .Select(c => c.GetAnother(port))
                .ForEach(p => p.node.OnPortPushed(port, p, args));
        }
        
        public IEnumerable<object> Pull(Port port) {
            var p = system.connections
                .Where(c => c.Contains(port))
                .Select(c => c.GetAnother(port))
                .FirstOrDefault();
            
            if (p != null)
                foreach (var arg in p.node.OnPortPulled(port, p))
                    yield return arg;
        }
        
        public IEnumerable<IEnumerable<object>> PullAll(Port port) {
            return system.connections
                .Where(c => c.Contains(port))
                .Select(c => c.GetAnother(port))
                .Select(p => p.node.OnPortPulled(port, p));
        }
        
        public bool Pull<T>(Port port, out T t) {
            t = default;
            
            try {
                var arg = Pull(port)?.FirstOrDefault();
                
                if (arg is T _t) {
                    t = _t;
                    return true;
                }
            } catch (Exception e) {
                Debug.LogException(e);
            }
            
            return false;
        }

        public virtual void OnPortPushed(Port sourcePort, Port targetPort, object[] args) {}
        public virtual IEnumerable<object> OnPortPulled(Port sourcePort, Port targetPort) => null;

        #endregion

        #region ISerializable
        
        public virtual void Serialize(IWriter writer) {
            writer.Write("ID", ID);
            
            #if UNITY_EDITOR
            writer.Write("position", position);
            writer.Write("height", height);
            if (orientation != 0)
                writer.Write("orientation", orientation);
            #endif
        }

        public virtual void Deserialize(IReader reader) {
            reader.Read("ID", ref ID);
            
            #if UNITY_EDITOR
            reader.Read("position", ref position);
            reader.Read("height", ref height);
            reader.Read("orientation", ref orientation);
            SetOrientation(orientation);
            #endif
        }
        
        #endregion
    }
}