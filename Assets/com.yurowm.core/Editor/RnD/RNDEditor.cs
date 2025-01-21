using System;
using System.Collections.Generic;
using System.Linq;
using Newtonsoft.Json;
using UnityEditor;
using UnityEngine;
using Yurowm.Dashboard;
using Yurowm.Extensions;
using Yurowm.GUIHelpers;
using Yurowm.HierarchyLists;
using Yurowm.Serialization;
using Yurowm.Utilities;

namespace Utilities.RnD {
    [DashboardGroup("Development")]
    [DashboardTab ("R'n'D", null)]
    public class RNDEditor : DashboardEditor {

        TestList list;
        GUIHelper.LayoutSplitter splitter = null;

        public override bool Initialize() {
            list = new TestList();
            list.onSelectedItemChanged += OnSelectSection;

            splitter = new GUIHelper.LayoutSplitter(OrientationLine.Horizontal, OrientationLine.Vertical, 200);
            return true;
        }

        Dictionary<Type, TestSection> sections = new();
        TestSection currentSection;
        
        static string TypeToStorageKey(Type type) => $"RnDSection_{type.FullName}";

        void OnSelectSection(List<Type> types) {
            if (types.Count == 1) {
                currentSection = sections.Get(types[0]);
                if (currentSection == null) {
                    currentSection = (TestSection) Activator.CreateInstance(types[0]);
                    if (currentSection is ISerializable) {
                        var instance = EditorStorage.Instance.GetSerializable(TypeToStorageKey(types[0]));
                        if (instance is TestSection ts)
                            currentSection = ts;
                    }
                        
                    currentSection.editor = window;
                    currentSection.Initialize();
                    sections.Set(types[0], currentSection);
                }
            }
        }

        public override void OnToolbarGUI() {
            base.OnToolbarGUI();
            if (currentSection is ISerializable serializable)
                if (GUILayout.Button("Save", EditorStyles.toolbarButton, GUILayout.Width(60))) {
                    EditorStorage.Instance.SetSerializable(TypeToStorageKey(currentSection.GetType()), serializable);
                    EditorStorage.Instance.SetDirty();
                }
        }

        public override void OnGUI() {
            using (splitter.Start()) {
                if (splitter.Area()) {
                    list.OnGUI();
                }
                if (splitter.Area()) {
                    if (currentSection != null)
                        currentSection.OnGUI();
                    else
                        GUILayout.FlexibleSpace();
                }
            }
        }

        class TestList : NonHierarchyList<Type> {
            public TestList() : base(Utils.FindInheritorTypes<TestSection>(true, true)
                    .Where(t => !t.IsAbstract)
                    .ToList()) { }

            public override Type CreateItem() {
                return null;
            }

            public override void DrawItem(Rect rect, ItemInfo info) {
                GUI.Label(rect, info.content.Name);
            }

            public override int GetUniqueID(Type element) {
                return element.FullName.GetHashCode();
            }

            protected override bool CanStartDrag(CanStartDragArgs args) {
                return false;
            }
        }
    }
}