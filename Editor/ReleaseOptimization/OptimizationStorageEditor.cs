using System;
using System.Linq;
using UnityEditor;
using UnityEngine;
using Yurowm.Dashboard;
using Yurowm.Extensions;
using Yurowm.GUIHelpers;
using Yurowm.GUIStyles;
using Yurowm.ObjectEditors;
using Yurowm.Serialization;

namespace Yurowm.DeveloperTools {
    [DashboardGroup("Development")]
    [DashboardTab("Optimizations", "Hammer")]
    public class OptimizationStorageEditor : StorageEditor<Optimization> {
        bool allowToBuild = false;
        
        public override string GetItemName(Optimization item) {
            return item.ID;
        }

        public override Storage<Optimization> OpenStorage() {
            return Optimization.storage;
        }

        protected override void Sort() {}

        protected override void OnOtherContextMenu(GenericMenu menu) {
            menu.AddItem(new GUIContent("Analysis"), false, Analysis);
            if (allowToBuild)
                menu.AddItem(new GUIContent("Build"), false, () => {
                    Analysis();
                    if (allowToBuild)
                        PackageExporter.Export(PackageExporter.PassType.Customer);
                });
            base.OnOtherContextMenu(menu);
        }

        public void Analysis() {
            var title = "Release Optimizations";
            
            EditorUtility.DisplayProgressBar(title, "Reflection", 0);

            var optimizations = storage.items.ToArray();
            
            float i = 0;
            foreach (var optimization in optimizations) {
                EditorUtility.DisplayProgressBar(title, optimization.ID, i / optimizations.Length);
                optimization.Analysis();
                i++;
            }

            EditorUtility.ClearProgressBar();
            
            allowToBuild = optimizations.All(t => t.validation == Optimization.Validation.Passed);
            
            storage.items.ForEach(UpdateTags);
        }

        public override bool Initialize() {
            passedTag = tags.New("Passed", passedColor);
            errorTag = tags.New("Error", errorColor);
            problemTag = tags.New("Problem", notPassedColor);
            return base.Initialize();
        }
        
        int passedTag;
        int errorTag;
        int problemTag;
        
        
        public static readonly Color unknownColor = new Color(0.76f, 1f, 1f);
        public static readonly Color passedColor = new Color(.5f, 1f, .5f);
        public static readonly Color notPassedColor = new Color(0.6f, 0.44f, 0.44f);
        public static readonly Color errorColor = new Color(1f, 0.24f, 0.29f);

        protected override void UpdateTags(Optimization item) {
            base.UpdateTags(item);
            tags.Set(item, passedTag, item.validation == Optimization.Validation.Passed);
            tags.Set(item, problemTag, item.validation == Optimization.Validation.NotPassed);
            tags.Set(item, errorTag, item.validation == Optimization.Validation.Error);
        }
    }

    public abstract class Optimization : ISerializableID {
        
        public static Storage<Optimization> storage = new Storage<Optimization>("Optimization", TextCatalog.ProjectFolder);
     
        public string ID { get; set; }
        
        public string report;

        public enum Validation {
            Unknown,
            Passed,
            NotPassed,
            Error
        }

        public Validation validation = Validation.Unknown;
        
        public void Analysis() {
            report = "";
            
            if (!isInitialized)
                Initialize();
            
            try {
                validation = DoAnalysis() ? Validation.Passed : Validation.NotPassed;
            }
            catch (Exception e) {
                Debug.LogException(e);
                validation = Validation.Error;
            }
        }
        
        public abstract bool DoAnalysis();
        public abstract bool CanBeAutomaticallyFixed();
        public virtual void Fix() {}
        
        bool isInitialized = false;

        public void Initialize() {
            if (isInitialized)
                return;
            isInitialized = true;

            OnInitialize();
        }
        
        public virtual void OnInitialize() { }

        public Color GetColor() {
            switch (validation) {
                case Validation.Unknown: return OptimizationStorageEditor.unknownColor;
                case Validation.Passed: return OptimizationStorageEditor.passedColor;
                case Validation.NotPassed: return OptimizationStorageEditor.notPassedColor;
                case Validation.Error: return OptimizationStorageEditor.errorColor;
                default: return EditorGUIUtility.isProSkin ? Color.white : Color.black;
            }
        }

        public virtual void Serialize(IWriter writer) {
            writer.Write("ID", ID);
        }

        public virtual void Deserialize(IReader reader) {
            ID = reader.Read<string>("ID");
        }
    }
    
    public class ReleaseOptimizationEditor : ObjectEditor<Optimization> {
        public override void OnGUI(Optimization ro, object context = null) {
            ro.Initialize();
            
            using (GUIHelper.Horizontal.Start()) {
                using (GUIHelper.Color.Start(ro.GetColor())) {
                    EditorGUILayout.PrefixLabel($"Status {ro.validation}", Styles.whiteBoldLabel);
                    
                    if (GUILayout.Button("Analysis", EditorStyles.miniButton, GUILayout.Width(80)))
                        ro.Analysis();

                    if (ro.validation == Optimization.Validation.NotPassed && ro.CanBeAutomaticallyFixed()) {
                        if (GUILayout.Button("Fix", EditorStyles.miniButton, GUILayout.Width(40))) {
                            ro.Fix();
                            if (context is OptimizationStorageEditor rose)
                                rose.Analysis();
                        }
                    }
                }
            }
            
            if (!ro.report.IsNullOrEmpty()) {
                EditorGUILayout.HelpBox(ro.report, MessageType.None, true);
                if (GUILayout.Button("Copy report to clipboard")) 
                    EditorGUIUtility.systemCopyBuffer = ro.report;
            }

        }
    }
}