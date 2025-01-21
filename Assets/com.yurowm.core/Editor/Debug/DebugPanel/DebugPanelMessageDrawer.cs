using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using UnityEditor;
using UnityEngine;
using Yurowm.Extensions;
using Yurowm.GUIHelpers;
using Yurowm.Utilities;

namespace Yurowm.DebugTools {
    public abstract class MessageDrawer {
        
        static MessageDrawer[] all = Utils
            .FindInheritorTypes<MessageDrawer>(true, true)
            .Where(t => t.IsInstanceReadyType())
            .Select(Activator.CreateInstance)
            .Cast<MessageDrawer>()
            .ToArray();
        
        static Dictionary<Type, MessageDrawer> byType = new Dictionary<Type, MessageDrawer>();
        
        public abstract bool IsSuitableFor(Type messageType);

        public abstract void Draw(Rect rect, DebugPanel.IMessage message);

        public abstract void DrawFull(DebugPanel.IMessage message);
        
        public virtual void DoubleClick(DebugPanel.IMessage message) {}
        
        public static MessageDrawer Get(Type type) {
            if (!byType.TryGetValue(type, out var drawer)) {
                drawer = all.FirstOrDefault(d => d.IsSuitableFor(type));
                byType.Add(type, drawer);
            }
            
            return drawer;
        }

        public abstract bool IsEmpty(DebugPanel.IMessage message);
    }
    
    public abstract class MessageDrawer<T> : MessageDrawer where T : DebugPanel.IMessage {
        public override bool IsSuitableFor(Type messageType) {
            return typeof(T).IsAssignableFrom(messageType);
        }

        public sealed override void Draw(Rect rect, DebugPanel.IMessage message) {
            if (message is T t) Draw(rect, t);
        }

        public sealed override void DrawFull(DebugPanel.IMessage message) {
            if (message is T t) DrawFull(t);
        }

        public override bool IsEmpty(DebugPanel.IMessage message) {
            return message is not T t || IsEmpty(t);
        }

        protected abstract void Draw(Rect rect, T message);
        protected abstract void DrawFull(T message);
        protected abstract bool IsEmpty(T message);
    }
    
    public class OtherMessage : MessageDrawer<DebugPanel.OtherMessage> {
        protected override void Draw(Rect rect, DebugPanel.OtherMessage message) {
            using (GUIHelper.Color.Start(Color.white.Transparent(.5f))) 
                GUI.Label(rect, message.text);
        }
        
        protected override void DrawFull(DebugPanel.OtherMessage message) {
            EditorGUILayout.HelpBox(message.text, MessageType.None, true);
        }

        protected override bool IsEmpty(DebugPanel.OtherMessage message) {
            return message.text.IsNullOrEmpty();
        }

        public override void DoubleClick(DebugPanel.IMessage message) { }
    }
    
    public class TextMessageDrawer : MessageDrawer<DebugPanel.TextMessage> {
        protected override void Draw(Rect rect, DebugPanel.TextMessage message) {
            GUI.Label(rect, message.Value);
        }

        protected override void DrawFull(DebugPanel.TextMessage message) {
            EditorGUILayout.HelpBox(message.Value, MessageType.None, true);
        }

        protected override bool IsEmpty(DebugPanel.TextMessage message) {
            return message.Value.IsNullOrEmpty();
        }

        public override void DoubleClick(DebugPanel.IMessage message) { }
    }
    
    public class ActionMessageDrawer : MessageDrawer<DebugPanel.ActionMessage> {
        protected override void Draw(Rect rect, DebugPanel.ActionMessage message) {
            if (GUI.Button(rect, "Invoke", EditorStyles.miniButton)) {
                try {
                    message.Value.Invoke();
                } catch (Exception e) {
                    Debug.LogException(e);
                }
            }
        }

        protected override void DrawFull(DebugPanel.ActionMessage message) { }
        
        protected override bool IsEmpty(DebugPanel.ActionMessage message) => true;

        public override void DoubleClick(DebugPanel.IMessage message) {
            (message as DebugPanel.ActionMessage).Value.Invoke();
        }
    }
    
    public class UnityMessageDrawer : MessageDrawer<DebugPanelUnityLogHandler.Message> {
        GUIStyle style;
        GUIStyle stackTraceStyle;
        
        public UnityMessageDrawer() {
            EditorGUI.hyperLinkClicked += HyperLinkClicked;
        }
        
        protected override void Draw(Rect rect, DebugPanelUnityLogHandler.Message message) {
            if (style == null) {
                style = new GUIStyle(EditorStyles.label);
                style.alignment = TextAnchor.UpperLeft;
            }

            if (message.value.count > 1)
                rect = ItemIconDrawer.DrawTag(rect, message.value.count.ToString(), GUI.contentColor);

            GUI.Label(rect, message.value.message, style);
        }

        protected override void DrawFull(DebugPanelUnityLogHandler.Message message) {
            if (stackTraceStyle == null) 
                stackTraceStyle = "CN Message";

            EditorGUILayout.TextArea(message.value.message.Bold(), stackTraceStyle,
                GUILayout.ExpandWidth(true));

            EditorGUILayout.TextArea(Highlight(message.value.stackTrace), stackTraceStyle,
                GUILayout.ExpandWidth(true));
        }

        protected override bool IsEmpty(DebugPanelUnityLogHandler.Message message) {
            return false;
        }

        public override void DoubleClick(DebugPanel.IMessage message) {
            if (message is DebugPanelUnityLogHandler.Message m) {
                var stackTrace = m.value.stackTrace;

                var match = fileNameRegex.Match(stackTrace);
                
                if (match.Success)
                    Open(match.Groups["path"].Value, int.Parse(match.Groups["position"].Value));
            }
        }
        
        void Open(string path, int position) {
            if (path.IsNullOrEmpty()) 
                return;
            
            #if UNITY_EDITOR
            UnityEditorInternal.InternalEditorUtility.OpenFileAtLineExternal(path, position, 0);
            #endif
        }

        static Regex fileNameRegex = new Regex(@"\(at (?<link>(?<path>.*\.cs):(?<position>\d+))\)");
        
        static StringBuilder builder = new StringBuilder();
        
        static Dictionary<string, string> hyperLinkData = new Dictionary<string, string>();
        
        static string Highlight(string original) {
            string Replace(Match match) {
                hyperLinkData.Clear();
                hyperLinkData["path"] = match.Groups["path"].Value;
                hyperLinkData["position"] = match.Groups["position"].Value;
                
                var link = match.Groups["link"].Value;
                
                return match.Value.Replace(link, link.HyperLink(hyperLinkData));
            }
            
            return original.Replace(fileNameRegex, Replace);
        }
        
        void HyperLinkClicked(EditorWindow window, HyperLinkClickedEventArgs args) {
            if (args.hyperLinkData.TryGetValue("path", out var path) && 
                args.hyperLinkData.TryGetValue("position", out var p)
                && int.TryParse(p, out var position))
                Open(path, position);
        }
    }
}