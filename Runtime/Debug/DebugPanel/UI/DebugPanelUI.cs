using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using Yurowm.Coroutines;
using Yurowm.Extensions;
using Yurowm.Serialization;
using Yurowm.UI;
using Yurowm.Utilities;
using UButton = UnityEngine.UI.Button;

namespace Yurowm.DebugTools {
    
    public class DebugPanelUI : MonoBehaviour, IPrefabProvider {
        
        public static DebugPanelUI Instance {
            get;
            private set;
        }

        [OnLaunch]
        static void CreateViewUI() {
            Instance = FindObjectOfType<DebugPanelUI>();
            if (Instance) return;
            
            // if (Application.isEditor || !Debug.isDebugBuild)
            //     return;
            
            var prefab = Resources.Load<DebugPanelUI>("DebugPanel");
            if (!prefab) return;

            Instance = Instantiate(prefab.gameObject)
                .GetComponent<DebugPanelUI>();
            Instance.name = nameof(DebugPanelUI);
            
            Instance.transform.Reset();
            Instance.gameObject.hideFlags = HideFlags.HideInHierarchy | HideFlags.DontSaveInBuild;
            
            Instance.LockWithPassword(!Debug.isDebugBuild && !GameParameters.GetModule<DebugPanelParameters>().password.IsNullOrEmpty());
            
            Instance.gameObject.SetActive(false);
            
            GameSettings.ExecuteOnLoad(s => Instance.gameObject.SetActive(s.GetModule<DebugSettings>().DebugPanel));
            
            ShowPass().Run();
        }
        
        static IEnumerator ShowPass() {
            var clicks = 0;

            int GetPassNum() {
                #if UNITY_WEBGL
                var count = 0;
                if (Input.GetKey(KeyCode.A)) count++;
                if (Input.GetKey(KeyCode.S)) count++;
                if (Input.GetKey(KeyCode.D)) count++;
                return count;
                #else
                return Input.touchCount;
                #endif
            }
            
            while (true) {
                clicks = 0;
                yield return null;

                while (GetPassNum() >= 1) {
                    
                    if (GetPassNum() >= 3) {
                        clicks ++;
                        while (GetPassNum() >= 3)
                            yield return null;
                    }
                    
                    if (clicks >= 10) {
                        Instance.gameObject.SetActive(true);
                        break;
                    }
                    
                    yield return null;
                }
            }
            
        }
        
        Transform reserveRoot;

        void Awake() {
            reserveRoot = new GameObject("Reserve").transform;
            reserveRoot.SetParent(transform);
            reserveRoot.Reset();
            reserveRoot.gameObject.SetActive(false);
            
            virtualizedScroll.prefabProvider = this;
            entryUI?.gameObject.SetActive(false);
            
            SetupButtons();
            SetLock(true);
            ExpandMessage(false);
            
            BuildSettings();
            
            DebugPanel.AddListener(this);
            
            DebugPanel.onLog += OnLog;
            DebugPanel.onNewEntry += OnNew;
            DebugPanel.onNewMessage += OnNew;
        }

        #region Settings

        void BuildSettings() {
            
            const string group = "DebugPanel Settings";
            
            var canvasScaler = gameObject.GetComponentInChildren<CanvasScaler>();
            
            if (canvasScaler) {
                var panelScaleSlider = new DebugVariableRange<float>(
                    () => canvasScaler.referenceResolution.x.RoundToInt(10),
                    v => canvasScaler.referenceResolution = v.RoundToInt(10) * Vector2.one,
                    500, 1000);
                    
                DebugPanel.Log("UI Scale", group, panelScaleSlider);
            }
            
            var boolValue = false;
            
            var boolTest = new DebugVariable<bool>(
                () => boolValue,
                v => boolValue = v);
                    
            DebugPanel.Log("Test Bool", group, boolTest);
            
            var floatValue = 0f;
            
            var intTest = new DebugVariable<float>(
                () => floatValue,
                v => floatValue = v);
                    
            DebugPanel.Log("Test Float", group, intTest);
            
            var intValue = 0;

            var intSliderTest = new DebugVariableRange<int>(
                () => intValue,
                v => intValue = v,
                -10, 10);
                    
            DebugPanel.Log("Test Int (Slider)", group, intSliderTest);
            
            
            var stringValue = "Hello";
            
            var stringTest = new DebugVariable<string>(
                () => stringValue,
                v => stringValue = v);
                    
            DebugPanel.Log("Test String", group, stringTest);
            
            onDestroy += () => DebugPanel.RemoveGroup(group);
        }
        

        
        #endregion
        
        void Close() {
            SetLock(true);
            GameSettings.ExecuteOnLoad(i => i.GetModule<DebugSettings>().DebugPanel = false);
        }
        
