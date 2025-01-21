using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System;
using System.Reflection;
using System.Linq;
using Cysharp.Threading.Tasks;
using Yurowm.Console;
using Yurowm.Coroutines;
using Yurowm.DebugTools;
using Yurowm.Extensions;
using Yurowm.Integrations;
using Yurowm.Serialization;
using Yurowm.UI;
using Yurowm.Utilities;
using Yurowm.YJSONSerialization;

namespace Yurowm.Localizations {
    public static class Localization {
        static LocalizationSettings _settings;
        static LocalizationSettings settings {
            get {
                if (_settings == null)
                    _settings = GameSettings.Instance.GetModule<LocalizationSettings>();
                return _settings;
            }
        }
        
        public static Language defaultLanguage;
        
        static Language[] supportLanguages;
        
        public static Language language {
            get => settings?.language ?? Language.Unknown;
            private set => settings.SetLanguage(value);
        }
        
        public static bool IsSupported(this Language language) {
            return supportLanguages?.Contains(language) ?? false;
        }
        
        public static Language GetFallbackLanguage(this Language language) {
            if (language.IsSupported())
                return language;
            
            var result = language;
            
            switch (language) {
                case Language.Belarusian:
                case Language.Ukrainian:
                case Language.Uzbek:
                case Language.Kazakh:
                    result = Language.Russian;
                    break;
                default:
                    result = Language.English;
                    break;
            }
            
            if (language == result)
                return Language.Unknown;
            
            return result.GetFallbackLanguage();
        }

        public static LanguageContent content {
            get; private set;
        }
        
        public static string localized(this string key) {
            return content[key];
        }
        
        [OnLaunch(int.MinValue)]
        static async UniTask OnLaunch() {
            if (OnceAccess.GetAccess("Localization")) {
                supportLanguages = await LanguageContent.GetSupportedLanguagesProgess();

                defaultLanguage = Language.English;

                foreach (var l in supportLanguages) 
                    DebugPanel.Log(l.ToString(), "Localization", () => LearnLanguage(l).Forget());
                
                // #if UNITY_EDITOR || DEVELOPMENT_BUILD
                //
                #region Default logic
                
                if (!supportLanguages.Contains(defaultLanguage))
                    defaultLanguage = supportLanguages.IsEmpty() ? Language.Unknown : supportLanguages.First();
                    
                if (language == Language.Unknown)
                    language = (Language) (int) Application.systemLanguage;
                
                if (!supportLanguages.Contains(language))
                    language = defaultLanguage;
                
                #endregion
                //
                // #else
                
                if (language == Language.Unknown)
                    language = defaultLanguage;
                
                language = language.GetFallbackLanguage();
                
                // #region Force English
                //
                // language = defaultLanguage;
                //
                // #endregion

                // #endif
                
                await LearnLanguage(language);
            }
        }

        public static Language GetNativeLanguage() {
            var native = (Language) (int) Application.systemLanguage;
            // var native = Language.German;

            if (!supportLanguages.Contains(native))
                native = defaultLanguage;
            
            return native;
        }

        [QuickCommand("localize", "test", "Show localization for 'test' key")]
        static void ShowLocalization(string key) { 
            YConsole.Alias(content[key]);
        }

        public static async UniTask LearnLanguage(Language language) {
            if (content != null && content.language == language)
                return;
            Localization.language = language;
            content = await LanguageContent.LoadProcess(Localization.language, true);
            UIRefresh.Invoke();
        }
        
        public static IEnumerable<string> CollectKeys(object entry) {
            if (entry is string s) {
                yield return s;
                yield break;
            }
            
            IEnumerator enumerator;

            switch (entry) {
                case ILocalized l: enumerator = l.GetLocalizationKeys().GetEnumerator(); break;
                case IEnumerable el: enumerator = el.GetEnumerator(); break;
                case IEnumerator er: enumerator = er; break;
                default: yield break;
            }

            while (enumerator.MoveNext())
                foreach (var key in CollectKeys(enumerator.Current))
                    yield return key;
        }
    }
    
    public class LocalizationSettings : SettingsModule {
        
        public Language language = Language.Unknown;
        
        public void SetLanguage(Language value) {
            if (language == value) return;
            language = value;
            SetDirty();
        }        
        
        public override void Serialize(IWriter writer) {
            writer.Write("language", language);
        }

        public override void Deserialize(IReader reader) {
            reader.Read("language", ref language);
        }
    }
    
    public class LanguageContent : ISerializable, IEnumerable<KeyValuePair<string, string>> {
        static readonly string iniFileName = "list" + Serializer.FileExtension;
        Dictionary<string, string> content = new();
        public Language language;

        public string this[string key] {
            get {
                if (!content.ContainsKey(key))
                    return key;
                return content[key];
            }
            set => content[key] = value;
        }

        public bool HasKey(string key) {
            return content.ContainsKey(key);
        }
        
