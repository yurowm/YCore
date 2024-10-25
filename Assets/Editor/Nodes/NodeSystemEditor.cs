using System;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEngine;
using Yurowm.Extensions;
using Yurowm.GUIHelpers;
using Yurowm.ObjectEditors;
using Yurowm.Serialization;
using Yurowm.Utilities;

namespace Yurowm.Nodes.Editor {
    public class NodeSystemEditor {
        public NodeSystem nodeSystem {
            get;
            private set;
        }

        public readonly EditorWindow window;
        
        public Grid grid = new Grid();
        
        bool paramsVisible;
        
        public NodeSystemEditor(NodeSystem nodeSystem, EditorWindow window) {
            this.nodeSystem = nodeSystem;
            this.window = window;
            
            void GetCenterOnLaunch() {
                GoToCenter();
                onViewRectUpdate -= GetCenterOnLaunch;
            }
            
            onViewRectUpdate += GetCenterOnLaunch;
        }
        
        public void OnGUI(params GUILayoutOption[] options) {
            
            using (GUIHelper.Horizontal.Start(options))
                UpdateViewRect(
                    EditorGUILayout.GetControlRect(
                        GUILayout.ExpandWidth(true),
                        GUILayout.ExpandHeight(true)));

            Scrolling();
            
            if (GUI.enabled) {
                NavigationMove();
                
                var pixelCorrection = 1f / EditorGUIUtility.pixelsPerPoint;
                    
                position.x = position.x.Round(pixelCorrection);
                position.y = position.y.Round(pixelCorrection);
                
                GUI.BeginClip(canvasViewRect, position, Vector2.zero, false);
                
                grid.Draw(new Rect(-position, canvasViewRect.size));
                
                DrawConnections();
                
                ConnectPorts();
                
                DrawNodeWindows();

                GUI.EndClip();
            }
            
            if (dirty) {
                dirty = false;
                Save();        
            }
        }

        bool dirty;
        
        public void SetDirty() {
            dirty = true;
        }
        
        public Action onSave = delegate {};
        
        public void Save() {
            onSave.Invoke();
        }

        #region Navigation & Control
        
        Rect canvasMainRect = new Rect(Vector2.zero, Vector2.one);
        Rect canvasViewRect = new Rect();
        Action onViewRectUpdate = delegate {};
        
        public Rect rect => canvasViewRect;
        
        void UpdateViewRect(Rect rect) {
            if (Event.current.type != EventType.Repaint) return;
            canvasViewRect = rect;
            onViewRectUpdate?.Invoke();
        }

        public Vector2 position;
        public Vector2 cameraPosition {
            get => canvasViewRect.size / 2 - position;
            set => position = canvasViewRect.size / 2 - value;
        }
        
        bool movingCanvas;

        Vector2 s = default;
        
        void Scrolling() {
            Vector2 scrollPos = position;
            
            const float scrollWidth = 16;
            
            #region Vertical Scroll
            if (canvasMainRect.height > canvasViewRect.height) {
                if (Event.current.type == EventType.Repaint)
                    canvasViewRect.xMax -= scrollWidth;

                Rect scrollRect = canvasViewRect;
                scrollRect.xMin = scrollRect.xMax;
                scrollRect.width = scrollWidth;
                
                scrollPos.y = Mathf.Clamp(-scrollPos.y, canvasMainRect.yMin, canvasMainRect.yMax - canvasViewRect.height);
                
                scrollPos.y = -GUI.VerticalScrollbar(scrollRect, scrollPos.y, 
                    canvasViewRect.height, canvasMainRect.yMin, canvasMainRect.yMax);
            } else 
                scrollPos.y = canvasMainRect.yMin;
            #endregion
            
            #region Horizontal Scroll
            if (canvasMainRect.width > canvasViewRect.width) {
                if (Event.current.type == EventType.Repaint)
                    canvasViewRect.yMax -= scrollWidth;
                
                Rect scrollRect = canvasViewRect;
                scrollRect.yMin = scrollRect.yMax;
                scrollRect.height = scrollWidth;
                
                scrollPos.x = -GUI.HorizontalScrollbar(scrollRect, 
                    Mathf.Clamp(-scrollPos.x, canvasMainRect.xMin, canvasMainRect.xMax - canvasViewRect.width),
                    canvasViewRect.width, canvasMainRect.xMin, canvasMainRect.xMax);
            } else 
                scrollPos.x = canvasMainRect.xMin;
            #endregion

            if (scrollPos != position) {
                position = scrollPos;
                UpdateNodesVisibility();
            }
        }
        
