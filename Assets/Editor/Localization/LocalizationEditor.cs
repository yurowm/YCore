using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Reflection;
using System.Text;
using System.Text.RegularExpressions;
using Newtonsoft.Json;
using UnityEditor;
using UnityEditor.IMGUI.Controls;
using UnityEngine;
using UnityEngine.Assertions.Must;
using Yurowm.Coroutines;
using Yurowm.Dashboard;
using Yurowm.Editors;
using Yurowm.Extensions;
using Yurowm.GUIHelpers;
using Yurowm.GUIStyles;
using Yurowm.HierarchyLists;
using Yurowm.Icons;
using Yurowm.Integrations;
using Yurowm.ObjectEditors;
using Yurowm.Serialization;
using Yurowm.Services;
using Yurowm.Utilities;
using Yurowm.YJSONSerialization;

namespace Yurowm.Localizations {
    [DashboardGroup("UI")]
    [DashboardTab("Localization", "LocalizationLanguageIcon", 1)]
    public class LocalizationEditor : DashboardEditor {
        static List<LocalizationKeysProvider> keysProviders;

        static LocalizationEditor() {
            keysProviders = new List<LocalizationKeysProvider>();
            
            foreach (var method in AppDomain.CurrentDomain.GetAssemblies().SelectMany(a => a.GetTypes())
                .SelectMany(t => t.GetMethods(BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic))
                .Where(m => m.GetCustomAttributes(true).Any(a => a is LocalizationKeysProvider))) {
                var provider = method.GetCustomAttributes(true).CastOne<LocalizationKeysProvider>();
                provider.SetMethod(method);
                keysProviders.Add(provider);
            }
            
            foreach (var field in AppDomain.CurrentDomain.GetAssemblies().SelectMany(a => a.GetTypes())
                .SelectMany(t => t.GetFields(BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic))
                .Where(m => m.FieldType == typeof(string) && m.GetCustomAttributes(true).Any(a => a is LocalizationKeysProvider))) {
                var provider = field.GetCustomAttributes(true).CastOne<LocalizationKeysProvider>();
                provider.SetField(field);
                keysProviders.Add(provider);
            }
        }
        
        public LocalizationEditor() {
            InitializeTags();
        }

        public static List<Entry> phrases = new List<Entry>();
        static List<Entry> phrases_ref = new List<Entry>();
        public Language? language = null;
        public Language? languageRef = null;

        PhraseTree tree;
        GUIHelper.LayoutSplitter splitter;
        GUIHelper.SearchPanel searchPanel;

        List<Language> _languages = null;
        List<Language> languages {
            get {
                if (_languages == null)
                    _languages = LanguageContent.GetSupportedLanguages().ToList();
                return _languages;
            }
            set {
                _languages = value;
                LanguageContent.SetSupportedLanguages(value);
            }
        }

        public static Dictionary<Language, LanguageContent> content;

        Entry selectedEntry = null;
        Entry refEntry = null;

        public override bool Initialize() {
            if (language.HasValue) Refresh();

            if (language == null || (!languageRef.HasValue && languages.Count > 1)) {
                language = languages.First();
                languageRef = languages.FirstOrDefault(x => x != language);
                return Initialize();
            }

            if (searchPanel == null)
                searchPanel = new GUIHelper.SearchPanel("");

            splitter = new GUIHelper.LayoutSplitter(OrientationLine.Horizontal, OrientationLine.Vertical, 200);

            CreateTree();
            
            return true;
        }

        const string phraseTreeStatePref = "LocalizationEditor_PhraseTree";
        TreeViewState phraseTreeState = null;

