using System.Collections;
using System.Collections.Generic;
using Yurowm.Coroutines;
using Yurowm.Utilities;

namespace Yurowm.YDebug {
    public class UnityLog : ILogger {
        
        #if UNITY_EDITOR
        [UnityEditor.InitializeOnLoadMethod]
        static void OnLaunch() {
            Log.AddLogger(new UnityLog());
        }
        #endif
        
        public UnityLog() {
            Update().Run();
        }
        
        Queue<IMessage> queue = new Queue<IMessage>();
        
        IEnumerator Update() {
            while (true) {
                while (queue.Count > 0) {
                    LogMessage(queue.Dequeue());
                }
                
                yield return null;
            }
        }
        
        public void OnLogMessage(IMessage message) {
            if (Utils.IsMainThread())
                LogMessage(message);
            else
                queue.Enqueue(message);
        }

        public void OnStart() { }

        public void OnStop() { }

        void LogMessage(IMessage message) {
            switch (message) {
                case ErrorMessage m: UnityEngine.Debug.LogError(m.head); return;
                case ExceptionMessage m: UnityEngine.Debug.LogException(m.exception); return;
                default: UnityEngine.Debug.Log(message.head); return;
            }
        } 
    }
}