using System.Linq;
using UnityEditor;
using UnityEngine;
using Yurowm;
using Yurowm.Extensions;
using Yurowm.GUIHelpers;

namespace YMatchThree.Editor {
    public class PointProviderEditor {
        
        IPointProvider provider;
        SerializedObject serializedObject;
        
        
        public enum Options {
	        Default = Line,
	        Line = 1 << 0
        }
        
        public Options options = Options.Default;
        
        SerializedProperty pointsProp;
        
        public PointProviderEditor(IPointProvider provider, SerializedObject serializedObject) {
	        this.provider = provider;
	        this.serializedObject = serializedObject;
	        pointsProp = serializedObject.FindProperty("points");
        }
        
        public void OnSceneGUI() {
	        if (provider == null || !pointsProp.isExpanded)
		        return;
	        
	        if (pointsProp.arraySize == 0) {
                pointsProp.InsertArrayElementAtIndex(0);
				ApplyChanges();   
            }
	        
	        if (options.HasFlag(Options.Line)) {
	            Handles.color = Color.green;
	            Handles.DrawAAPolyLine(
		            Enumerator.For(0, pointsProp.arraySize - 1, 1)
			            .Select(i =>  pointsProp.GetArrayElementAtIndex(i).vector2Value)
			            .Select(p => provider.TransformPoint(p))
			            .ToArray());
	        }

            using (GUIHelper.Change.Start(ApplyChanges)) {

                if (Event.current.control) {
	                DrawRemovePointPosition(ref pointsProp, 1);
                } else {
                    for (int i = 0; i < pointsProp.arraySize; i++) {
                        var position = pointsProp.GetArrayElementAtIndex(i);
                        DrawUpdatePointPosition(ref position, i.ToString());
                    }
                    
                    if (Event.current.shift)
	                    DrawInbetweenButtons(ref pointsProp);
                }
            }
        }
        
        
        void ApplyChanges() {
	        serializedObject.ApplyModifiedProperties();
        }
        
        void DrawRemovePointPosition(
            ref SerializedProperty positions,
            int minPoints) {
	        
            for (int i = 0; i < positions.arraySize; i++) {
                var element = positions.GetArrayElementAtIndex(i);
				
                var worldPosition = provider.TransformPoint(element.vector2Value);

                float handleSize = HandleUtility.GetHandleSize(worldPosition) * 0.1f;

                if (!Handles.Button(worldPosition, Quaternion.identity, handleSize, handleSize,
                        DrawRemovePointHandle) || positions.arraySize <= minPoints) continue;
				
                // shift other points
                for (int j = i; j < positions.arraySize - 1; j++)
                    positions.GetArrayElementAtIndex(j).vector2Value =
                        positions.GetArrayElementAtIndex(j + 1).vector2Value;

                positions.DeleteArrayElementAtIndex(positions.arraySize - 1);

                GUI.changed = true;
            }
        }
        
        void DrawUpdatePointPosition(ref SerializedProperty position, string label) {
	        var worldPosition = provider.TransformPoint(position.vector2Value);

	        float size = HandleUtility.GetHandleSize(worldPosition);
	        
            var draggedPosition = provider.InverseTransformPoint(
                Handles.FreeMoveHandle(
                    worldPosition,
                    size * .1f,
                    Vector3.zero,
                    DrawPointHandle
                )
            );
            
            if (!label.IsNullOrEmpty())
				Handles.Label(worldPosition + new Vector3(0, size * .4f, 0), label);

            if (draggedPosition != position.vector2Value)
	            position.vector2Value = draggedPosition;
        }
        
        void DrawInbetweenButtons(ref SerializedProperty positions) {
	        
			Handles.color = Color.red;

			for (int i = positions.arraySize - 2; i >= 0; i--) {

				var element = positions.GetArrayElementAtIndex(i);
				var elementNext = positions.GetArrayElementAtIndex(i + 1);
				
				Vector3 worldPosition = (element.vector2Value + elementNext.vector2Value) * 0.5f;

				worldPosition = provider.TransformPoint(worldPosition);

				var handleSize = HandleUtility.GetHandleSize(worldPosition) * 0.08f;

				if (!Handles.Button(worldPosition, Quaternion.identity, handleSize, handleSize,
					DrawAddPointHandle)) continue;
				
				positions.InsertArrayElementAtIndex(positions.arraySize);

				// shift other points
				for (int j = positions.arraySize - 1; j > i; j--)
					positions.GetArrayElementAtIndex(j).vector2Value =
						positions.GetArrayElementAtIndex(j - 1).vector2Value;

				positions.GetArrayElementAtIndex(i + 1).vector2Value = 
					provider.InverseTransformPoint(worldPosition);

				GUI.changed = true;
			}
        }
        
        static void DrawPointHandle(int controlId, Vector3 position, Quaternion rotation, float size, EventType eventType){
	        Handles.color = Color.black;

	        Handles.DrawSolidDisc(position, Vector3.forward, size * 1.4f);

	        Handles.color = Color.white;
	        Handles.DrawSolidDisc(position, Vector3.forward, size);
	        Handles.CircleHandleCap(controlId, position, rotation, size, eventType);

	        Handles.color = Color.black;
	        Handles.DrawSolidDisc(position, Vector3.forward, size * 0.8f);
        }

        static void DrawRemovePointHandle(int controlId, Vector3 position, Quaternion rotation, float size, EventType eventType){
	        Handles.color = Color.black;

	        Handles.DrawSolidDisc(position, Vector3.forward, size * 1.4f);
	        Handles.CircleHandleCap(controlId, position, rotation,  size * 1.4f, eventType);

	        Handles.color = Color.red;
	        Handles.DrawSolidDisc(position, Vector3.forward, size);

	        Handles.color = Color.black;
	        Handles.DrawSolidDisc(position, Vector3.forward, size * 0.8f);
        }

        static void DrawAddPointHandle(int controlId, Vector3 position, Quaternion rotation, float size, EventType eventType){
	        Handles.color = Color.black;
	        Handles.CircleHandleCap(controlId, position, rotation, size, eventType);
	        Handles.DrawSolidDisc(position, Vector3.forward, size);

	        Handles.color = Color.white;
	        Handles.DrawSolidDisc(position, Vector3.forward, size * 0.2f);
        }
    }
}