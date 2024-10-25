using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using Yurowm.Extensions;

namespace Yurowm.DebugTools {
    public static class DebugPanelUnityLogHandler {
        
        [RuntimeInitializeOnLoadMethod]
        static void Initialize() {
            void HandleLog(string message, string stackTrace, LogType type) {
                var unityMessage = GetMessage(message, stackTrace, type);
                
                switch (type) {
                    case LogType.Exception: DebugPanel.Log(unityMessage.key, "Exception", unityMessage); return;
                    case LogType.Error: DebugPanel.Log(unityMessage.key, "Error", unityMessage); return;
                    case LogType.Warning: DebugPanel.Log(unityMessage.key, "Warning", unityMessage); return;
                    default: DebugPanel.Log(unityMessage.key, "Log", unityMessage); break;
                }
            }
			
            Application.logMessageReceived += HandleLog;
        }
        
        static Dictionary<int, UnityLogMessage> messages = new Dictionary<int,UnityLogMessage>();
        
        static UnityLogMessage GetMessage(string message, string stackTrace, LogType logType) {
            var code = UnityLogMessage.GetHashCode(message, stackTrace, logType);
            
            if (!messages.TryGetValue(code, out var unityMessage)) {
                unityMessage = new UnityLogMessage(message, stackTrace, logType);
                messages.Add(code, unityMessage);
            }
            
            unityMessage.count ++;
            
            return unityMessage;
        }
        
        public class Message : DebugPanel.IMessage {
            public UnityLogMessage value;
            
            public Message() {}
            
            Message(UnityLogMessage value) {
                this.value = value;    
            }

            public int CastPriority => 1;
            
            public DebugPanel.IMessage TryToEmitFor(object value) {
                if (value is UnityLogMessage um)
                    return new Message(um);
                return null;
            }

            public bool Update(object obj) {
                return obj == value;
            }

            public bool IsExtendable() {
                return value.Length > 20;
            }
        }
        
        public class UnityLogMessage {
            readonly int hashCode;
            public readonly string message;
            public readonly string stackTrace;
            public readonly LogType logType;
            
            public string key => $"~{logType} ({hashCode})";
            
            public int Length => message.Length + stackTrace.Length;

            public int count = 0;
            
            public UnityLogMessage(string message, string stackTrace, LogType logType) {
                this.message = message;
                this.stackTrace = stackTrace;
                this.logType = logType;
                hashCode = GetHashCode(message, stackTrace, logType);
            }
            
            public static int GetHashCode(string message, string stackTrace, LogType logType) {
                unchecked {
                    var result = message?.GetHashCode() ?? 0;
                    result = (result * 397) ^ logType.GetHashCode();
                    result = (result * 397) ^ (stackTrace?.GetHashCode() ?? 0);
                    return result;
                }
            }

            public override int GetHashCode() {
                return hashCode;
            }
        }
   
        public class Builder : MessageUIBuilder {
            
            readonly Color newLineColor = new Color(1f, 0.02f, 0f);
            readonly Color evenLineColor = new Color(1f, 0.54f, 0.54f);
            readonly Color oddLineColor = new Color(1f, 0.93f, 0.51f);
            
            public override bool IsSuitableFor(Type messageType) {
                return messageType == typeof(DebugPanelUnityLogHandler.Message);
            }

            protected override MessageUI EmitMessageUI(DebugPanel.Entry entry) {
                var messageUI = debugPanelUI.EmitMessageUI("TextMessageUI");
                
                if (messageUI.SetupComponent(out Text textUI)) {
                    textUI.color = extendMode ? 
                        Color.white :
                        DebugPanel.GroupToColor(entry.@group);
                    
                    var text = "";
                    
                    if (entry.message is Message m) {
                        var value = m.value;
                        if (extendMode) {
                            text = value.message.Bold().Colorize(Color.yellow) + 
                                "\n\n" +
                                value.stackTrace
                                .Replace(@"[\w\d_-]*\.cs:\d+",
                                    s => s.Colorize(Color.cyan));
                        } else
                            text = value.message;
                    }
                    
                    textUI.text = text.Trim(); 
                }
                
                return messageUI;
            }
        }
    }
    
}