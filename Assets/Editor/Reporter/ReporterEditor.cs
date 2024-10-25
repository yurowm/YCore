using System;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEngine;
using Yurowm.Dashboard;
using Yurowm.GUIStyles;
using Yurowm.Utilities;

namespace Yurowm.InUnityReporting {
    [DashboardGroup("Debug")]
    [DashboardTab("Repoter", null)]
    public class RepoterEditor : DashboardEditor {
        static List<ReportEditor> editors = null;

        Report report = null;
        
        public override bool Initialize() {
            if (editors == null)
                editors = Utils.FindInheritorTypes<ReportEditor>(true)
                    .Where(t => !t.IsAbstract)
                    .Select(t => (ReportEditor) Activator.CreateInstance(t))
                    .ToList();
            return true;
        }

        public override void OnGUI() {
            if (report != null)
                if (!report.OnGUI(GUILayout.ExpandHeight(true)))
                    EditorGUILayout.TextArea(report.GetTextReport(), Styles.monospaceLabel, GUILayout.ExpandWidth(true), GUILayout.ExpandHeight(true));
        }

        public override void OnToolbarGUI() {   
            if (GUILayout.Button("Snapshot", EditorStyles.toolbarButton, GUILayout.Width(80))) {
                GenericMenu menu = new GenericMenu();
                foreach (string actionName in Reporter.GetActionsList()) {
                    var name = actionName;
                    menu.AddItem(new GUIContent(name), false, () => {
                        report = Reporter.GetReport(name);
                        var editor = editors.FirstOrDefault(e => e.IsSuitableFor(report));
                        if (editor != null) {
                            editor.SetProvider(report);
                            report = editor;
                        }
                    });
                }
                if (menu.GetItemCount() > 0)
                    menu.ShowAsContext();
                GUI.FocusControl("");
                Repaint();
            }
                
            if (report != null && GUILayout.Button("Refresh", EditorStyles.toolbarButton, GUILayout.Width(80)))
                report.Refresh();
                
            if (report != null && report is ReportEditor editorReport)
                editorReport.OnToolbarGUI();
        }
    }
    
    public abstract class ReportEditor : Report {
        public abstract bool IsSuitableFor(Report report);

        public abstract void SetProvider(Report report);

        public override string GetTextReport() {
            return "Editor";
        }

        public override bool Refresh() {
            return true;
        }

        public virtual void OnToolbarGUI() {}
    }
    
    public abstract class ReportEditor<R> : ReportEditor where R : Report {
        public override bool IsSuitableFor(Report report) {
            return report is R;
        }
        
        public override void SetProvider(Report report) {
            if (IsSuitableFor(report))
                SetProvider((R) report);
        }
        
        public abstract void SetProvider(R report);
    }
}