        void CreateTree() {
            if (phraseTreeState == null) {
                string stateRaw = EditorPrefs.GetString(phraseTreeStatePref);
                if (stateRaw.IsNullOrEmpty())
                    phraseTreeState = new TreeViewState();
                else
                    phraseTreeState = JsonUtility.FromJson<TreeViewState>(stateRaw);
            }

            tree = new PhraseTree(phrases, phraseTreeState);
            tree.onSelectedItemChanged = x => {
                if (x.Count == 1)
                    selectedEntry = x[0];
                else
                    selectedEntry = null;

                if (phrases_ref != null && selectedEntry != null) {
                    refEntry = phrases_ref.FirstOrDefault(e => e.fullPath == selectedEntry.fullPath);
                } else
                    refEntry = null;
            };
            tree.onChanged = Save;
            tree.drawItem = DrawPhraseItem;
            tree.onRebuild += () => tree.itemCollection.ForEach(UpdateTags);
            tree.onRemove += keyInfos => {
                foreach (var keyInfo in keyInfos)
                foreach (var content in content.Values)
                    content[keyInfo.asItemKind.content.fullPath] = null;
                
                Save();
            };
            
            tags.SetName("Localization");
            tags.SetFilter(tree);
            
            if (searchPanel != null && !searchPanel.value.IsNullOrEmpty())
                tree.SetSearchFilter(searchPanel.value);
            else
                ReloadTree();
        }

        public override void OnGUI() {
            using (splitter.Start(true, true)) {
                if (splitter.Area()) {
                    using (GUIHelper.Change.Start(Save))
                        tree.OnGUI();
                }
                if (splitter.Area()) {
                    if (language.HasValue)
                        GUILayout.Label(language.Value.ToString(), Styles.title, GUILayout.ExpandWidth(true));
                    if (selectedEntry != null) {
                        GUILayout.Label(selectedEntry.fullPath, Styles.centeredMiniLabel, GUILayout.ExpandWidth(true));

                        using (scrollEntryEditor.Start())
                            using (GUIHelper.Change.Start(Save)) 
                                selectedEntry.value = EditorGUILayout.TextArea(selectedEntry.value, Styles.textAreaLineBreaked, GUILayout.ExpandHeight(true), GUILayout.ExpandWidth(true));

                        if (refEntry != null && !refEntry.value.IsNullOrEmpty())
                            using (scrollRefEditor.Start())
                                EditorGUILayout.HelpBox(refEntry.value, MessageType.None, true);
                        
                        UpdateTags(selectedEntry);
                    } else
                        GUILayout.Label("Select an entry to edit", Styles.centeredMiniLabel, GUILayout.ExpandHeight(true), GUILayout.ExpandWidth(true));
                }
            }
        }

        public Rect DrawPhraseItem(Rect rect, Entry entry) {
            return tags.DrawLabelTags(rect, entry);
        }

        GUIHelper.Scroll scrollEntryEditor = new GUIHelper.Scroll(GUILayout.ExpandHeight(true), GUILayout.ExpandWidth(true));
        GUIHelper.Scroll scrollRefEditor = new GUIHelper.Scroll(GUILayout.MaxHeight(150), GUILayout.ExpandWidth(true));

        static Texture2D menuIcon;
        static Texture2D plusIcon;

