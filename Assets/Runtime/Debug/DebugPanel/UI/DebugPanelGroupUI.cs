using System;
using UnityEngine;
using UnityEngine.UI;
using Yurowm.Extensions;

namespace Yurowm.DebugTools {
    public class DebugPanelGroupUI : MonoBehaviour {
        public Action action;
        
        Image background;

        UnityEngine.UI.Button button;
        Text title;
        
        void Awake() {
            Initialize();
        }
        
        
        bool isInitialized = false;
        void Initialize() {
            if (isInitialized)
                return;
            
            isInitialized = true;
            
            if (this.SetupComponent(out button))
                button.onClick.SetSingleListner(OnClick);
            this.SetupComponent(out background);
            this.SetupChildComponent(out title);
        }

        void OnClick() {
            action?.Invoke();
        }

        public void SetAlpha(float alpha) {
            Initialize();
            background.color = background.color.Transparent(alpha);
        }

        public void SetTitle(string text) {
            Initialize();
            title.text = text;
        }

        public void SetColor(Color color) {
            Initialize();
            background.color = color.Transparent(background.color.a);
        }
    }
}