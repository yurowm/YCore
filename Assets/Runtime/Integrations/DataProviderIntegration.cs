using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Cysharp.Threading.Tasks;
using Yurowm.Extensions;
using Yurowm.Serialization;

namespace Yurowm.Integrations {
    public abstract class DataProviderIntegration : Integration {
        public override string GetIntegrationType() => $"Data,{base.GetIntegrationType()}";

        public abstract bool IsReady();
        
        public abstract bool GetData(string key, out float data);
        public abstract bool GetData(string key, out int data);
        public abstract bool GetData(string key, out bool data);
        public abstract bool GetData(string key, out string data);

        public async UniTask WaitReady() {
            while (!IsReady())
                await UniTask.Yield();
        }
        
        public class Data: ISerializable {
            public string ID;
            public Type type;
            public object value;
                
            public enum Type {
                Float = 0,
                Bool = 1,
                String = 2,
                Int = 3
            } 

            public void Serialize(IWriter writer) {
                writer.Write("ID", ID);
                writer.Write("dType", type);
                switch (type) {
                    case Type.Bool: writer.Write("value", value is true); break;
                    case Type.Float: writer.Write("value", value is float f ? f : 0f); break;
                    case Type.Int: writer.Write("value", value is int i ? i : 0); break;
                    case Type.String: writer.Write("value", value as string ?? string.Empty); break;
                }
            }

            public void Deserialize(IReader reader) {
                reader.Read("ID", ref ID);
                reader.Read("dType", ref type);
                switch (type) {
                    case Type.Bool: value = reader.Read<bool>("value"); break;
                    case Type.Float: value = reader.Read<float>("value"); break;
                    case Type.Int: value = reader.Read<int>("value"); break;
                    case Type.String: value = reader.Read<string>("value"); break;
                }
            }
        }
    }
    
    public class DataStorage: DataProviderIntegration {
        public List<Data> data = new ();

        public override string GetName() => "Local Data";

        public override bool IsReady() => true;
        
        public Data GetData(string ID) => data.FirstOrDefault(d => d.ID == ID);

        public override bool GetData(string key, out float data) {
            data = default;
            
            var defaultValue = GetData(key);
            if (defaultValue is { type: Data.Type.Float }) {
                data = (float) defaultValue.value;
                return true;
            }
            
            return false;
        }
        
        public override bool GetData(string key, out int data) {
            data = default;
            
            var defaultValue = GetData(key);
            if (defaultValue is { type: Data.Type.Int }) {
                data = (int) defaultValue.value;
                return true;
            }
            
            return false;
        }

        public override bool GetData(string key, out bool data) {
            data = default;
            
            var defaultValue = GetData(key);
            if (defaultValue is { type: Data.Type.Bool }) {
                data = (bool) defaultValue.value;
                return true;
            }
            
            return false;
        }

        public override bool GetData(string key, out string data) {
            data = default;
            
            var defaultValue = GetData(key);
            if (defaultValue is { type: Data.Type.String }) {
                data = (string) defaultValue.value;
                return true;
            }
            
            return false;
        }

        public override void Serialize(IWriter writer) {
            base.Serialize(writer);
            writer.Write("data", data);
        }

        public override void Deserialize(IReader reader) {
            base.Deserialize(reader);
            data.Reuse(reader.ReadCollection<Data>("data"));
        }
    }
}