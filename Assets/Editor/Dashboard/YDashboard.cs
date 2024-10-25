using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using UnityEditor;
using UnityEditor.IMGUI.Controls;
using UnityEngine;
using Yurowm.Extensions;
using Yurowm.GUIHelpers;
using Yurowm.GUIStyles;
using Yurowm.HierarchyLists;
using Yurowm.Icons;
using Yurowm.Utilities;

namespace Yurowm.Dashboard {
    public class YDashboard : EditorWindow {
        static YDashboard instance = null;

        Texture logo;
        static bool style_IsInitialized = false;
        static GUIStyle style_Exeption;
        static GUIStyle tabButtonStyle;
        
        YDashboardEditorsList editorsList;
        GUIHelper.LayoutSplitter splitter = new GUIHelper.LayoutSplitter(
        OrientationLine.Horizontal, 
            OrientationLine.Vertical,
        200);

        void InitializeStyles() {
            style_Exeption = new GUIStyle(GUI.skin.label);
            style_Exeption.normal.textColor = new Color(0.5f, 0, 0, 1);
            style_Exeption.alignment = TextAnchor.UpperLeft;
            style_Exeption.wordWrap = true;

            tabButtonStyle = new GUIStyle(EditorStyles.miniButton);
            tabButtonStyle.normal.textColor = Color.white;
            tabButtonStyle.active.textColor = new Color(1, .8f, .8f, 1);

            style_IsInitialized = true;
        }

        [MenuItem("Tools/Yurowm/Dashboard")]
        public static YDashboard Create() {
            YDashboard window;
            if (instance == null) {
                window = GetWindow<YDashboard>();
                window.Show();
                window.OnEnable();
            } else {
                window = instance;
                window.Show();
            }
            return window;
        }

        void OnFocus() {
            currentEditor?.editor.OnFocus();
        }

        void OnEnable() {
            menuIcon = EditorIcons.GetIcon("MenuIcon");
            maximizedIcon = EditorIcons.GetIcon("MaximizedIcon");
            
            instance = this;
            
            titleContent.text = Application.productName;

            LoadEditors();

            ShowFirstEditor();
        }

        void ShowFirstEditor() {
            if (!lastEditorName.IsNullOrEmpty()) {
                var savedEditor = editorsList.itemCollection.FirstOrDefault(x => x.fullPath == lastEditorName);
                if (savedEditor != null) {
                    Show(savedEditor);
                    return;
                }
            }

            var defaultEditor = editorsList.itemCollection.FirstOrDefault(e => e.editorType.GetCustomAttribute<DashboardDefaultAttribute>() != null);
            if (defaultEditor != null)
                Show(defaultEditor);
        }
                
        void LoadEditors() {
            var editors = Utils.FindInheritorTypes<IDashboardEditor>(true)
                .Where(t => !t.IsInterface && t.GetCustomAttribute<DashboardTabAttribute>() != null)
                .Select(t => new YEditor() {
                    editorType = t,
                    group = t.GetCustomAttribute<DashboardGroupAttribute>(true)?.Group,
                    attribute = t.GetCustomAttribute<DashboardTabAttribute>(true)
                })
                .OrderBy(e => e.group.IsNullOrEmpty() ? 0 : 1)
                .ThenBy(e => e.group)
                .ThenBy(e => e.attribute.Title)
                .ToList();

            int indexer = 0;
            editors.ForEach(e => e.ID = indexer++);
            
            editorsList = new YDashboardEditorsList(editors);
            editorsList.onSelectedItemChanged = l => {
                if (l.Count > 0)
                    Show(l[0]);
            };

        }

        public Vector2 editorScroll;
        YEditor currentEditor;
        public IDashboardEditor CurrentEditor => currentEditor?.editor;
        public Rect editorRect;
        Action editorRender;
        
        bool menuVisible = true;
        
