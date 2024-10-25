using System;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEngine;
using UnityEngine.SceneManagement;
using Yurowm.Extensions;
using Yurowm.GUIHelpers;
using Yurowm.UI;
using Yurowm.Utilities;

namespace Yurowm.Localizations {
    [CustomEditor(typeof(LabelFormat))]
    public class LabelFormatEditor : Editor {
        LabelFormat provider;

        GUIHelper.Spoiler spoiler = new();
        string references;
        
        void OnEnable() {
            provider = target as LabelFormat;
            
            references = ReferenceValues.Keys()
                .OrderBy(d => d.name)
                .Select(d => $"{{@{d.name}}} ({d.type.Name})")
                .Join("\n");
        }

        public override void OnInspectorGUI() {
            Undo.RecordObject(provider, "LabelFormat changes");

            DrawDefaultInspector();

            provider.format = EditorGUILayout.TextArea(provider.format);

            if (!references.IsNullOrEmpty())
                using (spoiler.Start("References"))
                    if (spoiler.IsVisible())
                        EditorGUILayout.HelpBox(references, MessageType.None, false);

        }
    }
}