        IEnumerator CloseFor5Seconds() {
            SetLock(true);
            gameObject.SetActive(false);
            
            yield return new Wait(5);
            
            gameObject.SetActive(true);
        }
        
        public void MakeVisible(bool value) {
            gameObject.SetActive(value);
        }

        Action onDestroy = delegate {};
        
        void OnDestroy() {
            DebugPanel.onLog -= OnLog;
            DebugPanel.onNewEntry -= OnNew;
            DebugPanel.onNewMessage -= OnNew;
            
            DebugPanel.RemoveListener(this);
            onDestroy?.Invoke();
        }

        void OnLog() {
            isDirty = true;
        }

        void OnNew(DebugPanel.Entry entry) {
            isDirtyFull = true;
        }

        #region Lock
        
        bool isLocked = false;
        
        public CanvasGroup canvasGroup;
        public GameObject controls;
        public GameObject groups;
        public GameObject lockedUI;
        public GameObject passwordGroup;

        public static void Hide() {
            var panel = FindObjectOfType<DebugPanelUI>();
            if (panel) {
                panel.SetLock(true);
                panel.ExpandAll(false);
            }
        }
        
        void SetLock(bool value) {
            isLocked = value;
            lockedUI?.SetActive(isLocked);    
            controls?.SetActive(!isLocked);    
            groups?.SetActive(!isLocked);  
            
            if (canvasGroup) {
                canvasGroup.alpha = isLocked ? .5f : 1f;
                canvasGroup.blocksRaycasts = !isLocked;
            }
        }
        
        void SubmitPassword() {
            if (!passwordField) return;
            
            var password = passwordField.text;
            passwordField.text = "";
            
            var parameters = GameParameters.GetModule<DebugPanelParameters>();
            
            if (parameters.password.IsNullOrEmpty() || password == parameters.password)
                LockWithPassword(false);
        }
        
        void LockWithPassword(bool state) {
            if (state) {
                lockedUI?.SetActive(false);    
                controls?.SetActive(false);    
                groups?.SetActive(false);
            } else
                SetLock(true);

            passwordGroup?.SetActive(state);
        }
        
        public GameObject list;
        public GameObject expandedMessage;
        
        public void ExpandMessage(bool value) {
            list.SetActive(!value);
            expandedMessage.SetActive(value);
        }
        
        #endregion
        
        #region Buttons
        
        public UButton lockButton;
        public UButton unlockButton;
        
        public UButton clearButton;
        public UButton showAllButton;
        public UButton hideAllButton;
        public UButton closeButton;
        public UButton close5Button;
        
        public UButton submitPasswordButton;
        public UButton closePasswordButton;
        public InputField passwordField;
        
        void SetupButtons() {
            lockButton?.onClick.SetSingleListner(() => SetLock(true));
            unlockButton?.onClick.SetSingleListner(() => SetLock(false));
            
            clearButton?.onClick.SetSingleListner(Clear);
            showAllButton?.onClick.SetSingleListner(() => ExpandAll(true));
            hideAllButton?.onClick.SetSingleListner(() => ExpandAll(false));
            closeButton?.onClick.SetSingleListner(Close);
            closePasswordButton?.onClick.SetSingleListner(Close);
            close5Button?.onClick.SetSingleListner(() => CloseFor5Seconds().Run());
            submitPasswordButton?.onClick.SetSingleListner(SubmitPassword);
        }

        #endregion

        #region Update

        bool isDirty = false;
        bool isDirtyFull = false;

        void Update() {
            if (!isDirty && !isDirtyFull) return;
            
            if (EventSystem.current?.currentSelectedGameObject != null)
                if (EventSystem.current.currentSelectedGameObject
                        .SetupComponent(out InputField inputField) &&
                    inputField.isFocused)
                    return;
            
            if (isDirtyFull)
                virtualizedScroll?.SetList(GetEntries());
            else if (isDirty) 
                virtualizedScroll?.Refresh();
            
            isDirty = false;
            isDirtyFull = false;
        }
        
        #endregion

        #region Message Builders

        MessageUIBuilder[] messageBuilders;
        
        static Dictionary<Type, MessageUIBuilder> messageBuildersByType = new Dictionary<Type, MessageUIBuilder>();
        
        MessageUIBuilder Get(Type messageType) {
            if (messageBuilders == null)
                messageBuilders = Utils
                    .FindInheritorTypes<MessageUIBuilder>(true, true)
                    .Select(Activator.CreateInstance)
                    .Cast<MessageUIBuilder>()
                    .Initialize(b => b.debugPanelUI = this)
                    .ToArray();

            if (!messageBuildersByType.TryGetValue(messageType, out var builder)) {
                builder = messageBuilders.FirstOrDefault(d => d.IsSuitableFor(messageType));
                messageBuildersByType.Add(messageType, builder);
            }
            
            return builder;
        }

