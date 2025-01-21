using System;
using UnityEngine;
using UnityEngine.UI;
using Yurowm.UI;

namespace Yurowm.DebugTools {
    public class DebugPanelEntryUI : VirtualizedScrollItemBody {
        public Text title;
        
        public RectTransform content;

        public UnityEngine.UI.Button moreButton;
        
        [NonSerialized]
        public MessageUI messageUI;

        public void Setup(DebugPanel.Entry entry) {
            title.text = entry.name;
            SetColor(DebugPanel.GroupToColor(entry.group));
        }

        void SetColor(Color color) {
            title.color = color;
        }
    }
}