        public void Remove(string key) {
            content.Remove(key);
        }

        public void Deserialize(IReader reader) {
            content = reader.ReadDictionary<string>("content")
                .Where(p => p.Key != null)
                .ToDictionary();
        }

        public void Serialize(IWriter writer) {
            writer.WriteDictionary("content", content.OrderBy(p => p.Key).ToDictionary());
        }
        
        #region Supported Languages
        
        public static async UniTask<Language[]> GetSupportedLanguagesProgess() {
            var raw = await TextData.LoadTextTask(Path.Combine("Languages", iniFileName));

            if (raw.IsNullOrEmpty())
                return Array.Empty<Language>();
            
            return raw
                .Split(',')
                .Select(t => Enum.TryParse(t.Trim(), out Language language) ? language : Language.Unknown)
                .Where(l => l != Language.Unknown)
                .ToArray();
        }

        public static IEnumerable<Language> GetSupportedLanguages() {
            var raw = TextData.LoadTextInEditor(Path.Combine("Languages", iniFileName));

            if (raw.IsNullOrEmpty()) {
                #if UNITY_EDITOR
                var directory = new DirectoryInfo(Path.Combine(Application.streamingAssetsPath, "Languages"));
                if (!directory.Exists) directory.Create();
                var files = Directory.GetFiles(directory.FullName)
                    .Select(p => new FileInfo(p)).Where(f => f.Extension == Serializer.FileExtension).ToArray();
                if (files.Length > 0) {
                    foreach (var file in files) {
                        if (Enum.TryParse(file.NameWithoutExtension(), out Language language))
                            yield return language;
                    }
                    yield break;
                }
                #endif

                yield return Language.English;
            } else {
                foreach (string lang in raw.Split(',').Select(t => t.Trim())) {
                    if (Enum.TryParse(lang, out Language language))
                        yield return language;
                }
            }
        }

        public static void SetSupportedLanguages(IEnumerable<Language> languages) {
            #if UNITY_EDITOR
            File.WriteAllText(Path.Combine(Application.streamingAssetsPath, "Languages", iniFileName),
                string.Join(",", languages.Select(l => l.ToString()).ToArray()));
            #else
            Debug.LogError("It works only in Editor mode");
            #endif
        }

        #endregion

        public static async UniTask<LanguageContent> LoadProcess(Language language, bool createNew) {
            string raw = await TextData.LoadTextTask(
                Path.Combine("Languages", language + Serializer.FileExtension));
            
            return Load(raw, language, createNew);
        }

        public static LanguageContent LoadFast(Language language, bool createNew) {
            var raw = TextData.LoadTextInEditor(Path.Combine("Languages", language + Serializer.FileExtension));
            return Load(raw, language, createNew);
        }

        static LanguageContent Load(string raw, Language language, bool createNew) {
            if (raw.IsNullOrEmpty())
                return createNew ? CreateEmpty() : null;

            var result = Serializer.Instance.FromJson<LanguageContent>(raw) ?? new ();
            result.language = language;
            return result;
        }

        public static LanguageContent CreateEmpty(IEnumerable<string> keys = null) {
            LanguageContent result = new LanguageContent();

            if (keys != null)
                foreach (string key in keys)
                    result[key] = key;

            return result;
        }

        public static LanguageContent Create(Dictionary<string, string> dictionary) {
            LanguageContent result = new LanguageContent();
            foreach (var pair in dictionary)
                result[pair.Key] = pair.Value;
            return result;
        }
        
        public bool IsEmpty() => content.IsEmpty();

        #region IEnumerable
        public IEnumerator<KeyValuePair<string, string>> GetEnumerator() {
            foreach (var pair in content)
                yield return pair;
        }

        IEnumerator IEnumerable.GetEnumerator() {
            return GetEnumerator();
        }
        #endregion
    }

    [AttributeUsage(AttributeTargets.Property, AllowMultiple = false, Inherited = true)]
    public class EditorLocalizedKeyAttribute : Attribute {
        public readonly string name;
        public readonly bool longText;

        public EditorLocalizedKeyAttribute(string name, bool longText = false) {
            this.name = name;
            this.longText = longText;
        }
    }

    [AttributeUsage(AttributeTargets.Method | AttributeTargets.Field, AllowMultiple = false)]
    public class LocalizationKeysProvider : Attribute {
        Func<IEnumerable<string>> provider;
        
        public void SetMethod(MethodInfo method) {
            provider = () => Localization.CollectKeys(method.Invoke(null, Array.Empty<object>()));
        }
        
        public void SetField(FieldInfo field) {
            provider = () => Localization.CollectKeys(field.GetValue(null));
        }

        public IEnumerable<string> GetKeys() {
            if (provider != null)
                foreach (var key in provider())
                    yield return key;
        }
    }

    public interface ILocalized {
        IEnumerable GetLocalizationKeys();
    }

    public interface ILocalizedComponent : ILocalized { }
}
