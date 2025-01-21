using System;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEngine;
using Yurowm.Dashboard;
using Yurowm.Extensions;
using Yurowm.GUIHelpers;
using Yurowm.GUIStyles;
using Yurowm.HierarchyLists;
using Yurowm.ObjectEditors;
using Yurowm.Utilities;

namespace Yurowm.EditorCore {
    [DashboardGroup("Development")]
    [DashboardTab("S.D.Symbols", "At")]
    public class ScriptingDefineSymbolsEditor : DashboardEditor {

        List<ScriptingDefineSymbolBase> symbols;
        SymbolsTree tree;

        GUIHelper.LayoutSplitter splitter = null;
        GUIHelper.Scroll scroll = null;
        
        BuildTargetGroup selectedBuildTarget = EditorUserBuildSettings.selectedBuildTargetGroup;
        
        [InitializeOnLoadMethod]
        static void EditorOnLaunch() {
            var buildTargetGroup = EditorUserBuildSettings.selectedBuildTargetGroup;
            
            var symbols = GetSymbols(buildTargetGroup);
            
            var changed = false;
            
            symbols
                .CastIfPossible<ScriptingDefineSymbolAuto>()
                .ForEach(s => {
                    var isEnabled = s.IsEnabled();
                    if (isEnabled != s.GetState())
                        changed = true;
                    s.SetState(isEnabled);
                });
            
            if (changed)
                SetSymbols(symbols
                    .Where(s => s.GetState())
                    .Select(s => s.GetSybmol()),
                    buildTargetGroup);
        }

        public override bool Initialize() {
            symbols = GetSymbols(selectedBuildTarget);
            
            tree = new SymbolsTree(GetRawSymbols(selectedBuildTarget).Select(x => new Symbol(x)).ToList());

            splitter = new GUIHelper.LayoutSplitter(OrientationLine.Horizontal, OrientationLine.Vertical, 200);

            scroll = new GUIHelper.Scroll(GUILayout.ExpandHeight(true));
            return true;
        }

        public static IEnumerable<string> GetRawSymbols(BuildTargetGroup targetGroup) {
            var sds = PlayerSettings.GetScriptingDefineSymbolsForGroup(targetGroup).Trim();
            
            if (sds.IsNullOrEmpty()) yield break;
            
            foreach (var symbol in sds.Split(';').Select(x => x.Trim().ToUpper()).Distinct())
                yield return symbol;
        }
        
        static List<Type> symbolsTypes = null;
        
        public static List<ScriptingDefineSymbolBase> GetSymbols(BuildTargetGroup targetGroup) {
            var all = GetRawSymbols(targetGroup);

            if (symbolsTypes == null)
                symbolsTypes = Utils
                    .FindInheritorTypes<ScriptingDefineSymbolBase>(true)
                    .Where(t => !t.IsAbstract)
                    .ToList();
            
            var result = symbolsTypes
                .Select(Activator.CreateInstance)
                .Cast<ScriptingDefineSymbolBase>()
                .ToList();
            
            result.ForEach(s => {
                s.SetState(all.Any(x => x == s.GetSybmol().ToUpper()));
            });
            
            result.Sort((x, y) => String.Compare(x.GetSybmol(), y.GetSybmol(), StringComparison.Ordinal));

            return result;
        }
        
        public static void SetSymbols(IEnumerable<string> symbols, BuildTargetGroup targetGroup) {
            PlayerSettings.SetScriptingDefineSymbolsForGroup(targetGroup, symbols.Join("; "));
            AssetDatabase.Refresh(ImportAssetOptions.ForceUpdate);
        }
        
        public override void OnToolbarGUI() {
            using (GUIHelper.Change.Start(() => Initialize()))
                selectedBuildTarget = (BuildTargetGroup) EditorGUILayout.EnumPopup("Build Target", selectedBuildTarget,
                    EditorStyles.toolbarPopup, GUILayout.Width(300));
            base.OnToolbarGUI();
            if (GUILayout.Button("Save", EditorStyles.toolbarButton, GUILayout.Width(100)))
                SetSymbols(tree.itemCollection.Select(x => x.symbol), selectedBuildTarget);
        }

        public override void OnGUI() {
            using (GUIHelper.Lock.Start(EditorApplication.isCompiling)) {
                using (splitter.Start(true, symbols.Count > 0)) {
                    if (splitter.Area()) 
                        tree.OnGUI(GUILayout.ExpandHeight(true));

                    if (splitter.Area()) {
                        using (scroll.Start()) {
                            using (GUIHelper.Change.Start(UpdateTree)) {
                                foreach (var symbol in symbols) {
                                    using (GUIHelper.Horizontal.Start(Styles.blackArea, GUILayout.ExpandWidth(true))) {
                                        GUILayout.Label(symbol.GetSybmol(), Styles.whiteBoldLabel);
                                        ObjectEditor.Edit(symbol);
                                    }
                                }
                                GUILayout.FlexibleSpace();
                            }
                        }
                    }
                }
            }
        }

        void UpdateTree() {
            foreach (var sds in symbols) {
                var state = sds.GetState();
                var symbol = sds.GetSybmol().ToUpper();
                if (state && tree.itemCollection.All(s => s.symbol != symbol))
                    tree.itemCollection.Add(new Symbol(symbol));
                if (!state)
                    tree.itemCollection.RemoveAll(x => x.symbol == symbol);
            }
            tree.Reload();
        }

        class Symbol {
            public string symbol = null;
            public int id = UnityEngine.Random.Range(int.MinValue, int.MaxValue);

            public Symbol(string symbol) {
                this.symbol = symbol;
                if (!symbol.IsNullOrEmpty())
                    id = symbol.GetHashCode();
            }
        }

        class SymbolsTree : NonHierarchyList<Symbol> {
            public SymbolsTree(List<Symbol> collection) : base(collection) {
            }

            public override Symbol CreateItem() {
                return new Symbol(null);
            }

            public override void DrawItem(Rect rect, ItemInfo info) {
                rect.xMin += 16;
                if (!info.content.symbol.IsNullOrEmpty())
                    GUI.Label(rect, info.content.symbol);
            }

            public override int GetUniqueID(Symbol element) {
                return element.id;
            }

            public override string GetName(Symbol element) {
                return element.symbol;
            }

            public override void SetName(Symbol element, string name) {
                element.symbol = name;
            }

            protected override void RenameEnded(RenameEndedArgs args) {
                args.newName = args.newName.ToUpper();
                base.RenameEnded(args);
            }
        }
    }
}