        public override void OnToolbarGUI() {

            #region Add New Language
            
            if (plusIcon == null) 
                plusIcon = EditorIcons.GetIcon("PlusIcon");

            using (GUIHelper.ContentColor.ProLiteStart()) 
                if (GUILayout.Button(plusIcon, EditorStyles.toolbarButton, GUILayout.Width(25))) {
                    
                    void AddLanguage(Language language) {
                        languages.Add(language);
                        languages = languages; // To Save
                        content.Add(language, LanguageContent.CreateEmpty(GetKeyList()));
                        this.language = language;
                        Save();
                        Initialize();
                        ReloadTree();
                    }
                    
                    void RemoveLanguage(Language language) {
                        if (EditorUtility.DisplayDialog(
                            "Remove language",
                            "Are you sure that you want to remove this language? All the entries will be lost.",
                            "Remove", "Cancel")) {
                            languages.Remove(language);
                            content.Remove(language);
                            this.language = languages[0];
                            languages = languages;
                            Initialize();
                            ReloadTree();
                        }
                    }
                    
                    GenericMenu menu = new GenericMenu();
                    foreach (Language language in Enum.GetValues(typeof(Language))) {
                        var existed = languages.Contains(language);
                            
                        var _l = language;
                        if (!existed || languages.Count > 1) {
                            menu.AddItem(new GUIContent(language.ToString()), existed, () => {
                                if (existed) 
                                    RemoveLanguage(_l);
                                else 
                                    AddLanguage(_l);
                            });
                        }
                    }
                    menu.ShowAsContext();
                }
            
            #endregion
            
            #region Language Selector

            using (GUIHelper.Color.Start(ItemIconDrawer.GetUniqueColor(language.ToString())))
                if (GUILayout.Button(language.ToString(), EditorStyles.toolbarPopup, GUILayout.Width(100))) {
                    var menu = new GenericMenu();

                    foreach (var lang in languages) {
                        var _lang = lang;
                        menu.AddItem(new GUIContent(lang.ToString()), lang == language, () => {
                            language = _lang;
                            Initialize();
                        });
                    }

                    if (menu.GetItemCount() > 0)
                        menu.ShowAsContext();
                }

            #endregion

            #region Reference Selector

            if (languages.Count > 1) {
                GUILayout.Label("Ref:", EditorStyles.toolbarButton, GUILayout.Width(30));
                
                using (GUIHelper.Color.Start(ItemIconDrawer.GetUniqueColor(languageRef.ToString())))
                    if (GUILayout.Button(languageRef.ToString(), EditorStyles.toolbarPopup, GUILayout.Width(100))) {
                        var menu = new GenericMenu();

                        foreach (var lang in languages) {
                            if (lang == language) 
                                continue;
                            
                            var _lang = lang;
                            menu.AddItem(new GUIContent(lang.ToString()), lang == languageRef, () => {
                                languageRef = _lang;
                                Initialize();
                            });
                        }

                        if (menu.GetItemCount() > 0)
                            menu.ShowAsContext();
                    }
            }

            #endregion
            
            searchPanel.OnGUI(x => tree.SetSearchFilter(x), GUILayout.ExpandWidth(true));

            tags.FilterButton(tree);

            #region Other Menu

            if (menuIcon == null)
                menuIcon = EditorIcons.GetUnityIcon("_Menu@2x", "d__Menu@2x");

            using (GUIHelper.ContentColor.ProLiteStart()) 
                if (GUILayout.Button(menuIcon, EditorStyles.toolbarButton, GUILayout.Width(30))) {
                    var menu = new GenericMenu();
                    
                    menu.AddItem(new GUIContent("Refresh"), false, Refresh);
                    
                    menu.AddItem(new GUIContent("Sort"), false, () => {
                        Sort();
                        Save();
                        ReloadTree();
                    });
                    
                    menu.AddItem(new GUIContent("Get Missed Keys"), false, GetMissedKeys);
                    
                    menu.AddItem(new GUIContent("Tasks/Export (Spreadsheet)"), false, TasksExportSpreadsheet);
                    menu.AddItem(new GUIContent("Tasks/Export"), false, TasksExport);
                    menu.AddItem(new GUIContent("Tasks/Import"), false, TasksImport);

                    menu.AddItem(new GUIContent("Collect Charset/All"), false, () => 
                        CharsetToBuffer(CollectCharset()));
                    
                    foreach (var lang in languages) {
                        var l = lang;
                        menu.AddItem(new GUIContent($"Collect Charset/{lang}"), false, () =>
                            CharsetToBuffer(CollectCharset(l)));
                    }
                    
                    if (menu.GetItemCount() > 0)
                        menu.ShowAsContext();
                }

            #endregion
        }
        
        void ReloadTree() {
            tree.Reload();
        }

        void Sort() {
            phrases.SortBy(p => p.fullPath);
        }

        void GetMissedKeys() {
            bool changed = false;
            
            var actualKeys = keysProviders.SelectMany(p => p.GetKeys()).ToHashSet();

            foreach (string key in actualKeys)
                if (phrases.All(x => x.fullPath != key)) {
                    phrases.Add(new Entry(key, ""));
                    changed = true;
                }

            GPTContext.Instance.Clear(actualKeys);
            
            if (changed) {
                Sort();
                Save();
                Refresh();
            } else 
                Actualize();
        }
        
        void CharsetToBuffer(IEnumerable<char> charset) {
            var array = charset.ToArray();
            EditorGUIUtility.systemCopyBuffer = new string(array);
        }
        
