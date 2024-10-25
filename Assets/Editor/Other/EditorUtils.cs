using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEngine;
using Yurowm.Extensions;
using Yurowm.GUIHelpers;
using Yurowm.GUIStyles;
using Yurowm.Icons;
using Yurowm.Utilities;

namespace Yurowm {
    public static class EUtils {
        public static void DrawMixedProperty<C, V>(IEnumerable<C> contents, 
            Func<C, V> getValue, Action<C, V> setValue,
            Func<C, V, V> drawSingle,
            Action<V, Action<V>> drawMultiple = null,
            Action drawEmpty = null,
            Func<C, bool> mask = null) {
       
            bool multiple = false;
            bool assigned = false;
            V value = default;
            V temp = default;
            C lastContent = default;

            foreach (C content in contents) {
                if (mask != null && !mask.Invoke(content)) continue;
                if (!assigned) {
                    value = getValue.Invoke(content);
                    lastContent = content;
                    assigned = true;
                    continue;
                }
            
                temp = getValue.Invoke(content);
            
                if (!value.Equals(temp)) {
                    multiple = true;
                    break;
                }
            }

            if (!assigned) {
                drawEmpty?.Invoke();
                return;
            }

            if (multiple) {
                EditorGUI.showMixedValue = true;
                
                void SetSingleValue(V singleValue) {
                    value = singleValue;
                    multiple = false;
                }
                
                if (drawMultiple == null) { 
                    EditorGUI.BeginChangeCheck();
                    temp = drawSingle(lastContent, temp);
                    if (EditorGUI.EndChangeCheck()) 
                        SetSingleValue(temp);
                } else
                    drawMultiple(temp, SetSingleValue);
                
                EditorGUI.showMixedValue = false;
            } else
                value = drawSingle(lastContent, value);

            if (!multiple)
                foreach (C content in contents)
                    if (mask == null || mask.Invoke(content))
                        setValue(content, value);
        }
    
        public static List<FileInfo> SearchFiles(string directory) {
            List<FileInfo> result = new List<FileInfo>();
            result.AddRange(new DirectoryInfo(directory).GetFiles().ToList());
            foreach (DirectoryInfo dir in new DirectoryInfo(directory).GetDirectories())
                result.AddRange(SearchFiles(dir.FullName));
            return result;
        }

        public static string BytesToString(long byteCount) {
            string[] suf = { "B", "KB", "MB", "GB", "TB", "PB", "EB" };
            if (byteCount == 0)
                return "0" + suf[0];
            long bytes = Math.Abs(byteCount);
            int place = Convert.ToInt32(Math.Floor(Math.Log(bytes, 1024)));
            double num = Math.Round(bytes / Math.Pow(1024, place), 1);
            return (Math.Sign(byteCount) * num).ToString(CultureInfo.InvariantCulture) + suf[place];
        }

        public static IEnumerable<FileInfo> ProjectFiles(DirectoryInfo directory) {
            foreach (FileInfo file in directory.GetFiles())
                yield return file;
            foreach (DirectoryInfo subDirectory in directory.GetDirectories())
            foreach (FileInfo file in ProjectFiles(subDirectory))        
                yield return file;
        }
    
        public static IEnumerable<FileInfo> ProjectFiles(string directory) {
            return ProjectFiles(new DirectoryInfo(directory));
        }
    }
    
    public static class ItemIconDrawer {
        static readonly YRandom random = new YRandom(27);
        
        static Texture2D texture;
        static GUIStyle labelStyle;
        static GUIStyle tagStyle;

        static float ratio;
        static Dictionary<Type, Color> colors = new Dictionary<Type, Color>();
                
        static Color GetAutoIconColor(object obj) {
            var type = GetType(obj);
            if (!colors.ContainsKey(type)) {
                var code = type.FullName.GetHashCode().ToString();
                colors.Add(type, GetUniqueColor(code));
            }
            return colors[type];
        }

        public static Rect DrawAuto(Rect rect, object obj, Side side = Side.Left, float height = -1) {
            return DrawAuto(rect,
                GetType(obj).Name.Substring(0, 1).ToUpper(),
                GetAutoIconColor(obj),
                side,
                height);
        }
        
        static Type GetType(object obj) {
            if (obj is Type t)
                return t;
            return obj.GetType();
        }

