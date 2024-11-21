using System.Collections.Generic;
using System.Linq;
using org.mariuszgromada.math.mxparser;
using org.mariuszgromada.math.mxparser.parsertokens;
using UnityEditor;
using UnityEngine;
using Yurowm.GUIHelpers;
using Yurowm.Utilities;

namespace Yurowm.UI {
	[CustomEditor(typeof(ObjectMask))]
	public class ObjectMaskEditor : UnityEditor.Editor {

		ObjectMask main;
		
		SerializedProperty targetsSP;
		SerializedProperty inverseTargetsSP;
		SerializedProperty actionSP;
		SerializedProperty allChildSP;
		SerializedProperty expressionSP;
		
		Expression expression;
		
		void OnEnable() {
			main = (ObjectMask) target;
			targetsSP = serializedObject.FindProperty(nameof(main.targets));
			inverseTargetsSP = serializedObject.FindProperty(nameof(main.inverseTargets));
			actionSP = serializedObject.FindProperty(nameof(main.action));
			allChildSP = serializedObject.FindProperty(nameof(main.allChild));
			expressionSP = serializedObject.FindProperty(nameof(main.expression));
			OnChangeExpression();
		}

		
		List<string> unkownArguments = new List<string>();
		void OnChangeExpression() {
			expression = new Expression(main.expression);
			
			unkownArguments.Clear();
			foreach (var t in expression.getCopyOfInitialTokens())
				if (t.tokenTypeId == Token.NOT_MATCHED && t.looksLike == "argument")
					unkownArguments.Add(t.tokenStr);
			
			if (main.arguments == null)
				main.arguments = new List<ObjectMask.Arg>();
			else
				main.arguments.RemoveAll(a => !unkownArguments.Contains(a.name));

			foreach (var argument in unkownArguments) {
				if (main.arguments.Any(a => a.name == argument))
					continue;
				main.arguments.Add(new ObjectMask.Arg() {
					name = argument
				});
			}
		}

		public override void OnInspectorGUI() {
			Undo.RecordObject(main, "ItemMask changes");

			using (GUIHelper.Change.Start(OnChangeExpression))
				EditorGUILayout.PropertyField(expressionSP);

			#region Arguments

			using (GUIHelper.IndentLevel.Start()) {
				foreach (var arg in main.arguments) {	
					using (GUIHelper.Horizontal.Start()) {
						EditorGUILayout.PrefixLabel(arg.name);
						
						if (GUILayout.Button(arg.reference, EditorStyles.popup, GUILayout.ExpandWidth(true))) {
							var menu = new GenericMenu();

							foreach (var data in ReferenceValues.Keys())
								if (data.type.IsNumericType()) {
									var d = data;
									menu.AddItem(new GUIContent(data.name), arg.reference == data.name, () => {
										arg.reference = d.name;
									});
								}

							if (menu.GetItemCount() > 0)
								menu.ShowAsContext();
						}
					}
				}
			}

			#endregion
			
			EditorGUILayout.PropertyField(actionSP);

			EditorGUILayout.PropertyField(allChildSP);

			if (!main.allChild) {
				EditorGUILayout.PropertyField(targetsSP);
				EditorGUILayout.PropertyField(inverseTargetsSP);
			}

			serializedObject.ApplyModifiedProperties();
		}
	}
}