        IEnumerable<char> CollectCharset() {
            return languages.SelectMany(CollectCharset).Distinct().OrderBy(c => c);
        }

        IEnumerable<char> CollectCharset(Language language) {
            var content = LanguageContent.LoadFast(language, false);
            
            if (content == null)
                return Array.Empty<char>();
            
            return content.Select(p => p.Value).NotNull().SelectMany(v => v).Distinct().OrderBy(c => c);
        }

        #region Export/Import Tasks

        void TasksExportSpreadsheet() {
            if (language == null) return;
            
            var content = LanguageContent.LoadFast(language.Value, false);
            
            if (content == null) return;

            var builder = new StringBuilder();
            
            foreach (var pair in content)
                builder.AppendLine($"{pair.Key} {pair.Value}");

            GUIUtility.systemCopyBuffer = builder.ToString();
        }
        
        void TasksExport() {
            var referenceContent = LanguageContent.LoadFast(Language.English, false);;
            
            if (referenceContent == null) 
                return;

            var path = EditorUtility.OpenFolderPanel("Export Tasks", null, null);
            
            if (path.IsNullOrEmpty())
                return;
            
            var actualKeys = keysProviders.SelectMany(p => p.GetKeys()).ToList();
            
            foreach (var lang in languages) {
                if (lang == Language.English)
                    continue;
                
                var content = LanguageContent.LoadFast(lang, false);
                
                if (content == null) continue;
                
                var pack = new LanguagePack();

                foreach (var key in actualKeys)
                    if (!key.IsNullOrEmpty() && !content.HasKey(key) || content[key].IsNullOrEmpty())
                        pack.content[key] = referenceContent[key];

                if (!pack.content.IsEmpty()) {
                    var raw = JsonConvert.SerializeObject(pack);
                    File.WriteAllText(Path.Combine(path, $"{lang}.json"), raw);
                }
            }
        }
        
        void TasksImport() {
            var path = EditorUtility.OpenFolderPanel("Import Tasks", null, null);
            
            if (path.IsNullOrEmpty())
                return;
            
            foreach (var lang in languages) {
                
                var content = LanguageContent.LoadFast(lang, false);
                if (content == null) continue;
                
                var file = new FileInfo(Path.Combine(path, $"{lang}.json"));
                if (!file.Exists) continue;
                
                var raw = File.ReadAllText(file.FullName);
                if (raw.IsNullOrEmpty())
                    continue;

                var pack = JsonConvert.DeserializeObject<LanguagePack>(raw);
                if (pack.content == null)
                    continue;

                foreach (var pair in pack.content)
                    if (!pair.Key.IsNullOrEmpty())
                        content[pair.Key] = pair.Value;
                
                SaveContent(content);
            }
            
            Refresh();
        }
        
        #endregion
        
        void Actualize() {
            List<string> actualKeys = keysProviders.SelectMany(p => p.GetKeys()).ToList();
            
            foreach (Entry phrase in phrases)
                phrase.actual = actualKeys.Contains(phrase.fullPath);
        }
        
        void Refresh() {
            EditorUtility.DisplayProgressBar("Localization", "Refreshing", 0);
            
            LoadContent();
            phrases = content[language.Value].Select(x => new Entry(x.Key, x.Value)).ToList();
            if (languages.Count > 1) {
                if (!languageRef.HasValue || languageRef == language)
                    languageRef = languages.First(x => x != language);
                phrases_ref = content[languageRef.Value].Select(x => new Entry(x.Key, x.Value)).ToList();
            } else {
                languageRef = null;
                phrases_ref = null;
            }
            
            Actualize();
            
            CreateTree();
            
            EditorUtility.ClearProgressBar();
        }

        public static string[] GetKeyList() {
            LanguageContent content;
            List<string> result = new List<string>();

            foreach (Language language in LanguageContent.GetSupportedLanguages()) {
                content = LanguageContent.LoadFast(language, false);
                if (content == null) continue;
                foreach (var pair in content)
                    if (!pair.Key.IsNullOrEmpty() && !result.Contains(pair.Key))
                        result.Add(pair.Key);
            }

            result.Sort();

            return result.ToArray();
        }

