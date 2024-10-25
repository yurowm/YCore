using System;
using System.Text;
using Yurowm.Utilities;    

namespace Yurowm.Nodes {
    public class Port {
        public Node node;
        public readonly int ID;
        
        [Flags]
        public enum Info {
            Input = 1 << 0,
            Output = 1 << 1,
            MultipleConnection = 1 << 2,
            DoubleSide = Input | Output
        }
        
        #if UNITY_EDITOR
        
        public readonly string name;
        public readonly string tooltip;
        public int orientation;
        
        readonly Side _side;
        public Side side => _side.Rotate(orientation * 2);
        public readonly Info info;
        
        string BuildToolTip() {
            StringBuilder builder = new StringBuilder();
            builder.AppendLine(name);
            builder.AppendLine($"({info})");
            return builder.ToString().Trim();
        }
        
        public static bool IsSuitable(Port portA, Port portB) {
            if (portA == portB) return false;
            
            if (portA.info.HasFlag(Info.Output) && portB.info.HasFlag(Info.Input)) return true;
            if (portB.info.HasFlag(Info.Output) && portA.info.HasFlag(Info.Input)) return true;
            
            return false;
        }

        #endif

        public Port(int ID, string name, Info info, Side side) {
            this.ID = ID;
            
            #if UNITY_EDITOR
            this.name = name;
            this.info = info;
            this._side = side;
            tooltip = BuildToolTip();
            #endif
        }
    }
}