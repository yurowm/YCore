using System;
using System.Collections;
using Cysharp.Threading.Tasks;
using UnityEditor;
using UnityEngine;
using Yurowm.Coroutines;
using Yurowm.Dashboard;
using Yurowm.Extensions;
using Yurowm.GUIHelpers;
using Yurowm.GUIStyles;
using Yurowm.Icons;
using Yurowm.InUnityReporting;
using Yurowm.ObjectEditors;

namespace Yurowm.Serialization {
    public abstract class PropertyStorageEditor : DashboardEditor {
        protected IPropertyStorage storage { get; private set; }

        GUIHelper.LayoutSplitter splitter;
        GUIHelper.Scroll settingsScroll = new GUIHelper.Scroll(GUILayout.ExpandHeight(true));

        public override bool isScrollable => false;

        bool rawView = false;

        SerializableReportEditor rawViewDict = null;
        
        public override bool Initialize() {
            Load().Complete();

            rawViewDict = null;
            
            return true;
        }

        public override void OnGUI() {
            if (menuIcon == null) menuIcon = EditorIcons.GetUnityIcon("_Menu@2x", "d__Menu@2x");

            using (settingsScroll.Start()) {
                if (storage == null)
                    Load();
                
                GUILayout.Label($"<{storage.GetType().FullName}>", Styles.miniLabelBlack);
                
                if (rawView)
                    DrawRawItem(storage);
                else
                    ObjectEditor.Edit(storage, this);
                
                GUILayout.FlexibleSpace();
            }
        }
        
        void DrawRawItem(ISerializable item) {
            if (rawViewDict == null) {
                rawViewDict = new SerializableReportEditor();
                var report = new SerializableReport(item);
                report.Refresh();
                rawViewDict.SetProvider(report);
            }
            
            rawViewDict.OnGUI(GUILayout.Height(Mathf.Min(500, rawViewDict.TreeHeight)));
        }

        static Texture2D menuIcon;

        public virtual void OnStorageToolbarGUI() {}

        public override void OnToolbarGUI() {
            using (GUIHelper.Horizontal.Start(EditorStyles.toolbar, GUILayout.ExpandWidth(true))) {
                if (rawView != GUILayout.Toggle(rawView, "Raw", EditorStyles.toolbarButton, GUILayout.Width(50))) {
                    rawView = !rawView;
                    if (rawView) rawViewDict.Refresh();
                }

                OnStorageToolbarGUI();
                
                base.OnToolbarGUI();

                if (GUILayout.Button("Apply", EditorStyles.toolbarButton, GUILayout.Width(100)))
                    Save();

                if (GUILayout.Button(menuIcon, EditorStyles.toolbarButton, GUILayout.Width(25))) {
                    var menu = new GenericMenu();
                    OnOtherContextMenu(menu);
                    if (menu.GetItemCount() > 0)
                        menu.ShowAsContext();
                }
            }
        }
        
        
        protected virtual void OnOtherContextMenu(GenericMenu menu) {
            menu.AddItem(new GUIContent("Reset"), false, () => Load().Forget());
            
            menu.AddItem(new GUIContent("Raw Data/Save to System Buffer"), false, () => 
                EditorGUIUtility.systemCopyBuffer = Serializator.ToTextData(storage));
            
            menu.AddItem(new GUIContent("Raw Data/Source to System Buffer"), false, async () => 
                EditorGUIUtility.systemCopyBuffer = await PropertyStorage.GetSource(storage));
            
            menu.AddItem(new GUIContent("Raw Data/Inject"), false, () => {
                try {
                    var raw = EditorGUIUtility.systemCopyBuffer;
                    var reference = Serializator.FromTextData(raw);
                    if (reference != null && reference.GetType() == storage.GetType()) {
                        Serializator.FromTextData(storage, raw);
                        Debug.Log("Successfully Injected");
                    }
                } catch (Exception e) {
                    Debug.LogException(e);
                }
            });
        }

        protected abstract IPropertyStorage EmitNew();
        
        void Save() {
            PropertyStorage.Save(storage);
        }
        
        async UniTask Load() {
            if (storage == null)
                storage = EmitNew();
            await PropertyStorage.Load(storage);
        }

    }
}