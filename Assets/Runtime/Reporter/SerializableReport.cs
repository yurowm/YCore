using System;
using UnityEngine;
using Yurowm.Serialization;

namespace Yurowm.InUnityReporting {
    public class SerializableReport : Report {
        ISerializable target;
        Reader.Entry entry;
        
        public Reader.Entry GetEntry() {
            return entry;
        }
        
        public SerializableReport(ISerializable serializable) {
            target = serializable;
        }
        
        public override bool Refresh() {
            try {
                string raw = Serializator.ToTextData(target);
                entry = Reader.Parse(raw);
                return true;
            } catch (Exception e) {
                Debug.LogException(e);
            }
            
            return false;
        }

        public override bool OnGUI(params GUILayoutOption[] layoutOptions) {
            return false;
        }

        public override string GetTextReport() {
            return "";
        }
        
        public static void Add(string name, ISerializable serializable) {
            Reporter.AddReport(name, new SerializableReport(serializable));
        }
    }
}