        public static void LoadContent() {
            content = null;

            content = new Dictionary<Language, LanguageContent>();
            foreach (Language language in LanguageContent.GetSupportedLanguages())
                content.Add(language, LanguageContent.LoadFast(language, true));
        }

        public void Save() {
            Save(language.Value);

            EditorPrefs.SetString(phraseTreeStatePref, JsonUtility.ToJson(tree.state));
        }

        public static void Save(Language language) {
            content[language] = LanguageContent.Create(phrases.ToDictionary(x => x.fullPath, x => x.value));

            SaveContent(language);
        }

        public static void SaveContent(Language language) {
            var c = content[language];
            if (c == null) return;
            c.language = language;
            SaveContent(c);
        }

        public static void SaveContent(LanguageContent content) {
            var raw = content.ToJson();
            TextData.SaveText(Path.Combine("Languages", content.language + Serializer.FileExtension), raw);
        }

        public static void Edit(string _search = "") {
            var languages = LanguageContent.GetSupportedLanguages().ToArray();
            if (languages.Length > 0)
                Edit(languages[0], _search);
        }

        public static void Edit(Language _language, string _search = "") {
            var dashboard = YDashboard.Create();
            LocalizationEditor editor;

            dashboard.ShowTab();

            if (dashboard.CurrentEditor is LocalizationEditor)
                editor = (LocalizationEditor) dashboard.CurrentEditor;
            else {
                editor = new LocalizationEditor();
                dashboard.Show(editor);
            }
        
            editor.language = _language;
            editor.searchPanel.value = _search;
            editor.Initialize();
            editor.Repaint();
        }

        #region Edit Popup

        public static void EditPopup(string key) {
            ObjectEditorWindow.Show(new Key(key), null, null, null, true);
        }
        
        class Key {
            public string key;

            public Key(string key) {
                this.key = key;
            }
        }
        
        class KeyEditor: ObjectEditor<Key> {
            public override void OnGUI(Key obj, object context = null) {
                EditorGUILayout.LabelField("Key", obj.key);
                EditOutside(obj.key, true);
            }
        }

        #endregion
        
        
        public static void EditOutside(string key, bool longText = false) {
            EditOutside(null, key, longText);
        }
        
        static bool locked;
        
        public class GPTContext: ISerializable {
            static GPTContext _Instance;
            public static GPTContext Instance {
                get {
                    if (_Instance == null) {
                        _Instance = EditorStorage.Instance.GetSerializable("gptContext") as GPTContext;
                        _Instance ??= new GPTContext();
                    }
                
                    return _Instance;
                }
            }
            
            public static void Save() {
                if (_Instance != null) {
                    EditorStorage.Instance.SetSerializable("gptContext", _Instance);
                    EditorStorage.Instance.SetDirty();
                }
            }
            
            public Dictionary<string, string> dictionary = new ();
            
            public string this[string key] {
                get {
                    if (dictionary.TryGetValue(key, out var value))
                        return value;
                    return string.Empty;
                }
                set {
                    if (value.IsNullOrEmpty())
                        dictionary.Remove(key);
                    else 
                        dictionary[key] = value;
                }
            }

            public void Clear(IEnumerable<string> actualKeys) {
                foreach (var key in dictionary.Keys.ToArray().Except(actualKeys)) 
                    dictionary.Remove(key);
                Save();
            }
            
            public void Serialize(IWriter writer) {
                dictionary.RemoveAll(p => p.Value.IsNullOrEmpty());
                writer.Write("dictionary", dictionary);
            }

            public void Deserialize(IReader reader) {
                dictionary.Reuse(reader.ReadDictionary<string>("dictionary"));
            }
        }
        