        void NavigationMove() {
            if (!Event.current.isMouse) return;
            
            if (!canvasViewRect.Contains(Event.current.mousePosition)) return;

            var mouse = Event.current.mousePosition - position - canvasViewRect.position;

            if (Event.current.type == EventType.MouseDown)
                OnClickDown(mouse);

            if (movingCanvas && Event.current.type == EventType.MouseDrag)
                Move(Event.current.delta);
        }

        void Move(Vector2 delta) {
            if (delta.IsEmpty()) return;

            position += delta;
            UpdateNodesVisibility();
        }
        
        void OnClickDown(Vector2 mouse) {
            
            var clickedNode = nodeDrawers.Values.FirstOrDefault(d => d.visible && d.rect.Contains(mouse));
            
            if (clickedNode != null) {
                if (!focusedNodes.Contains(clickedNode)) {
                    if (!Event.current.control) 
                        focusedNodes.Clear();
                
                    focusedNodes.Add(clickedNode);
                }
                
                ObjectEditorWindow.Show(clickedNode.node, this, null, Save, false);

                movingCanvas = false;
                GUI.FocusControl("");
                return;
            }

            Port clickedPort = GetPortByPoint(mouse);
            if (clickedPort != null) {
                var connectedPort = nodeSystem.connections
                    .FirstOrDefault(c => c.Contains(clickedPort))?
                    .GetAnother(clickedPort);
                
                if (connectedPort != null && !Event.current.alt) {
                    nodeSystem.connections.Remove(new Pair<Port>(clickedPort, connectedPort));
                    SetDirty();
                    connectingPort = connectedPort;
                } else
                    connectingPort = clickedPort;
                
                movingCanvas = false;
                return;
            }
            
            switch (Event.current.button) {
                case 0: {
                    movingCanvas = true; 
                    if (!Event.current.control) 
                        focusedNodes.Clear();
                    break;
                }
                case 1: CanvasContextMenu(mouse); break;
            }

            Repaint();
        }

        public Action<GenericMenu> onContextMenu = delegate {};
        
        void CanvasContextMenu(Vector2 position) {
            GenericMenu menu = new GenericMenu();

            #region Add New Node

            void Add(Type type) {
                try {
                    var node = (Node) Activator.CreateInstance(type);
                    AddNode(node);
                    node.position = position;
                } catch (Exception e) {
                    Debug.LogException(e);
                }
            }
            
            var supportedTypes = nodeSystem.GetSupportedNodeTypes().ToArray();

            foreach (var nodeType in nodeTypes) {
                if (supportedTypes.Any(s => s.IsAssignableFrom(nodeType))) {
                    var t = nodeType;
                    menu.AddItem(new GUIContent($"Add/{nodeType.Name.NameFormat(null, "Node", true)}"), false, () => Add(t));
                }   
            }
            
            if (!nodeReference.IsEmpty())
                if (supportedTypes.Any(t => nodeReference.Any(t.IsInstanceOfType))) {
                    var toPaste = nodeReference
                        .Where(n => supportedTypes.Any(t => t.IsInstanceOfType(n)))
                        .ToArray();
                    
                    var startPosition = toPaste.First().position;
                    
                    menu.AddItem(new GUIContent("Paste"), false, () => {
                        foreach (var nodeRef in toPaste) {
                            var node = nodeRef.Clone();
                            AddNode(node);
                            node.position = grid.Snap(position - startPosition + nodeRef.position);
                        }
                    });
                }

            #endregion
            
            if (nodeDrawers.Count > 0)
                menu.AddItem(new GUIContent("Go To Cenert"), false, GoToCenter);

            onContextMenu.Invoke(menu);

            if (menu.GetItemCount() > 0)
                menu.ShowAsContext();
        }
        
