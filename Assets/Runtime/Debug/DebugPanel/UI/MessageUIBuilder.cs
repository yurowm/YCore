using System;
using UnityEngine.Scripting;
using Yurowm.Extensions;
using Yurowm.UI;
using Yurowm.Utilities;

namespace Yurowm.DebugTools {
    [Preserve]
    public abstract class MessageUIBuilder {
        public DebugPanelUI debugPanelUI;
        public bool extendMode;

        public abstract bool IsSuitableFor(Type messageType);

        protected abstract MessageUI EmitMessageUI(DebugPanel.Entry entry);
        
        class EntryUI : IVirtualizedScrollItem {
            readonly DebugPanel.Entry entry;
            readonly MessageUIBuilder builder;

            public EntryUI(DebugPanel.Entry entry, MessageUIBuilder builder) {
                this.entry = entry;
                this.builder = builder;
            }

            public void SetupBody(VirtualizedScrollItemBody body) {
                if (body is DebugPanelEntryUI eui) {
                    eui.Setup(entry);
                    
                    if (entry.message.IsExtendable()) {
                        eui.moreButton.gameObject.SetActive(true);
                        eui.moreButton.onClick.SetSingleListner(Extend);
                    } else
                        eui.moreButton.gameObject.SetActive(false);

                    EmitMessage(eui);
                }
            }

            void EmitMessage(DebugPanelEntryUI eui) {
                builder.debugPanelUI.ClearEntry(eui);
                
                var messageUI = builder.EmitMessageUI(entry);
                if (messageUI) {
                    eui.messageUI = messageUI;
                    
                    messageUI.transform.SetParent(eui.content);
                    messageUI.transform.Reset();
                    messageUI.transform.rect().Maximize();
                }
            }
            
            void Extend() {
                var eui = builder.debugPanelUI.fullScreenEntryUI;
                
                if (!eui) return;
                
                eui.Setup(entry);
                    
                eui.moreButton.onClick.SetSingleListner(() => 
                    builder.debugPanelUI.ExpandMessage(false));
                    
                builder.extendMode = true;
                EmitMessage(eui);
                builder.extendMode = false;
                
                builder.debugPanelUI.ExpandMessage(true);
            }

            public string GetBodyPrefabName() => null;
        }

        public IVirtualizedScrollItem NewEntry(DebugPanel.Entry entry) {
            return new EntryUI(entry, this);
        }
    }
}