        public static void EditOutside(string label, string key, bool longText = false) {
            using (GUIHelper.Horizontal.Start()) {
                EditorGUILayout.LabelField(label ?? string.Empty);
                if (GUILayout.Button("...", GUILayout.Width(20))) {
                    var menu = new GenericMenu();
                    
                    void Copy(bool full) {
                        var builder = new StringBuilder();
                        
                        foreach (var language in content.Keys.ToArray()) {
                            var value = content[language].HasKey(key) ? content[language][key] : string.Empty;
                            
                            if (!full && value.IsNullOrEmpty())
                                continue;
                            
                            builder.AppendLine($"+++ {language}: {value}");
                        }
                        
                        EditorGUIUtility.systemCopyBuffer = builder.ToString();
                    }
                    
                    menu.AddItem(new GUIContent("Copy"), false, () => Copy(false));
                    
                    menu.AddItem(new GUIContent("Copy Full"), false, () => Copy(true));
                        
                    menu.AddItem(new GUIContent("Paste"), false, () => {
                        var regex = new Regex(@"\+{3}\s(?<language>\w+)\:(?<phrase>.*?)(?=\+{3}|$)", RegexOptions.Singleline);
                        foreach (Match match in regex.Matches(EditorGUIUtility.systemCopyBuffer)) {
                            if (Enum.TryParse(match.Groups["language"].Value, out Language language)) {
                                content[language][key] = match.Groups["phrase"].Value.Trim();
                                SaveContent(language);
                            }
                        }
                    });
                    
                    if (menu.GetItemCount() > 0)
                        menu.ShowAsContext();
                }
            }

            using (GUIHelper.Lock.Start(locked))
            using (GUIHelper.IndentLevel.Start())
            using (GUIHelper.Vertical.Start()) {
                var filled = 0;
                foreach (var language in content.Keys.ToArray()) {
                    bool has = content[language].HasKey(key);

                    string value = has ? content[language][key] : "";
                    
                    if (!value.IsNullOrEmpty())
                        filled ++;

                    if (longText) {
                        using (GUIHelper.Horizontal.Start()) {
                            EditorGUILayout.PrefixLabel(language.ToString());
                            value = EditorGUILayout.TextArea(value, Styles.textAreaLineBreaked);
                            if (GUILayout.Button("X", GUILayout.Width(20))) 
                                value = string.Empty;
                        }
                    }
                    else
                        value = EditorGUILayout.TextField(language.ToString(), value);

                    if (has || (value != key && !value.IsNullOrEmpty()))
                        content[language][key] = value;

                    if (GUI.changed)
                        SaveContent(language);
                }
                
                if (content.Count > 1) {
                    using (GUIHelper.Horizontal.Start()) {
                        using (GUIHelper.Change.Start(GPTContext.Save))
                            GPTContext.Instance[key] = EditorGUILayout.TextField("GPT Context", GPTContext.Instance[key]);

                        if (filled != 0 && filled != content.Count && GUILayout.Button("Fill", GUILayout.Width(40))) {
                            var order = new LocalizationOrder();
            
                            order.dict = content
                                .ToDictionary(c => c.Key.ToString(), 
                                    c => c.Value.HasKey(key) ? c.Value[key] ?? "" : "");
            
                            order.context = GPTContext.Instance[key];
                    
                            if (order.dict.Values.All(v => v.IsNullOrEmpty()))
                                return;
                    
                            locked = true;
                    
                            EditorGUI.FocusTextInControl("");
                    
                            Integration
                                .Get<YurowmAPIIntegration>()
                                .Request("localize", order, MethodType.POST, r => {
                                    foreach (var pair in content)
                                        if (r.dict.TryGetValue(pair.Key.ToString(), out var value)) {
                                            content[pair.Key][key] = value;
                                            SaveContent(pair.Key);
                                        }
                                })
                                .ContinueWith(() => {
                                    locked = false;
                                    GUI.changed = true;
                                })
                                .Run();
                        }
                    }
                }
            }
        }
        
        public class LocalizationOrder {
            public string context { get; set; }
            public Dictionary<string, string> dict { get; set; }
        }
        
        public static void Set(string key, string value, Language language) {
            if (!content.ContainsKey(language))
                return;
            
            var has = content[language].HasKey(key);

            if (has || (value != key && !value.IsNullOrEmpty())) {
                content[language][key] = value;
                SaveContent(language);
            }
        }

        public static string Get(string key, Language language) {
            if (!content.ContainsKey(language))
                return key;
            
            return content[language][key];
        }

        #region Tags

        StorageTags<Entry> tags = new StorageTags<Entry>();

