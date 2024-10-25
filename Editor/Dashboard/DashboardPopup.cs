using UnityEditor;
using UnityEngine;
using Yurowm.GUIStyles;

namespace Yurowm.Dashboard {
    public abstract class DashboardPopup : EditorWindow {
        public virtual GUIStyle style => Styles.popup;
        
        public enum Level {
            Dashboard,
            Editor
        }

        public YDashboard yDashboard;
        public Level level = Level.Dashboard;
        
        public virtual void Initialize() { }

        public abstract void OnGUI();
        
        public void Repaint() {
            yDashboard?.Repaint();
        }
        
        public static W Create<W>() where W : EditorWindow {
            return CreateInstance(typeof(W)) as W;
        }
    }
}
