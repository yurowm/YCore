using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using Yurowm.Colors;
using Yurowm.ContentManager;
using Yurowm.Coroutines;
using Yurowm.Extensions;
using Yurowm.UI;
using Yurowm.Utilities;

namespace Yurowm.ComposedPages {
    public class ComposedPage : Behaviour, IPageContainer {
        
        // [OnLaunch(100)]
        // static void PrepareElements() {
        //     if (!AssetManager.Instance) return;
        //     
        //     foreach (ComposedElement element in AssetManager.GetPrefabList<ComposedElement>())
        //         if (element is IReserved)
        //             Reserve.Prepare(element, 2);
        // }
        
        static Dictionary<string, ComposedPage> all = new Dictionary<string, ComposedPage>();

        public static ComposedPage ByID(string composedPageID) {
            return all.Get(composedPageID);
        } 
        
        public static ComposedPage Any() {
            return all.Values.FirstOrDefault();
        }
        
        UIPanel panel;
        public Transform container;
        public Button backgroundButton;
        
        public string composedPageID;

        #region Color Scheme
        
        public string colorScheme;
        
        public UIColorScheme GetColorScheme() {
            if (colorScheme.IsNullOrEmpty())
                return UIColorScheme.current;
            return UIColorScheme.storage.items
                       .FirstOrDefault(s => s.ID == colorScheme)
                   ?? UIColorScheme.current;
        }
        
        #endregion
        
        public override void Initialize() {
            base.Initialize();
            if (!composedPageID.IsNullOrEmpty())    
                all[composedPageID] = this;
            this.SetupComponent(out panel);
        }

        public void HideElements() {
            container.AllChild(false)
                .Select(c => c.GetComponent<ComposedElement>())
                .Where(c => c)
                .ForEach(c => c.gameObject.SetActive(false));
        }

        public void RemoveElements() {
            container.AllChild(false)
                .Select(c => c.GetComponent<ComposedElement>())
                .Where(c => c)
                .ForEach(c => c.Kill());
        }


        #region IPageContainer

        public Page currentPage = null;

        public Transform GetContainer() {
            return container;
        }

        public void Show(Page page) {
            Showing(page).Run();
        }

        public Page GetCurrentPage() {
            return currentPage;
        }

        #endregion

        IEnumerator Showing(Page page) {
            if (IsShown() && page.parentPage != currentPage) {
                currentPage.ShowSubPage(page);
                yield break;
            }

            yield return Animation(false);

            HideElements();

            currentPage = page;

            if (!currentPage.IsBuilt)
                currentPage.Build(this);
            
            if (currentPage.backgroundClose)
                backgroundButton?.SetAction(BackgroundClose);
            else 
                backgroundButton?.NoAction();
            
            if (container.SetupComponent(out CanvasGroup cg))
                cg.blocksRaycasts = currentPage.interactable;

            currentPage.Show();

            transform.SetAsLastSibling();
            
            yield return Animation(true);
        }

        public void BackgroundClose() {
            if (currentPage != null && currentPage.backgroundClose)
                Close();
        }
        
        public void Close() {
            Closing().Run();
        }
        
        IEnumerator Closing() {
            if (currentPage != null) {
                yield return Animation(false);
                currentPage.Clear();
                currentPage.visible = false;

                while (true) {
                    currentPage = currentPage.parentPage;
                    if (currentPage == null)
                        break;
                    if (!currentPage.isActual)
                        currentPage.Clear();
                    else
                        break;
                }                
            }

            if (currentPage != null) {
                currentPage.Show();
                if (currentPage is IUIRefresh refresh) refresh.Refresh();
                yield return Animation(true);
            } else
                RemoveElements();
        }
        
        public void Clear() {
            Clearing().Run();
        }
        
        IEnumerator Clearing() {
            if (currentPage != null)
                yield return Animation(false);
            
            while (currentPage != null) {
                currentPage.Clear();
                currentPage.visible = false;
                currentPage = currentPage.parentPage;
            }

            RemoveElements();
        }
        
        IEnumerator Animation(bool targetVisibility) {
            if (panel) {
                if (panel.isPlaying)
                    yield return panel.WaitPlaying();
                panel.SetVisible(targetVisibility);
                yield return panel.WaitPlaying();
            } else
                gameObject.SetActive(targetVisibility);
        }

        public bool IsShown() {
            return gameObject.activeSelf;
        }
    }

    public interface IPageContainer : ILiveContexted {
        void Close();
        Transform GetContainer();
        void Show(Page page);
        Page GetCurrentPage();
        UIColorScheme GetColorScheme();
    }
    
    public abstract class Page {
        public IPageContainer pageManager {get; private set;}
        List<ComposedElement> elements = new List<ComposedElement>();
        
        public bool visible { get; set; } = false;
        public bool isActual = true;

        public Page parentPage = null;
        public bool backgroundClose = true;
        public bool interactable = true;

        public bool IsBuilt {
            get; private set;
        } 

        public Page() {
            IsBuilt = false;
        }

        public virtual void Refresh() {
            Clear();
            IsBuilt = false;
            Build(pageManager);
        }

        public abstract void Building();

        public void Build(IPageContainer pageManager) {
            if (IsBuilt) return;
            IsBuilt = true;
            this.pageManager = pageManager;
            
            try {
                Building();
            } catch (Exception e) {
                Debug.LogException(e);
            }
            
            var colorScheme = pageManager.GetColorScheme();
            if (colorScheme != null)
                pageManager.GetContainer()?
                    .gameObject.Repaint(colorScheme);
        }

        public void Show() {
            if (this is IUIRefresh refresh)
                UIRefresh.Add(refresh);
            elements.ForEach(e => e.gameObject.SetActive(e.IsVisible()));
            visible = true;
        }
        
        public void Hide() {
            if (this is IUIRefresh refresh)
                UIRefresh.Remove(refresh);
            elements.ForEach(e => e.gameObject.SetActive(false));
            visible = false;
        }

        public void Clear() {
            elements.ForEach(e => e.Kill());
            elements.Clear();
        }

        public Action onClose = delegate {};
        
        public void Close() {
            if (pageManager != null && pageManager.GetCurrentPage() == this)
                pageManager.Close();
            if (this is IUIRefresh refresh) UIRefresh.Remove(refresh);
            onClose?.Invoke();
        }

        public IEnumerator Wait() {
            while (!IsBuilt)
                yield return null;
            while (visible)
                yield return null;
        }
        
        public T AddElementWithSuffix<T>(string suffix) where T : ComposedElement {
            return AddElement<T>(typeof(T).Name + suffix);
        }

        public T AddElement<T>(string name = null) where T : ComposedElement {
            if (pageManager == null) {
                Debug.LogError("Page Manager is not set yet");
                return null;
            }
            
            if (name == null) name = typeof(T).Name;

            T result = AssetManager.Emit<T>(name, pageManager.context) 
                       ?? AssetManager.Emit<T>("", pageManager.context);

            if (pageManager.GetContainer() != null) {
                result.transform.SetParent(pageManager.GetContainer());
                result.transform.Reset();
            }

            result.page = this;
            result.OnSetup();
            elements.Add(result);

            return result;
        }

        public void ShowSubPage(Page page) {
            page.parentPage = this;
            visible = false;
            pageManager.Show(page);
        }
    }
    
    public class FastPage : Page {
        Action<Page> build;
        
        public FastPage(Action<Page> build) {
            this.build = build;
        }
        
        public override void Building() {
            build?.Invoke(this);
        }
    }
}