        int emptyTag;
        int notUsedTag;
        
        void InitializeTags() {
            tags = new StorageTags<Entry>();
            
            emptyTag = tags.New("Empty", .15f);
            notUsedTag = tags.New("Not Used", 0f);
        }
        
        void UpdateTags(Entry entry) {
            tags.Set(entry, emptyTag, entry.value.IsNullOrEmpty());
            tags.Set(entry, notUsedTag, !entry.actual);
        }

        #endregion

        class PhraseTree : HierarchyList<Entry> {
            static readonly Color notActualColor = new Color(1, 1, 1, .5f);
            
            public Func<Rect, Entry, Rect> drawItem;

            public PhraseTree(List<Entry> collection, TreeViewState state) : base(collection, null, state) {}
            const string newItemNameFormat = "id{0}";
            const string newGroupNameFormat = "group{0}";

            public override void ContextMenu(GenericMenu menu, List<IInfo> selected) {
                if (selected.Count == 0) {
                    menu.AddItem(new GUIContent("New Entry"), false, () => AddNewItem(root, newItemNameFormat));
                    menu.AddItem(new GUIContent("New Group"), false, () => AddNewFolder(root, newGroupNameFormat));

                } else {
                    if (selected.Count == 1 && selected[0].isFolderKind) {
                        FolderInfo parent = selected[0].asFolderKind;

                        menu.AddItem(new GUIContent("Add New Entry"), false, () => AddNewItem(parent, newItemNameFormat));
                        menu.AddItem(new GUIContent("Add New Group"), false, () => AddNewFolder(parent, newGroupNameFormat));
                    } else {
                        FolderInfo parent = selected[0].parent;
                        if (selected.All(x => x.parent == parent))
                            menu.AddItem(new GUIContent("Group"), false, () => Group(selected, parent, newGroupNameFormat));
                        else 
                            menu.AddItem(new GUIContent("Group"), false, () => Group(selected, root, newGroupNameFormat));
                    }

                    if (selected.Count == 1 && selected[0].isItemKind) {
                        var key = selected[0].asItemKind.content.fullPath;

                        menu.AddItem(new GUIContent("Edit"), false, () => EditPopup(key));

                        menu.AddItem(new GUIContent("Copy Key"), false, () => 
                            EditorGUIUtility.systemCopyBuffer = key);
                        
                        foreach (var pair in content) {
                            if (!pair.Value.HasKey(key)) continue;
                            var value = pair.Value[key];
                            if (value.IsNullOrEmpty() || value == key) continue;

                            var c = pair.Value;
                            menu.AddItem(new GUIContent($"Copy Value/{pair.Key}"), false, () => 
                                EditorGUIUtility.systemCopyBuffer = c[key]);
                        }
                        
                        menu.AddItem(new GUIContent("Remove For All Lang"), false, () => {
                            if (!EditorUtility.DisplayDialog("Remove", "Are you sure want to remove this item from all languages", "Remove", "Cancel"))
                                return;
                            foreach (var lang in content.Keys.ToArray()) {
                                content[lang].Remove(key);
                                SaveContent(lang);
                            }
                            phrases.RemoveAll(p => p.fullPath == key);
                            Remove(true, selected.ToArray());
                        });
                    }

                    menu.AddItem(new GUIContent("Remove"), false, () => Remove(selected.ToArray()));
                }
            }

            public override void DrawItem(Rect rect, ItemInfo info) {
                rect = ItemIconDrawer.DrawAuto(rect, info.content);
                
                if (drawItem != null)
                    rect = drawItem(rect, info.content);
                
                base.DrawItem(rect, info);
            }

            public override string GetPath(Entry element) {
                return element.path;
            }

            public override int GetUniqueID(Entry element) {
                return ("/" + element.fullPath).GetHashCode();
            }

            public override void SetPath(Entry element, string path) {
                element.path = path;
            }

            protected override bool CanRename(TreeViewItem item) {
                return true;
            }

            public override void SetName(Entry element, string name) {
                element.name = name;
            }

            public override string GetName(Entry element) {
                return element.name;
            }

            public override Entry CreateItem() {
                return new Entry("", "");
            }

