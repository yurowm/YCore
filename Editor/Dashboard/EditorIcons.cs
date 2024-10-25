using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

namespace Yurowm.Icons {
    public static class EditorIcons {
        static Dictionary<string, Texture2D> icons = new Dictionary<string, Texture2D>();

        public static Texture2D GetIcon(string name) {
            if (!icons.ContainsKey(name))
                icons.Add(name, null);
            if (icons[name] == null)
                icons[name] = FindIcon(name);
            return icons[name];
        }
        
        public static Texture2D GetUnityIcon(string name) {
            if (!icons.ContainsKey(name))
                icons.Add(name, null);
            if (icons[name] == null)
                icons[name] = FindUnityIcon(name);
            return icons[name];
        }
        
        public static Texture2D GetUnityIcon(string lightName, string darkName) {
            if (EditorGUIUtility.isProSkin)
                return GetUnityIcon(darkName);
            else
                return GetUnityIcon(lightName);
        }

        static Texture2D FindIcon(string name) {
            return EditorGUIUtility.Load($"Icons/{name}.png") as Texture2D
                   ?? Resources.Load<Texture2D>($"Icons/{name}");
        }

        static Texture2D FindUnityIcon(string name) {
            return EditorGUIUtility.FindTexture(name);
        }
    }
}