﻿using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using Yurowm.Localizations;
using System.Collections;
using Yurowm.Extensions;

namespace Yurowm.UI {
    public class LabelFormat : LabelTextProviderBehaviour {

        [HideInInspector]
        public string format = "";
        
        public int crop = 0;

        public string Format => format;

        Dictionary<string, string> dictionary = new Dictionary<string, string>();

        public string this[string index] {
            get => dictionary.Get(index);
            set {
                dictionary[index] = value;
                SetDirty();
            }
        }

        public void SetText(LocalizedText text) {
            format = text.text;
            SetDirty();
        }
        
        public override void Initialize() {
            base.Initialize();
            InitializeWords();
        }

        static readonly Regex wordFormat = new Regex(@"\{(?<word>[\@A-Za-z0-9_]+)\}");
        void InitializeWords() {
            foreach (Match match in wordFormat.Matches(Format)) {
                var key = match.Groups["word"].Value;
                dictionary.TryAdd(key, key);
            }
        }

        protected override void OnEnable() {
            InitializeWords();
            base.OnEnable();
        }

        public override string GetText() {
            string result = Format;
            foreach (var word in dictionary) {
                var value = word.Value;
                
                if (word.Key.StartsWith("@")) {
                    var key = word.Key[1..];
                    value = ReferenceValues.Get(key)?.ToString() ?? "";
                }
                
                result = result.Replace("{" + word.Key + "}", value ?? string.Empty);
            }
            
            if (crop > 0 && result.Length > crop)
                return result[..crop].TrimEnd() + "...";
            
            return result;
        }
    }
}