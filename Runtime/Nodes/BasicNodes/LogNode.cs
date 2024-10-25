using System.Collections;
using System.Text;
using Yurowm.Extensions;
using Yurowm.Serialization;
using Yurowm.Utilities;

namespace Yurowm.Nodes {
    public class LogNode : BasicNode {
        
        public string message;
        
        Port inputPort = new Port(0, "Input", Port.Info.Input, Side.Top);
        
        readonly StringBuilder logBuilder = new StringBuilder();
        
        public override IEnumerable GetPorts() {
            yield return inputPort;
        }

        public override void OnPortPushed(Port sourcePort, Port targetPort, object[] args) {
            if (targetPort == inputPort) 
                Log(sourcePort, args);
        }

        void Log(Port sourcePort, object[] args) {
            #if UNITY_EDITOR
            
            logBuilder.Clear();
            if (!message.IsNullOrEmpty())
                logBuilder.AppendLine(message);
            
            logBuilder.AppendLine($"{sourcePort.node.ID}x{sourcePort.ID} ({sourcePort.node.GetTitle()}:{sourcePort.name}");
            
            if (args == null || args.Length == 0) 
                logBuilder.AppendLine("Void");
            else
                foreach (object arg in args)
                    logBuilder.AppendLine(arg == null ? "null" : $"{arg} ({arg.GetType().FullName})");
            
            UnityEngine.Debug.Log(logBuilder.ToString());
            
            #else
            UnityEngine.Debug.Log(message);
            #endif
        }

        public override void Serialize(IWriter writer) {
            base.Serialize(writer);
            if (!message.IsNullOrEmpty())
                writer.Write("message", message);
        }

        public override void Deserialize(IReader reader) {
            base.Deserialize(reader);
            reader.Read("message", ref message);
        }
    }
}