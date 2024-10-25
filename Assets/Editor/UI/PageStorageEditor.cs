using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Yurowm.Dashboard;
using Yurowm.Extensions;
using Yurowm.Serialization;

namespace Yurowm.UI {
    [DashboardGroup("UI")]
    [DashboardTab("Pages", "Window")]
    public class PageStorageEditor : StorageEditor<Page> {
        public override string GetItemName(Page item) {
            return item.ID;
        }

        public override Storage<Page> OpenStorage() {
            return Page.storage;
        }
        
        public PageStorageEditor() {
            UpdatePanels();
        }
        
        static Dictionary<int, UIPanel> panels;

        public override void OnFocus() {
            base.OnFocus();
            UpdatePanels();
        }

        void UpdatePanels() {
            panels = Object
                .FindObjectsOfType<UIPanel>(true)
                .ToDictionaryKey(p => p.linkID);
        }
        
        public static UIPanel GetPanel(int instanceID) {
            return panels.Get(instanceID);
        }
        
        public static IEnumerable<UIPanel> GetPanels() {
            return panels.Values;
        }

        protected override IEnumerable GetSearchData(Page page) {
            yield return base.GetSearchData(page);
            
            foreach (var info in page.panels) {
                var panel = GetPanel(info.linkID);
                if (!panel) continue;
                yield return panel.name;
            }
            
            foreach (var extension in page.extensions) {
                yield return extension.GetType().FullName;
                if (extension is ISearchable searchable)
                    yield return searchable.GetSearchData();
            }
        }
    }
}