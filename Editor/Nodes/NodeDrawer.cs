using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEngine;
using Yurowm.Extensions;
using Yurowm.GUIHelpers;
using Yurowm.GUIStyles;
using Yurowm.Icons;
using Yurowm.ObjectEditors;
using Yurowm.Serialization;
using Yurowm.Utilities;

namespace Yurowm.Nodes.Editor {
    public class NodeDrawer {
        public bool visible = true;
        public Rect rect;
        public Vector2 sizeMin;
        public readonly Node node;
        
        NodeSystemEditor editor;

        public NodeDrawer(Node node, NodeSystemEditor editor) {
            this.node = node;
            this.editor = editor;
            
            rect = new Rect(node.position, new Vector2(node.width, node.height));
        }

        static readonly Color focusedColor = new Color(1f, 0.85f, 0.53f);
        
        public void Draw() {
            var fixedRect = GUI.Window(node.ID, rect, DrawWindow, GUIContent.none, GUIStyle.none);
            
            fixedRect.width = fixedRect.width.ClampMin(sizeMin.x);
            fixedRect.height = fixedRect.height.ClampMin(sizeMin.y);
            
            if (fixedRect != rect) { 
                rect = editor.grid.Snap(fixedRect);
                
                var delta = rect.position - node.position;
                
                node.position = rect.position;
                node.height = rect.height;

                if (!delta.IsEmpty())
                    foreach (var focusedNode in editor.focusedNodes)
                        if (focusedNode != this) {
                            focusedNode.rect.position += delta; 
                            focusedNode.node.position = focusedNode.rect.position;
                        }

                editor.SetDirty();
            }
        }
        
        static Texture2D gearIcon;

        static readonly Color backgroundColor = EditorGUIUtility.isProSkin ? 
            new Color(.15f, .15f, .15f) : new Color(.9f, .9f, .9f); 
        
        void DrawWindow(int ID) {
            GUIHelper.DrawRect(new Rect(default, rect.size), backgroundColor);
            
            var titleRect = EditorGUILayout.GetControlRect(true, EditorStyles.boldLabel.lineHeight, GUILayout.ExpandWidth(true));
            
            if (!node.IconName.IsNullOrEmpty())
                titleRect = ItemIconDrawer.DrawSolid(titleRect, EditorIcons.GetIcon(node.IconName));
            
            GUI.Label(titleRect, node.GetTitle(), EditorStyles.boldLabel);
            
            using (GUIHelper.ContentColor.ProLiteStart()) {
                var gearRect = GUILayoutUtility.GetLastRect();
                gearRect.x = gearRect.xMax - gearRect.height;
                gearRect.width = gearRect.height;
                
                if (gearIcon == null)
                    gearIcon = EditorIcons.GetIcon("SmallGear");
                
                if (GUI.Button(gearRect, gearIcon, Styles.centeredMiniLabel)) {
                    var menu = new GenericMenu();
                    ShowContextMenu(menu);
                    editor.OnNodeContextMenu(node, menu);
                    if (menu.GetItemCount() > 0)
                        menu.ShowAsContext();
                }
            }
            
            NodeEditor.Edit(node, NodeEditor.Place.Node, editor, rect.width * .4f); 
            
            GUILayout.Space(2);
            
            if (Event.current.type == EventType.Repaint) {
                var height = GUILayoutUtility.GetLastRect().yMax;
                if (height != rect.height) {
                    rect.height = height;
                    node.height = height;
                    editor.SetDirty();
                }
            }
            
         
            if (Event.current.type == EventType.Repaint) 
                if (editor.focusedNodes.Contains(this))
                    GUIHelper.DrawRectLine(new Rect(default, rect.size).GrowSize(-2), focusedColor, 5);
            
            GUI.DragWindow();
        }

        #region Ports

        List<Port> portDrawBuffer = new List<Port>();
        
        static Texture2D portIcon;
        static Vector2 portIconSize;
        
        Dictionary<Side, Rect[]> portRects = new Dictionary<Side, Rect[]>();
        
        
        public void DrawPorts() {
            if (!visible || Event.current.type != EventType.Repaint) return;
            
            if (!portIcon) {
                portIcon = EditorIcons.GetIcon("Node");
                if (portIcon)
                    portIconSize = new Vector2(portIcon.width, portIcon.height);
                else
                    return;
            }
            
            var updateNodeMinSize = false;
            
            foreach (var side in Sides.straight) {
                portDrawBuffer.Clear();
                portDrawBuffer.AddRange(node.CollectPorts().Where(p => p.side == side));
            
                if (portDrawBuffer.Count == 0) continue;
                
                if (!portRects.TryGetValue(side, out var rects) || rects.Length != portDrawBuffer.Count) {
                    rects = new Rect[portDrawBuffer.Count];

                    Vector2 asix = side.Y() == 0 ? Vector2.up : Vector2.right;
                    
                    for (int i = 0; i < rects.Length; i++)
                        rects[i] = new Rect {
                            position = (1f * i - 0.5f * rects.Length) * portIcon.width * asix,
                            size = portIconSize
                        };
                    
                    portRects[side] = rects;
                    updateNodeMinSize = true;
                }
                
                Vector2 sidePosition = GetSidePosition(side);
                
                for (int i = 0; i < portDrawBuffer.Count; i++) {
                    var port = portDrawBuffer[i];
                    Rect r = rects[i];   
                    r.position += sidePosition;

                    using (GUIHelper.Color.Start(GetPortColor(port)))
                        GUI.DrawTexture(r, portIcon);
                    GUI.Label(r, new GUIContent(port.name.Substring(0, 1), port.tooltip),
                        Styles.tagLabelBlack);
                }
            }
            
            if (updateNodeMinSize) {
                sizeMin = default;
                foreach (var portRect in portRects) {
                    if (portRect.Key.IsHorizontal())
                        sizeMin.y = sizeMin.y.ClampMin(portRect.Value.Sum(r => r.height));
                    else
                        sizeMin.x = sizeMin.x.ClampMin(portRect.Value.Sum(r => r.width));
                }
            }

        }
        