        void GoToCenter() {
            var center = Vector2.zero;
            nodeDrawers.ForEach(d => center += d.Value.rect.center);
            center /= nodeDrawers.Count;
            center.x = Mathf.Round(center.x);
            center.y = Mathf.Round(center.y);
            cameraPosition = center;
            UpdateNodesVisibility();
        }
        
        #endregion

        #region Drawing
        
        void DrawNodeWindows() {
            if (window == null || nodeSystem.nodes.Count <= 0) return;
            
            bool drawingWindows = false;
            bool updateMainRect = Event.current.type == EventType.Repaint;
            bool mainRectInitialized = false;

            foreach (var node in nodeSystem.nodes) {
                var nodeDrawer = GetNodeDrawer(node);
                
                if (nodeDrawer.visible) {
                    if (!drawingWindows) {
                        drawingWindows = true;
                        window.BeginWindows();
                    }
                    nodeDrawer.Draw();
                    nodeDrawer.DrawPorts();
                }
                
                if (updateMainRect) {
                    if (mainRectInitialized) {
                        canvasMainRect.xMin = Mathf.Min(canvasMainRect.xMin, nodeDrawer.rect.xMin);
                        canvasMainRect.yMin = Mathf.Min(canvasMainRect.yMin, nodeDrawer.rect.yMin);
                        canvasMainRect.xMax = Mathf.Max(canvasMainRect.xMax, nodeDrawer.rect.xMax);
                        canvasMainRect.yMax = Mathf.Max(canvasMainRect.yMax, nodeDrawer.rect.yMax);
                    } else {
                        canvasMainRect = nodeDrawer.rect;
                        mainRectInitialized = true;
                    }
                }
            }
            
            if (updateMainRect && mainRectInitialized)
                canvasMainRect = canvasMainRect.GrowSize(canvasViewRect.size * 1.2f);
            
            if (drawingWindows)
                window.EndWindows();
        }
        
        void DrawConnections() {
            if (Event.current.type != EventType.Repaint) return;
            
            foreach (var connection in nodeSystem.connections) {
                    
                var nodeA = GetNodeDrawer(connection.a.node);
                    
                var nodeB = GetNodeDrawer(connection.b.node);
                
                if (!nodeA.visible && !nodeB.visible) continue;
                    
                if (nodeA.TryGetPortRect(connection.a, out var rectA) 
                    && nodeB.TryGetPortRect(connection.b, out var rectB)) {
                    DrawNodeCurve(rectA.center, connection.a.side, rectB.center, connection.b.side,
                        NodeDrawer.GetPortColor(connection.a), NodeDrawer.GetPortColor(connection.b));
                }
            }
        }
        
        Port connectingPort;

        void ConnectPorts() {
            if (connectingPort == null) return;

            var targetPort = GetPortByPoint(Event.current.mousePosition);
            
            if (Event.current.isMouse && Event.current.type != EventType.MouseDrag && Event.current.type != EventType.MouseDown) {
                if (targetPort != null && Port.IsSuitable(connectingPort, targetPort)) {
                    
                    var connection = new Pair<Port>(connectingPort, targetPort);
                    
                    if (!nodeSystem.connections.Contains(connection))
                        nodeSystem.connections.Add(connection);

                    SetDirty();
                }
                
                connectingPort = null;
            } 

            if (Event.current.type == EventType.Repaint && connectingPort != null) {
                
                Color colorA = NodeDrawer.GetPortColor(connectingPort);
                Color colorB = targetPort != null ? NodeDrawer.GetPortColor(targetPort) : NodeDrawer.GetPortPairColor(connectingPort);

                if (targetPort != null && !Port.IsSuitable(connectingPort, targetPort)) 
                    colorB = Color.red;
                
                if (GetNodeDrawer(connectingPort.node).TryGetPortRect(connectingPort, out var portRect))
                    DrawNodeCurve(portRect.center, connectingPort.side, Event.current.mousePosition, Side.Null,
                        colorA, colorB);
            }
            
            
            Repaint();
        }

