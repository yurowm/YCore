using UnityEditor;
using UnityEngine;
using Yurowm.GUIHelpers;

namespace Utilities.RnD {
    public abstract class Vector2TestSection : TestSection {
        float scale = 50;
    
        Rect vectorsRect;
    
        public override void OnGUI() {
            scale = EditorGUILayout.FloatField("Scale", scale);
        
            vectorsRect = EditorGUILayout.GetControlRect(true, 300, GUILayout.ExpandWidth(true));
        
            if (Event.current.type != EventType.Repaint) return;
        
            Handles.DrawSolidRectangleWithOutline(vectorsRect, Color.black, Color.clear);
        
            Handles.color = new Color(1, 1, 1, .2f);
        
            Handles.DrawLine(
                new Vector3(vectorsRect.xMin, vectorsRect.center.y),
                new Vector3(vectorsRect.xMax, vectorsRect.center.y)
            );
            Handles.DrawLine(
                new Vector3(vectorsRect.center.x, vectorsRect.yMin),
                new Vector3(vectorsRect.center.x, vectorsRect.yMax)
            );

            using (GUIHelper.Clip.Start(vectorsRect, out vectorsRect)) 
                OnDrawVectors();
        }
    
        public abstract void OnDrawVectors();

        protected void DrawVector(Vector2 vector, Color color) {
            Handles.color = color;
        
            Handles.DrawLine(
                vectorsRect.center,
                vectorsRect.center + vector * scale
            );
        }

        protected void DrawVector(Vector2 position, Vector2 vector, Color color) {
            Handles.color = color;
        
            Handles.DrawLine(
                vectorsRect.center + position * scale,
                vectorsRect.center + (position + vector) * scale
            );
        }
    }
}