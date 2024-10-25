using System;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEngine;
using Yurowm.Extensions;
using Yurowm.GUIHelpers;

namespace Yurowm.HierarchyLists {
    public class YList<T> {
        
        List<T> selected = new List<T>();
        
        public float elementHeight = 16;    
        public bool allowToRemove = true;
        
        float borderSize = 3;
        
        public Func<T, string> getName = null;
        public Func<T, Texture2D> getIcon = null;
        public Func<Rect, T, Rect> drawIcon = null;
        public Dictionary<string, Func<T>> getNewOptions = null;
        
        public Action<GenericMenu, T> onElementContextMenu = null;
        public Action<GenericMenu, List<T>> onContextMenu = null;
        public Action<IEnumerable<T>> onChangeSelection = null;
        
        bool changeReqest;
        
        float scrollPos = 0;

        public void SetDirty() {
            changeReqest = true;
        }
        
        public IEnumerable<T> GetSelected(List<T> elements) {
            return elements.Where(selected.Contains);
        }
        
        #region OnGUI

        public void OnGUI(Rect rect, List<T> elements) {
            if (changeReqest) {
                changeReqest = false;
                GUI.changed = true;
            }
                
            if (Event.current.type == EventType.Layout)
                return;
            
            bool contextEvent = Event.current.type == EventType.MouseDown && Event.current.button == 1;
            bool clickEvent = Event.current.type == EventType.MouseDown && Event.current.button == 0;
            
            if (Event.current.type == EventType.Repaint)
                EditorStyles.textArea.Draw(rect, false, true, false, false);

            rect = rect.GrowSize(-2f * borderSize);

            bool scrolling = rect.height < elements.Count * elementHeight;
            
            if (elements.Count > 0) {
                
                int firstIndex = 0;
                int lastIndex = elements.Count - 1;
                
                if (scrolling) {
                    const float scrollWidth = 16;
                    
                    rect.xMax -= scrollWidth;
                    
                    Rect scrollRect = rect;
                    scrollRect.x = rect.xMax;
                    scrollRect.width = scrollWidth;

                    scrollPos = GUI.VerticalScrollbar(scrollRect, scrollPos, rect.height, 0, elements.Count * elementHeight);
                    firstIndex = Mathf.Max(0, Mathf.FloorToInt(scrollPos / elementHeight));
                    lastIndex = Mathf.Min(lastIndex, firstIndex + Mathf.CeilToInt(rect.height / elementHeight));
                } else
                    scrollPos = 0;

                using (GUIHelper.Clip.Start(rect, out var rectCliped)) {
                    
                    for (int index = firstIndex; index <= lastIndex; index++) {
                        Rect elementRect = rectCliped;
                        elementRect.height = elementHeight;
                        elementRect.y += elementHeight * index - scrollPos;
                        
                        if (elementRect.yMin > rectCliped.yMax) break;
                        
                        var element = elements[index];
                        
                        DrawElement(elementRect, element);
                        
                        if (elementRect.Contains(Event.current.mousePosition)) {
                            if (clickEvent) {
                                clickEvent = false;
                                if (!Event.current.control)
                                    selected.RemoveAll(elements.Contains);
                                selected.Add(element);
                                onChangeSelection?.Invoke(GetSelected(elements));
                                GUI.changed = true;
                            }
                            if (contextEvent) {
                                contextEvent = false;
                                if (!selected.Contains(element)) {
                                    selected.RemoveAll(elements.Contains);
                                    selected.Add(element);
                                    onChangeSelection?.Invoke(GetSelected(elements));
                                    GUI.changed = true;
                                }
                                GenericMenu menu = new GenericMenu();
                                OnElementContextMenu(menu, elements, element);
                                OnContextMenu(menu, elements);
                                if (menu.GetItemCount() > 0)
                                    menu.ShowAsContext();
                            }
                        }
                    }
                }
            }
            
            if (rect.Contains(Event.current.mousePosition)) {
                
                if (clickEvent) {
                    selected.RemoveAll(elements.Contains);
                    onChangeSelection?.Invoke(new T[0]);
                    GUI.changed = true;
                }
                
                if (contextEvent) {
                    GenericMenu menu = new GenericMenu();
                    OnContextMenu(menu, elements);
                    if (menu.GetItemCount() > 0)
                        menu.ShowAsContext();
                }
            }
            
        }

        void OnElementContextMenu(GenericMenu menu, List<T> list, T element) {
            if (allowToRemove)
                menu.AddItem(new GUIContent("Remove"), false, () => {
                    var allSelected = selected.Where(list.Contains).ToArray();
                    if (allSelected.Length > 0)
                        list.RemoveAll(allSelected.Contains);
                    else
                        list.Remove(element);
                    SetDirty();
                });
            
            onElementContextMenu?.Invoke(menu, element);
        }

        void OnContextMenu(GenericMenu menu, List<T> list) {
            if (getNewOptions != null) {
                foreach (var option in getNewOptions) {
                    var func = option.Value;
                    menu.AddItem(new GUIContent($"New/{option.Key}"), false, () => {
                        list.Add(func.Invoke());
                        SetDirty();
                    });
                }
            }
            
            onContextMenu?.Invoke(menu, list);
        }

        public void OnGUILayout(string label, List<T> elements, params GUILayoutOption[] layoutOptions) {
            var height = elementHeight * (elements.Count + 1) + borderSize * 2;
            OnGUI(EditorGUILayout.GetControlRect(true, height, layoutOptions), label, elements);
        }
    
        public void OnGUILayout(GUIContent label, List<T> elements, params GUILayoutOption[] layoutOptions) {
            var height = elementHeight * (elements.Count + 1) + borderSize * 2;
            OnGUI(EditorGUILayout.GetControlRect(true, height, layoutOptions), label, elements);
        }
        
        public void OnGUILayout(List<T> elements, params GUILayoutOption[] layoutOptions) {
            var height = elementHeight * (elements.Count + 1) + borderSize * 2;
            OnGUI(EditorGUILayout.GetControlRect(false, height, layoutOptions), elements);
        }

        public void OnGUI(Rect rect, string label, List<T> elements) {
            OnGUI(rect, new GUIContent(label), elements);
        }
        
        public void OnGUI(Rect rect, GUIContent label, List<T> elements) {
            OnGUI(EditorGUI.PrefixLabel(rect, label), elements);
        }
        
        readonly static Color selectedColor = new Color(0f, 0.42f, 1f, 0.23f);

        void DrawElement(Rect rect, T element) {
            if (selected.Contains(element))
                Handles.DrawSolidRectangleWithOutline(rect, selectedColor, Color.clear);
            
            if (drawIcon == null)
                rect = DrawIcon(rect, getIcon?.Invoke(element));
            else {
                if (Event.current.type == EventType.Repaint) 
                   rect = drawIcon.Invoke(rect, element);
            }
            
            GUI.Label(rect, getName == null ? element.ToString() : getName.Invoke(element));
        }
        
        Rect DrawIcon(Rect rect, Texture2D icon) {
            if (icon == null) return rect;
            float ratio = 1f * icon.width / icon.height;
            float width = ratio * rect.height;
            
            if (Event.current.type == EventType.Repaint) {
                Rect iconRect = new Rect(rect.x, rect.y, width, rect.height);
                GUI.DrawTexture(iconRect, icon, ScaleMode.StretchToFill, true,
                    ratio, Color.white, 0, 0);
            }
            
            rect.x += width;
            rect.width -= width;
            return rect;
        }
        
        #endregion
    }
}