        public bool Build(DebugPanel.Entry entry, out IVirtualizedScrollItem result) {
            result = null;
            
            if (entry.message == null)
                return false;
            
            var builder = Get(entry.message.GetType());
            
            if (builder == null)
                return false;
            
            result = builder.NewEntry(entry);
            return true;
        }

        public MessageUI[] messageUIprefabs;
        List<MessageUI> reservedMessages = new List<MessageUI>();
        
        public void ClearEntry(DebugPanelEntryUI eui) {
            var mui = eui.messageUI;
            eui.messageUI = null;
            
            if (mui) {
                mui.transform.SetParent(reserveRoot);
                reservedMessages.Add(mui);
            }
        }
        
        
        public MessageUI EmitMessageUI(string name) {
            var result = reservedMessages.Grab(m => m.name == name);
                
            if (!result) {
                var prefab = messageUIprefabs.FirstOrDefault(mui => mui.name == name);
                if (prefab) {
                    result = Instantiate(prefab.gameObject).GetComponent<MessageUI>();
                    result.name = prefab.name;
                }
            }
            
            if (result) 
                result.gameObject.SetActive(true);

            return result;
        }
        
        #endregion
        
        #region Message List

        public VirtualizedScroll virtualizedScroll;
        
        IEnumerable<IVirtualizedScrollItem> GetEntries() {
            foreach (var entry in DebugPanel.GetEntries()) {
                if (!groupButtons.ContainsKey(entry.group)) 
                    EmitGroup(entry.group);
                if (groupVisibile.Contains(entry.group))
                    if (Build(entry, out var entryUI))
                        yield return entryUI;
            }
        }
        
        #endregion

        #region Groups

        Dictionary<string, DebugPanelGroupUI> groupButtons = new();
        List<string> groupVisibile = new();

        public DebugPanelGroupUI groupButtonPrefab;
        
        void ClearGroups() {
            groupButtons.Values
                .ForEach(b => Destroy(b.gameObject));
            groupVisibile.Clear();
            groupButtons.Clear();
        }
        
        void EmitGroup(string name) {
            if (!groupButtonPrefab) {
                groupButtons.Add(name, null);
                return;
            }
            
            var button = Instantiate(groupButtonPrefab.gameObject)
                .GetComponent<DebugPanelGroupUI>();
            
            button.name = name;
            button.transform.SetParent(groups.transform);
            button.transform.Reset();
            button.gameObject.SetActive(true);
            button.SetTitle(name);
            button.SetColor(DebugPanel.GroupToColor(name));
            
            button.action = () => OnGroupClick(name);
            
            groupButtons[name] = button;
            
            SetGroupVisible(name, false);
        }
        
        void ExpandAll(bool value) {
            groupButtons.Keys
                .ToArray()
                .ForEach(g => SetGroupVisible(g, value));
            
            isDirtyFull = true;
        }
        
        void OnGroupClick(string name) {
            SetGroupVisible(name, !groupVisibile.Contains(name));
            isDirtyFull = true;
        }
        
        void SetGroupVisible(string name, bool value) {
            if (groupButtons.TryGetValue(name, out var button)) {
                var visible = groupVisibile.Contains(name);
                
                button.SetAlpha(value ? 1f : .5f);
                
                if (value != visible) {
                    if (value)
                        groupVisibile.Add(name);
                    else
                        groupVisibile.Remove(name);
                }
            }
        }
        
        #endregion
        
        void Clear() {
            DebugPanel.Clear();
            ClearGroups();
            isDirtyFull = true;
        }

        #region IPrefabProvider
        
        public DebugPanelEntryUI entryUI;
        public DebugPanelEntryUI fullScreenEntryUI;
        
        List<VirtualizedScrollItemBody> reservedEntries = new List<VirtualizedScrollItemBody>();

        public VirtualizedScrollItemBody GetPrefab(string name) {
            return entryUI;
        }

        public VirtualizedScrollItemBody Emit(VirtualizedScrollItemBody _) {
            if (!entryUI) return null;
            
            var result = reservedEntries.Grab() ??
                Instantiate(entryUI.gameObject)
                    .GetComponent<VirtualizedScrollItemBody>();
            
            result.name = entryUI.name;
            result.gameObject.SetActive(true);
            
            return result;
        }

        public void Remove(VirtualizedScrollItemBody item) {
            reservedEntries.Add(item);
            item.transform.SetParent(reserveRoot);
        }

        #endregion
    }
}
