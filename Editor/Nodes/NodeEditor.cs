using System;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using Yurowm.Extensions;
using Yurowm.GUIHelpers;
using Yurowm.ObjectEditors;
using Yurowm.Utilities;

namespace Yurowm.Nodes.Editor {
    public abstract class NodeEditor {
        static NodeEditor[] editors = null;

        static NodeEditor() {
            editors = Utils.FindInheritorTypes<NodeEditor>(true)
                .Where(t => !t.IsAbstract && !t.ContainsGenericParameters)
                .Select(Activator.CreateInstance)
                .Cast<NodeEditor>()
                .OrderByDescending(e => e.Priority)
                .ToArray();
        }
        
        static Dictionary<Type, NodeEditor[]> references = new Dictionary<Type, NodeEditor[]>();

        public enum Place {
            Node,
            Parameters
        }
        
        static NodeEditor[] GetEditors(Node node) {
            if (node == null) return null;
            
            Type type = node.GetType();
            
            if (!references.ContainsKey(type))
                references.Add(type, NodeEditor.editors.Where(e => e.IsSuitableType(type)).ToArray());
            
            return references[type];
        }
        
        public static void Edit<N>(N node, Place place, NodeSystemEditor editor = null, float labelWidth = -1) where N : Node {
            var editors = GetEditors(node);
            
            if (editors.IsEmpty()) return;
            
            if (!editors.IsEmpty())
                using (GUIHelper.Change.Start(editor.SetDirty))
                using (GUIHelper.EditorLabelWidth.Start(labelWidth))
                    foreach (var nEditor in editors) {
                        switch (place) {
                            case Place.Node: nEditor.OnNodeGUI(node, editor); break;
                            case Place.Parameters: nEditor.OnParametersGUI(node, editor); break;
                        }
                    }
        }
        
        public static void Context(Node node, GenericMenu menu, NodeSystemEditor editor) {
            var editors = GetEditors(node);
            
            if (editors.IsEmpty()) return;
            
            foreach (var nEditor in editors) 
                nEditor.OnContextMenu(node, menu, editor);
        }

        public abstract void OnNodeGUI(object node, NodeSystemEditor editor = null);
        public abstract void OnParametersGUI(object node, NodeSystemEditor editor = null);
        
        public abstract void OnContextMenu(object node, GenericMenu menu, NodeSystemEditor editor = null);
        public abstract bool IsSuitableType(Type type);
        public virtual int Priority => 0;
    }

    public abstract class NodeEditor<N> : NodeEditor where N : Node {
        public override void OnNodeGUI(object node, NodeSystemEditor editor = null) => OnNodeGUI((N) node, editor);
        public override void OnParametersGUI(object node, NodeSystemEditor editor = null) => OnParametersGUI((N) node, editor);
        public override void OnContextMenu(object node, GenericMenu menu, NodeSystemEditor editor = null) => OnContextMenu((N) node, menu, editor);
        
        public abstract void OnNodeGUI(N node, NodeSystemEditor editor = null);
        public abstract void OnParametersGUI(N node, NodeSystemEditor editor = null);
        
        public virtual void OnContextMenu(N node, GenericMenu menu, NodeSystemEditor editor = null) {}

        public override bool IsSuitableType(Type type) {
            return typeof(N).IsAssignableFrom(type);
        }
    }
    
    public class NodeObjectEditor : ObjectEditor<Node> {
        public override void OnGUI(Node node, object context = null) {
            if (context is NodeSystemEditor editor)
                NodeEditor.Edit(node, NodeEditor.Place.Parameters, editor);
        }
    }
}