            protected override bool ItemSearchFilter(IInfo item, string filter) {
                if (item is ItemInfo i && Search(i.content.value, filter))
                    return true;
            
                return base.ItemSearchFilter(item, filter);
            }
        }

        public class Entry {
            public Entry(string path, string value) {
                
                this.value = value;
                int sep = path.LastIndexOf('/');
                if (sep >= 0) {
                    this.path = path.Substring(0, sep);
                    name = path.Substring(sep + 1, path.Length - sep - 1);
                } else
                    name = path;
            }

            public string value = "";
            public string name = "";
            public string path = "";
            public bool actual = true;
            public string fullPath => (path.Length > 0 ? path + "/" : "") + name;
        }
    }

    [Serializable]
    public class LanguagePack: ISerializable, ICollection<KeyValuePair<string, string>> {
        public Language language;
        public Dictionary<string, string> content = new();

        public void Add(KeyValuePair<string, string> item) => content[item.Key] = item.Value;

        public void Clear() => content.Clear();

        public bool Contains(KeyValuePair<string, string> item) => content.ContainsKey(item.Key);

        public void CopyTo(KeyValuePair<string, string>[] array, int arrayIndex) { }

        public bool Remove(KeyValuePair<string, string> item) => content.Remove(item.Key);

        public int Count => content.Count;

        public bool IsReadOnly => false;
        
        public IEnumerator<KeyValuePair<string, string>> GetEnumerator() {
            foreach (var pair in content)
                yield return pair;
        }

        IEnumerator IEnumerable.GetEnumerator() {
            return GetEnumerator();
        }

        public void Serialize(IWriter writer) {
            writer.Write("language", language);
            writer.Write("content", content);
        }

        public void Deserialize(IReader reader) {
            reader.Read("language", ref language);
            content.Reuse(reader.ReadDictionary<string>("content"));
        }
    }
    
    public class LocalizationObjectEditor : ObjectEditor {
        Language language;

        public LocalizationObjectEditor() {
            if (LocalizationEditor.content == null)
                LocalizationEditor.LoadContent();
            language = LocalizationEditor.content.Keys.First();
        }

        Dictionary<Type, Dictionary<PropertyInfo, EditorLocalizedKeyAttribute>> infos = new();

        public override bool IsSuitableType(Type type) {
            if (infos.ContainsKey(type)) 
                return infos[type].Count > 0;
            
            var info = new Dictionary<PropertyInfo, EditorLocalizedKeyAttribute>();
            infos.Add(type, info);

            while (type != null) {
                foreach (PropertyInfo propertyInfo in type.GetProperties(BindingFlags.NonPublic | BindingFlags.Public | BindingFlags.Instance)) {
                    if (propertyInfo.PropertyType != typeof(string)) continue;

                    var attribute = propertyInfo.GetCustomAttribute<EditorLocalizedKeyAttribute>();
                    if (attribute == null) continue;

                    info.Add(propertyInfo, attribute);
                }
                type = type.BaseType;
            }

            return info.Count > 0;
        }

        public override void OnGUI(object obj, object context = null) {
            Dictionary<PropertyInfo, EditorLocalizedKeyAttribute> info;
            Type type = obj.GetType();
            
            info = infos[type];

            language = (Language) EditorGUILayout.EnumPopup(new GUIContent("Language"), language,
                e => LocalizationEditor.content.ContainsKey((Language) e), false);

            foreach (var pair in info) {
                string key = pair.Key.GetValue(obj) as string;
                bool has = LocalizationEditor.content[language].HasKey(key);

                string value = has ? LocalizationEditor.content[language][key] : key;

                if (pair.Value.longText) {
                    using (GUIHelper.Horizontal.Start()) {
                        EditorGUILayout.PrefixLabel(pair.Value.name);
                        value = EditorGUILayout.TextArea(value, Styles.textAreaLineBreaked);
                    }
                } else
                    value = EditorGUILayout.TextField(pair.Value.name, value);

                if (has || value != key)
                    LocalizationEditor.content[language][key] = value;
            }

            if (GUI.changed)
                LocalizationEditor.SaveContent(language);
        }
    }
}