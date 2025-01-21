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

        GUIHelper.Spoiler spoiler = new GUIHelper.Spoiler();
        string references;
        
        void OnEnable() {
            provider = target as LabelFormat;
            
            if (LocalizationEditor.content == null)
                LocalizationEditor.LoadContent();
            
            references = ReferenceValues.Keys()
                .OrderBy(d => d.name)
                .Select(d => $"{{@{d.name}}} ({d.type.Name})")
                .Join("\n");
        }

        public override void OnInspectorGUI() {
            Undo.RecordObject(provider, "LabelFormat changes");

            DrawDefaultInspector();

            provider.localized = EditorGUILayout.Toggle("Localized", provider.localized);

            if (provider.localized) {
                using (GUIHelper.Horizontal.Start()) {
                    provider.localizationKey = EditorGUILayout.TextField("Localization Key", provider.localizationKey);
                    if (GUILayout.Button("<", EditorStyles.miniButton, GUILayout.Width(18))) {
                        var menu = new GenericMenu();
                        LocalizationEditor
                            .GetKeyList()
                            .ForEach(k => menu.AddItem(new GUIContent(k), false, () => provider.localizationKey = k));    
                        if (menu.GetItemCount() > 0) 
                            menu.ShowAsContext();
                        GUI.FocusControl("");
                    }
                }
                
                if (!provider.localizationKey.IsNullOrEmpty()) 
                    LocalizationEditor.EditOutside(provider.localizationKey);

            } else
                provider.format = EditorGUILayout.TextArea(provider.format);

            if (!references.IsNullOrEmpty())
                using (spoiler.Start("References"))
                    if (spoiler.IsVisible())
                        EditorGUILayout.HelpBox(references, MessageType.None, false);

        }

        #region Key Providers
        [LocalizationKeysProvider]
        static IEnumerator<string> SearchInAssets() {
            var localizedAssets = AssetDatabase.GetAllAssetPaths()
                .Select(AssetDatabase.LoadAssetAtPath<Transform>).Where(t => t)
                .SelectMany(t => t.AndAllChild(true).SelectMany(c =>
                c.gameObject.GetComponents<MonoBehaviour>().CastIfPossible<ILocalizedComponent>()));
            foreach (var asset in localizedAssets)
            foreach (var key in Localization.CollectKeys(asset))
                yield return key;
        }

        [LocalizationKeysProvider]
        static IEnumerator<string> SearchInScenes() {
            var componentType = typeof(Component);

            List<GameObject> roots = new List<GameObject>();
            for (int i = 0; i < SceneManager.sceneCount; i++)
                roots.AddRange(SceneManager.GetSceneAt(i).GetRootGameObjects());

            foreach (Type type in Utils.FindInheritorTypes<ILocalizedComponent>(true))
                if (componentType.IsAssignableFrom(type))
                    foreach (GameObject root in roots)
                    foreach (var comp in root.GetComponentsInChildren(type, true).Cast<ILocalizedComponent>())
                    foreach (var key in Localization.CollectKeys(comp))
                        yield return key;
        }
        #endregion
    }
}