        void OnGUI() {
            if (!style_IsInitialized)
                InitializeStyles();

            if (editorRender == null || currentEditor == null) {
                editorRender = null;
                currentEditor = null;
            }

            var popup = GetTopPopup();
            
            using (GUIHelper.Lock.Start(popup != null && popup.level == DashboardPopup.Level.Dashboard)) {
                DrawToolbar();
                using (splitter.Start(menuVisible, true)) {
                    if (splitter.Area()) {
                        using (GUIHelper.Vertical.Start(GUILayout.ExpandHeight(true))) {
                            if (!logo)
                                logo = PlayerSettings.GetIconsForTargetGroup(BuildTargetGroup.Unknown).FirstOrDefault();

                            if (logo) {
                                var logoRect = EditorGUILayout.GetControlRect(GUILayout.ExpandWidth(true), GUILayout.Height(logo.height.ClampMax(80)));
                                GUI.DrawTexture(logoRect, logo, ScaleMode.ScaleToFit);
                            }
                            
                            DrawTabs();
                        }
                    }
                    
                    if (splitter.Area()) {
                        if (currentEditor != null && currentEditor.editor.isScrollable)
                            editorScroll = EditorGUILayout.BeginScrollView(editorScroll);

                        if (currentEditor != null && editorRender != null) {
                            try {
                                if (EditorApplication.isCompiling)
                                    GUILayout.Label("Compiling...", Styles.centeredMiniLabel, GUILayout.ExpandHeight(true), GUILayout.ExpandWidth(true));
                                else {
                                    using (GUIHelper.Lock.Start(popup != null && popup.level == DashboardPopup.Level.Editor))
                                        editorRender.Invoke();
                                }
                            } catch (Exception e) {
                                Debug.LogException(e);
                                GUILayout.Label(e.ToString(), style_Exeption);
                            }
                        } else
                            GUILayout.Label("Nothing selected", Styles.centeredMiniLabel, GUILayout.ExpandHeight(true), GUILayout.ExpandWidth(true));

                        if (currentEditor != null && currentEditor.editor.isScrollable)
                            EditorGUILayout.EndScrollView();

                        if (Event.current.type == EventType.Repaint)
                            editorRect = GUILayoutUtility.GetLastRect();
                    }
                }
            }
        }
        
        Texture2D menuIcon;
        Texture2D maximizedIcon;

        void DrawToolbar() {
            using (GUIHelper.Toolbar.Start()) {

                using (GUIHelper.ContentColor.ProLiteStart()) {
                    menuVisible = GUILayout.Toggle(menuVisible, menuIcon, EditorStyles.toolbarButton, GUILayout.Width(25));
                    maximized = GUILayout.Toggle(maximized, maximizedIcon, EditorStyles.toolbarButton, GUILayout.Width(25));
                }

                if (!menuVisible) {
                    var icon = currentEditor.attribute?.Icon;
                    if (icon)
                        using (GUIHelper.Color.ProLiteStart())
                            GUI.DrawTexture(EditorGUILayout.GetControlRect(false, 16, GUILayout.Width(16)),
                                icon, ScaleMode.ScaleToFit);
                    GUILayout.Label(
                        new GUIContent(currentEditor.Title), 
                        EditorStyles.boldLabel, GUILayout.ExpandWidth(false));
                }
                
                if (currentEditor != null)
                    currentEditor.editor.OnToolbarGUI();
                else 
                    GUILayout.FlexibleSpace();
            }    
        }

        void DrawTabs() {
            editorsList.OnGUI(GUILayout.ExpandHeight(true));
        }

        public static void Scroll(float position) {
            if (instance != null)
                instance.editorScroll = new Vector2(0, position);
        }

        static string lastEditorName {
            get => EditorStorage.Instance.GetText("dashboard.lastEditor");  
            set => EditorStorage.Instance.SetText("dashboard.lastEditor", value);
        }