        public static Rect DrawAuto(Rect rect, string symbol, Color color, Side side = Side.Left, float height = -1) {
            if (texture == null) {
                texture = EditorIcons.GetIcon("NodeIcon");
                ratio = 1f * texture.width / texture.height;
            }
            
            if (labelStyle == null) {
                labelStyle = new GUIStyle(Styles.tagLabelBlack);
                labelStyle.fontSize = 12;
                labelStyle.fontStyle = FontStyle.Normal;
            }
            
            if (height < 0)
                height = rect.height;
            
            float width = ratio * height;
            
            if (Event.current.type == EventType.Repaint) {
                Rect iconRect = IconRect(rect, side, new Vector2(width, height), out rect);
                
                GUI.DrawTexture(iconRect, texture, ScaleMode.StretchToFill, true, ratio,
                    color, 0, 0);
                
                GUI.Label(iconRect, symbol.ToString(), labelStyle);

            }

            return rect;
        }
        
        public enum Side {
            Left,
            Right
        }
        
        static Rect IconRect(Rect rect, Side side, Vector2 size, out Rect rest) {
            switch (side) {
                case Side.Left: {
                    rest = rect;
                    rest.x += size.x;
                    rest.width -= size.x;

                    return new Rect(rect.x, rect.y, size.x, size.y);
                }
                case Side.Right: {
                    rest = rect;
                    rest.width -= size.x;

                    return new Rect(rect.x + rest.width, rect.y, size.x, size.y);
                }
            }
            
            rest = rect;
            return rect;
        }
        
        public static Rect DrawSolid(Rect rect, Texture2D icon, Side side = Side.Left, float height = -1) {
            return Draw(rect, icon, GUIHelper.GUIColor.GetProLiteColor(), side, height);
        }
        
        public static Rect Draw(Rect rect, Texture2D icon, Side side = Side.Left, float height = -1) {
            return Draw(rect, icon, Color.white, side, height);
        }
        
        public static Rect Draw(Rect rect, Texture2D icon, Color color, Side side = Side.Left, float height = -1) {
            if (icon == null) return rect;
            float iconRatio = 1f * icon.width / icon.height;
            if (height < 0)
                height = rect.height;
            float width = iconRatio * height;
            
            if (Event.current.type == EventType.Repaint) {
                Rect iconRect = IconRect(rect, side, new Vector2(width, height), out rect);
                
                GUI.DrawTexture(iconRect, icon, ScaleMode.StretchToFill, true, iconRatio,
                    color, 0, 0);
            }
            
            return rect;
        }

        public static Rect DrawTag(Rect rect, string tag, Side side = Side.Left) {
            return DrawTag(rect, tag, GetUniqueColor(tag), side);
        }
        
        public static Rect DrawTag(Rect rect, string tag, Color color, Side side = Side.Left) {
            if (tag.IsNullOrEmpty())
                return rect;
            
            if (tagStyle == null) {
                tagStyle = new GUIStyle(Styles.miniLabel);
                tagStyle.padding = new RectOffset(2, 2, 2, 2);
                tagStyle.normal.textColor = Color.black;
                tagStyle.hover = tagStyle.normal;
                tagStyle.fontStyle = FontStyle.Bold;
            }
            
            if (Event.current.type != EventType.Repaint) 
                return rect;
            
            if (tag.IsNullOrEmpty())
                return rect;

            var content = new GUIContent(tag);
            
            var size = tagStyle.CalcSize(content);
            
            var iconRect = IconRect(rect, side, size, out rect);
            
            iconRect.center = iconRect.center.ChangeY(rect.center.y);
            
            GUIHelper.DrawRect(iconRect.GrowSize(-2, -4), color);
            
            GUI.Label(iconRect, tag, tagStyle);
            
            return rect;
        }

        public static Color GetUniqueColor(object value) {
            return new HSBColor(random.Value(value.GetHashCode()), 0.5f, 1f).ToColor();
        }
    }

    public class ContextMenu {
        GenericMenu menu = new GenericMenu();
        
        public void Show() {
            if (menu.GetItemCount() > 0)
                menu.ShowAsContext();
        }
        
        public void Add(string path, Action action, bool selected = false) {
            menu.AddItem(new GUIContent(path), selected, action.Invoke);
        }
    }
}