        Rect GetPortBoundRect() {
            if (!portIcon)
                return rect;
            
            return rect.GrowSize(portIconSize * 2);
        }
        
        public static Color GetPortColor(Port port) {
            if (port.info.HasFlag(Port.Info.DoubleSide)) return Color.green;
            if (port.info.HasFlag(Port.Info.Input)) return Color.cyan;
            if (port.info.HasFlag(Port.Info.Output)) return Color.yellow;
            return Color.red;
        }
        
        public static Color GetPortPairColor(Port port) {
            if (port.info.HasFlag(Port.Info.DoubleSide)) return Color.green;
            if (port.info.HasFlag(Port.Info.Input)) return Color.yellow;
            if (port.info.HasFlag(Port.Info.Output)) return Color.cyan;
            return Color.red;
        }
        
        public Port GetPortByPoint(Vector2 point) {
            if (!visible) return null;
            
            if (!GetPortBoundRect().Contains(point)) return null;
         
            if (rect.Contains(point)) return null;
            
            Side side = Side.Null;
            
            if (point.y > rect.yMax)
                side = Side.Bottom;
            else if (point.y < rect.yMin)
                side = Side.Top;
            else if (point.x > rect.xMax)
                side = Side.Right;
            else if (point.x < rect.xMin)
                side = Side.Left;
            
            if (side == Side.Null) return null;
            
            if (!portRects.TryGetValue(side, out var rects) || rects.Length == 0)
                return null;
            
            Vector2 sidePosition = GetSidePosition(side);
            
            portDrawBuffer.Clear();
            portDrawBuffer.AddRange(node.CollectPorts().Where(p => p.side == side));
            
            for (int i = 0; i < portDrawBuffer.Count; i++) {
                Rect r = rects[i];   
                r.position += sidePosition;

                if (r.Contains(point))
                    return portDrawBuffer[i];
            }
            
            return null;
        }

        public bool TryGetPortRect(Port port, out Rect rect) {
            rect = default;
            
            var side = port.side;
            
            if (!side.IsStraight()) return false;
            
            if (!portRects.TryGetValue(side, out var rects) || rects.Length == 0)
                return false;
            
            Vector2 sidePosition = GetSidePosition(side);

            portDrawBuffer.Clear();
            portDrawBuffer.AddRange(node.CollectPorts().Where(p => p.side == side));
            
            if (portDrawBuffer.Count == 0) return false;
            
            int index = portDrawBuffer.IndexOf(port);
            
            if (index < 0) return false;
            
            rect = rects[index]; 
            rect.position += sidePosition;
            
            return true;
        }
        
        Vector2 GetSidePosition(Side side) {
            Vector2 result = side.ToVector2().Scale(y: -1);
            result = rect.center + rect.size * result / 2;
            switch (side) {
                case Side.Left: result.x -= portIconSize.x; break; 
                case Side.Top: result.y -= portIconSize.y; break; 
            }
            return result;
        }
        
        #endregion

        public virtual void ShowContextMenu(GenericMenu menu) {
            menu.AddItem(new GUIContent("Edit"), false, () =>
                ObjectEditorWindow.Show(node, editor, null, editor.Save, true));
            
            NodeEditor.Context(node, menu, editor); 
            
            void Rotate(int o) {
                node.SetOrientation(o);
                portRects.Clear();
            }
                
            menu.AddItem(new GUIContent("Rotate/CW"), false, () => Rotate(node.orientation - 1));
            menu.AddItem(new GUIContent("Rotate/CCW"), false, () => Rotate(node.orientation + 1));
            menu.AddItem(new GUIContent("Rotate/180"), false, () => Rotate(node.orientation + 2));
            menu.AddItem(new GUIContent("Rotate/Break"), false, () => Rotate(0));
            
            menu.AddItem(new GUIContent("Duplicate"), false, () => {
                var newNode = node.Clone();
                newNode.position += new Vector2(30, 30);
                editor.AddNode(newNode);
            });       
            menu.AddItem(new GUIContent("Remove"), false, () => editor.RemoveNode(node));
        }
    }
}