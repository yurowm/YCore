using System.Collections;
using System.Collections.Generic;
using Cysharp.Threading.Tasks;
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
            Update().Forget();
        }
        
        Queue<IMessage> queue = new();
        
        async UniTask Update() {
            while (true) {
                while (queue.Count > 0) {
                    LogMessage(queue.Dequeue());
                }
                
                await UniTask.Yield();
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