        void Show(YEditor yeditor) {
            EditorGUI.FocusTextInControl("");
            
            yeditor.Emit();
            
            var editor = yeditor.editor;
            
            editorsList.SetSelected(yeditor);
            
            yeditor.editor.window = this;
            popups.ForEach(p => p?.Close());
            popups.Clear();
            if (editor.Initialize()) {
                currentEditor = yeditor;
                lastEditorName = yeditor.fullPath;
                editorRender = currentEditor.editor.OnGUI;
            }
        }
        
        public void Show(IDashboardEditor editor) {
            var yeditor = editorsList.itemCollection.FirstOrDefault(e => e.editorType == editor.GetType());
            
            if (yeditor == null) return;
            
            yeditor.editor = editor;
            
            Show(yeditor);
        }

        public static IDashboardEditor GetCurrentEditor() {
            if (instance == null) return null;
            return instance.currentEditor.editor;
        }

        public void Show(string editorName) {
            var editor = editorsList.itemCollection.FirstOrDefault(x => x.fullPath == editorName);
            if (editor != null) Show(editor);
        }

        #region Popup

        List<DashboardPopup> popups = new List<DashboardPopup>();
        
        DashboardPopup GetTopPopup() {
            return popups.FirstOrDefault();
        }
        
        public void ShowPopup(DashboardPopup popup) {
            popups.Insert(0, popup);
            popup.yDashboard = this;
            var position = popup.position;
            position.center = new Vector2(Input.mousePosition.x, Screen.height / 2);
            popup.position = position;
            popup.ShowUtility();
            popup.Initialize();
            Repaint();
        }

        #endregion
        
        class YEditor {
            public Type editorType;
            public IDashboardEditor editor;
            public string Title => attribute.Title;
            public DashboardTabAttribute attribute;
            public string group;
            public int ID;

            public string fullPath {
                get {
                    if (group.IsNullOrEmpty())
                        return Title;
                    return $"{group}/{Title}";
                }
            }


            public string path {
                get {
                    if (group.IsNullOrEmpty())
                        return "";
                    return group;
                }
            }

            public void Emit() {
                if (editor != null) return;
                editor = (IDashboardEditor) Activator.CreateInstance(editorType);
             }
        }
        
        class YDashboardEditorsList : HierarchyList<YEditor> {
            
            Texture2D defaultEditorIcon;
            
            public YDashboardEditorsList(List<YEditor> collection) : base(collection, null, new TreeViewState()) {
                ExpandAll();
                defaultEditorIcon = EditorIcons.GetIcon("DefaultEditorIcon");
            }

            public override string GetName(YEditor element) {
                return element.Title;
            }

            public override void DrawFolder(Rect rect, FolderInfo info) {
                GUI.Label(rect, info.name, EditorStyles.boldLabel);
            }
            
            YEditor selected;
            static readonly Color selectedColor = new Color(0.31f, 0.56f, 1f, 0.32f);
            public void SetSelected(YEditor editor) {
                selected = editor;
            }

            public override void DrawItem(Rect rect, ItemInfo info) {
                if (info.content == selected)
                    Handles.DrawSolidRectangleWithOutline(rect, selectedColor, Color.clear);
                
                var icon = info.content.attribute.Icon ? 
                    info.content.attribute.Icon : defaultEditorIcon;
                
                rect = ItemIconDrawer.DrawSolid(rect, icon);
                
                base.DrawItem(rect, info);
            }

            public override string GetPath(YEditor element) {
                return element.path;
            }

            public override void SetPath(YEditor element, string path) {}

            public override int GetUniqueID(YEditor element) {
                return element.ID;
            }

            public override YEditor CreateItem() {
                return null;
            }

            public override void ContextMenu(GenericMenu menu, List<IInfo> selected) {
            }

            protected override bool CanRename(TreeViewItem item) {
                return false;
            }

            protected override bool CanStartDrag(CanStartDragArgs args) {
                return false;
            }
        }
    }
}