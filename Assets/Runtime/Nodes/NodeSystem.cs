using System;
using System.Collections.Generic;
using System.Linq;
using Yurowm.Serialization;
using Yurowm.Utilities;

namespace Yurowm.Nodes {
    public abstract class NodeSystem : ISerializable {
        
        public List<Node> nodes = new List<Node>();
        public List<Pair<Port>> connections = new List<Pair<Port>>();
        
        public virtual IEnumerable<Type> GetSupportedNodeTypes() {
            yield return typeof(Node);
        }

        #region ISerializable

        public virtual void Serialize(IWriter writer) {
            writer.Write("nodes", nodes.ToArray());
            
            writer.Write("connections", connections
                .Select(p => $"{new int2(p.a.node.ID, p.a.ID)}-{new int2(p.b.node.ID, p.b.ID)}")
                .ToArray());

        }

        public virtual void Deserialize(IReader reader) {
            nodes.Clear();
            nodes.AddRange(reader.ReadCollection<Node>("nodes"));
            
            nodes.ForEach(n => n.system = this);
            
            connections.Clear();

            foreach (var connection in reader.ReadCollection<string>("connections")) {
                var splitted = connection.Split('-');
                int2 coordA = int2.Parse(splitted[0]);
                int2 coordB = int2.Parse(splitted[1]);
                var portA = nodes.FirstOrDefault(n => n.ID == coordA.X)?.GetPortByID(coordA.Y);
                var portB = nodes.FirstOrDefault(n => n.ID == coordB.X)?.GetPortByID(coordB.Y);
                if (portA != null && portB != null)
                    connections.Add(new Pair<Port>(portA, portB));
            }
        }

        #endregion
    }
}