using System;
using System.Linq;
using UnityEngine;
using Yurowm.Colors;
using Yurowm.Extensions;

namespace Yurowm.ComposedPages {
    public class ComposedArea : Behaviour, IPageContainer {
        [TypeSelector.Target(typeof (Page))]
        public TypeSelector pageType;

        Page currentPage;
        
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
            try {
                if (pageType.GetSelectedType() != null)
                    Show(pageType.Emit<Page>());
            } catch (Exception e) {
                Debug.LogException(e);
            }
        }

        public void Close() {
            currentPage?.Clear();
            currentPage = null;
        }

        public Transform GetContainer() {
            return transform;
        }

        public void Show(Page page) {
            if (currentPage != null || page.IsBuilt) return;
            
            currentPage = page;
            currentPage.Build(this);
            currentPage.Show();
        }

        void OnEnable() {
            if (currentPage != null)
                currentPage.visible = true;
        }

        void OnDisable() {
            currentPage?.Hide();
        }

        public Page GetCurrentPage() {
            return currentPage;
        }
    }
}