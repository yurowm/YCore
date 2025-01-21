using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Cysharp.Threading.Tasks;
using Yurowm.Analytics;
using Yurowm.Console;
using Yurowm.Coroutines;
using Yurowm.DebugTools;
using Yurowm.Extensions;
using Yurowm.Serialization;
using Yurowm.Utilities;

namespace Yurowm.UI {
    public class Page : ISerializableID, IStorageElementExtraData {
        
        [PreloadStorage]
        public static Storage<Page> storage = new Storage<Page>("Pages", TextCatalog.StreamingAssets);
        
        public string ID {get; set;}
        
        public PanelInfo.Mode defaultMode = PanelInfo.Mode.Disable;
        public List<PanelInfo> panels = new List<PanelInfo>();
        public List<PageExtension> extensions = new List<PageExtension>();
        static Dictionary<int, UIPanel> panelLinks;
        
        [OnLaunch(Behaviour.INITIALIZATION_ORDER + 1)]
        static void OnLaunch() {
            Initialize();
            
            OnLaunchAttribute.unload += () => {
                IsInitialized = false;
                current = null;
                history.Clear();
            };
            
            GetDefault()?.Show();
        }
        
        [QuickCommand("ui show", "MainMenu", "Show 'MainMenu' page")]
        static void ShowPageCommand(string pageID) {
            var page = Get(pageID); 
            if (page != null) {
                page.Show();
                YConsole.Success("Success!");
            }

            YConsole.Error("Page isn't found!");
        }
        
        [QuickCommand("ui pages", null, "Show all page names")]
        static void ShowPageCommand() {
            foreach (var page in storage.items)
                YConsole.Alias(page.ID);
        }
        
        static bool IsInitialized = false;
        
        static void Initialize() {
            if (IsInitialized) return;
            panelLinks = new Dictionary<int, UIPanel>();
            
            Behaviour
                .GetAll<UIPanel>()
                .ForEach(p => panelLinks.Add(p.linkID, p));

            IsInitialized = true;
            
            ShowQueue().Forget();
        }

        #region Get
        
        public static UIPanel GetPanel(string name) {
            return panelLinks.Values.FirstOrDefault(p => p.name == name);
        }
        
        public static IEnumerable<UIPanel> GetActivePanels() {
            return panelLinks.Values.Where(p => p.isActiveAndEnabled);
        }
        
        public static UIPanel GetPanel(int linkID) {
            return panelLinks.Values.FirstOrDefault(p => p.linkID == linkID);
        }
        
        public static Page Get(string ID) {
            return storage.items.FirstOrDefault(p => p.ID == ID);
        }
        
        public static Page GetDefault() {
            return storage.GetDefault<Page>();
        }

        public static Page GetCurrent() {
            return history.Current ?? GetDefault();
        }
        
        public static Page GetWithTag(string tag) {
            return storage.items.FirstOrDefault(p => p.HasTag(tag));
        }
        
        #endregion
        
        #region Show
        
        static Queue<Action> showQueue = new();
        
        public static Action<Page> onShow = delegate {};
        
        static History<Page> history = new(100);
        
        static Page current;
        
        public void Clean() {
            var uiPanelBuffer = new List<UIPanel>(panelLinks.Values);
            
            foreach (var info in panels) {
                var panel = info.panel;
                if (!panel) continue;
                uiPanelBuffer.Remove(panel);
                if (info.mode != PanelInfo.Mode.Enable)
                    panel.SetVisible(false, true);
            }
            
            if (defaultMode != PanelInfo.Mode.Enable)
                foreach (var panel in uiPanelBuffer) 
                    panel.SetVisible(false, true);
        }
        
        public PanelInfo.Mode GetMode(UIPanel panel) {
            return panels.FirstOrDefault(i => i.panel == panel)?.mode ?? defaultMode;
        }
        
        public void Show(bool immediate = false) {
            
            if (IsAnimating) {
                showQueue.Enqueue(() => Show(immediate));
                return;
            }

            if (current == this)
                return;
            
            current?.extensions.ForEach(e => e.OnHide(current));
            
            current = this;
            
            history.Next(this);
            
            extensions.ForEach(e => e.OnShow(current));

            void SetMode(UIPanel panel, PanelInfo.Mode mode) {
                switch (mode) {
                    case PanelInfo.Mode.Ignore: break;
                    case PanelInfo.Mode.Enable: 
                        panel.SetVisible(true, immediate); break;
                    case PanelInfo.Mode.Disable:
                        panel.SetVisible(false, immediate);
                        break;
                }
            }
            
            var uiPanelBuffer = new List<UIPanel>(panelLinks.Values);
            
            foreach (var info in panels) {
                var panel = info.panel;
                if (!panel) continue;
                uiPanelBuffer.Remove(panel);
                SetMode(panel, info.mode);
            }
            
            foreach (var panel in uiPanelBuffer) 
                SetMode(panel, defaultMode);

            UIRefresh.Invoke();
            
            Analytic.Event($"PageShow_{ID}");
            
            onShow.Invoke(this);
            
            panels.ForEach(p => {
                if (!p.panel) return;
                p.panel.overrideHideClip = null;
                p.panel.overrideShowClip = null;
            });
        }
        