        void DrawNodeCurve(Vector2 pointA, Side sideA, Vector2 pointB, Side sideB, Color colorA, Color colorB) {
            if (Event.current.type != EventType.Repaint) return;

            if (pointA == pointB) return;

            Vector2 SideToTangent(Side side) {
                return side.ToVector2().Scale(y: -1);
            }
            
            float distance = Vector3.Distance(pointA, pointB);
            distance = Mathf.Min(distance / 2, 100);

            Vector2 startTan = pointA + SideToTangent(sideA) * distance;
            Vector2 endTan = pointB + SideToTangent(sideB) * distance;

            GUIHelper.DrawBezier(pointA, pointB,
                startTan, endTan, 
                colorA, colorB, 5);

        }

        void Repaint() {
            window?.Repaint();
        }

        #endregion

        #region Nodes & Ports

        static readonly List<Type> nodeTypes = Utils.FindInheritorTypes<Node>(true, true)
            .Where(t => !t.IsAbstract)
            .OrderBy(t => t.Name)
            .ToList();

        Dictionary<Node, NodeDrawer> nodeDrawers = new Dictionary<Node, NodeDrawer>();
        
        public List<NodeDrawer> focusedNodes = new List<NodeDrawer>();

        public void AddNode(Node node) {
            node.ID = GetNewNodeID();
            node.OnCreate();
            
            nodeSystem.nodes.Add(node);
            
            SetDirty();
        }
        
        public void RemoveNode(Node node) {
            if (node == null) return;
            
            foreach (var port in node.CollectPorts())
                nodeSystem.connections.RemoveAll(c => c.Contains(port));
            
            nodeDrawers.Remove(node);
            nodeSystem.nodes.Remove(node);
            
            SetDirty();
        }
        
        int GetNewNodeID() {
            if (nodeSystem.nodes.Count == 0)
                return 0;
            
            return nodeSystem.nodes.Max(n => n.ID) + 1;
        }
        
        NodeDrawer GetNodeDrawer(Node node) {
            if (nodeDrawers.TryGetValue(node, out var result))
                return result;
            
            result = new NodeDrawer(node, this);
            nodeDrawers.Add(node, result);
            return result;
        }
        
        void UpdateNodesVisibility() {
            var visibleRect = new Rect(-position, canvasViewRect.size).GrowSize(100);
            
            foreach (var node in nodeDrawers.Values)
                node.visible = node.rect.Overlaps(visibleRect);
            
            Repaint();
        }

        Port GetPortByPoint(Vector2 point) {
            return nodeDrawers.Values
                .Select(d => d.GetPortByPoint(point))
                .NotNull()
                .FirstOrDefault();
        }
        
        #endregion

        static Node[] nodeReference;
        
        public virtual void OnNodeContextMenu(Node node, GenericMenu menu) {
            if (!nodeSystem.GetSupportedNodeTypes().Any(t => t.IsInstanceOfType(node)))
                return;
            
            menu.AddItem(new GUIContent("Copy"), false, () => 
                nodeReference = focusedNodes.Select(d => d.node).ToArray());
            
            if (nodeReference != null && nodeReference.GetType() == node.GetType())
                menu.AddItem(new GUIContent("Paste Values"), false, () => {
                    var id = node.ID;
                    var position = node.position;
                    Serializator.FromTextData(node, Serializator.ToTextData(nodeReference));
                    node.ID = id;
                    node.position = position;
                });
        }
    }
    
    public class NodeSystemEditorWindow : EditorWindow {

        public static NodeSystemEditorWindow Show(NodeSystem system, Action<NodeSystemEditor> setupEditor = null) {
            var window = CreateInstance<NodeSystemEditorWindow>();
            window.editor = new NodeSystemEditor(system, window);
            setupEditor?.Invoke(window.editor);
            window.titleContent = new GUIContent(system.GetType().Name.NameFormat());
            window.ShowUtility();
            return window;
        }

        NodeSystemEditor editor;

        void OnGUI() {
            if (editor == null)
                Close();
            else
                editor.OnGUI();	
        }
    }
}