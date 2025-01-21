using System;
using System.Collections.Generic;
using System.Linq;
using TMPro;
using UnityEngine;
using Yurowm.Extensions;

namespace Yurowm.ComposedPages {
    public class ComposedValueSelector  : ComposedElementTitled {
        public TMP_Text valueLabel;
        public UnityEngine.UI.Button leftButton;
        public UnityEngine.UI.Button rightButton;

        public Action<int> onSelect = null;

        int currentID = 0;

        public override void OnSetup() {
            base.OnSetup();
            
            leftButton.onClick.SetSingleListner(Left);
            
            rightButton.onClick.SetSingleListner(Right);
            
            valueLabel.text = string.Empty;
        }

        void Left() {
            if (values == null) return;
            currentID--;
            if (currentID < 0) currentID = values.Length - 1;
            Select(currentID);
        }

        void Right() {
            if (values == null) return;
            currentID++;
            if (currentID >= values.Length) currentID = 0;
            Select(currentID);
        }

        public void Select(int id) {
            SetValue(id);
            onSelect?.Invoke(id);
        }

        public void SetValue(int id) {
            if (values != null && values.Length > 0) {
                id = Mathf.Clamp(id, 0, values.Length - 1);
                currentID = id;
                valueLabel.text = values[id];
            }
        }

        string[] values;

        public void SetValues(IEnumerable<string> values) {
            this.values = values?.ToArray();
        }

        public override void Rollout() {
            base.Rollout();
            SetValues(null);
            currentID = 0;
            onSelect = null;
        }
    }
}