        public static void Back(bool immediate = false) {
            if (!IsAnimating)
                history.Back()?.Show(immediate);
        }
        
        public async UniTask ShowAndWait() { 
            await WaitAnimation();
            Show();
            await WaitAnimation();
        }
        
        static async UniTask ShowQueue() {
            while (true) {
                DebugPanel.Log("Is Animating", "UI", IsAnimating);
                DebugPanel.Log("Current Page ID", "UI", GetCurrent()?.ID);
                
                if (!IsAnimating && showQueue.Count > 0)
                    showQueue.Dequeue().Invoke();
                
                await UniTask.Yield();
            }
        }

        #endregion

        #region Animation
        
        public static bool IsAnimating => HasActiveAnimations || !IsInitialized;

        public static async UniTask WaitAnimation() {
            while (IsAnimating)
                await UniTask.Yield();
        }
        
        static bool HasActiveAnimations => !animations.IsEmpty();        

        static List<ActiveAnimation> animations = new List<ActiveAnimation>();
        
        public static IDisposable NewActiveAnimation() {
            return new ActiveAnimation(animations);
        }
        
        class ActiveAnimation : IDisposable {
            List<ActiveAnimation> all;
            
            public ActiveAnimation(List<ActiveAnimation> all) {
                all.Add(this);
                this.all = all;
            }

            public void Dispose() {
                all.Remove(this);
            }
        }
        
        #endregion

        public class PanelInfo : ISerializable {
            public int linkID;
            public Mode mode = Mode.Enable;
            
            public UIPanel panel => panelLinks.Get(linkID);

            public enum Mode {
                Disable = 0,
                Enable = 1,
                Ignore = 2
            }
            
            public void Serialize(IWriter writer) {
                writer.Write("linkID", linkID);
                writer.Write("mode", mode);
            }

            public void Deserialize(IReader reader) {
                reader.Read("linkID", ref linkID);
                reader.Read("mode", ref mode);
            }
        }

        public StorageElementFlags storageElementFlags { get; set; }
        
        public void Serialize(IWriter writer) {
            writer.Write("ID", ID);
            writer.Write("storageElementFlags", storageElementFlags);
            writer.Write("defaultMode", defaultMode);
            writer.Write("panels", panels.Where(p => p.mode != defaultMode).ToArray());
            writer.Write("extensions", extensions.ToArray());
        }

        public void Deserialize(IReader reader) {
            ID = reader.Read<string>("ID");
            storageElementFlags = reader.Read<StorageElementFlags>("storageElementFlags");
            defaultMode = reader.Read<PanelInfo.Mode>("defaultMode");
            panels.Reuse(reader.ReadCollection<PanelInfo>("panels"));
            extensions.Reuse(reader.ReadCollection<PageExtension>("extensions"));
        }
        
        public static bool Warming {
            get;
            private set;
        } = false;

        public static async UniTask Warm() {
            Warming = true;
            
            foreach (var panel in panelLinks.Values) {
                if (panel.gameObject.activeSelf || !panel.warm)
                    continue;
                panel.SetVisible(true, true);
                await UniTask.Yield();
                panel.SetVisible(false, true);
            }
            
            Warming = false;
        }

        public static async UniTask WaitPage(string pageID) {
            var page = Get(pageID);
            
            if (page == null)
                await WaitAnimation();
            else {
                while (IsAnimating || GetCurrent() != page)
                    await UniTask.Yield();
            }
        }
        
        public static async UniTask WaitFor(Func<Page, bool> func) {
            if (func == null)
                return;
            
            if (func.Invoke(GetCurrent()))
                return;
            
            var wait = true;
            
            void OnShow(Page page) {
                if (func.Invoke(page))
                    wait = false;
            }
            
            onShow += OnShow;

            while (wait)
                await UniTask.Yield();
            
            onShow -= OnShow;
        }
        
        public static async UniTask WaitTag(string tag) {
            if (tag.IsNullOrEmpty())
                return;
            
            await WaitFor(p => p.HasTag(tag));
        }
        
        public static async UniTask WaitNoTag(string tag) {
            if (tag.IsNullOrEmpty())
                return;
            
            await WaitFor(p => !p.HasTag(tag));
        }
    }
}